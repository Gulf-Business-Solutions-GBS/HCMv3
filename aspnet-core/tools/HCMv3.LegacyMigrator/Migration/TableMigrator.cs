using System.Diagnostics;
using System.Text;
using HCMv3.LegacyMigrator.Plan;
using HCMv3.LegacyMigrator.Schema;
using Microsoft.Data.SqlClient;

namespace HCMv3.LegacyMigrator.Migration;

/// <summary>
/// Migrates ONE table, entirely set-based.
///
/// <para><b>Why set-based T-SQL and not EF Core.</b> Both databases live on the same SQL Server instance, so
/// the source is addressable as a three-part name (<c>HRMS_RCF.dbo.X</c>) from a connection opened against the
/// target. That means the extract, the FK remap and the insert are a single server-side statement per table:
/// no rows cross the wire at all. Row-by-row <c>SaveChanges</c> would be roughly 1.34M round-trips for
/// <c>TimeRegister</c> alone on a full customer, and would additionally require every HCMv3 entity to be
/// loadable, which drags in ABP's tenant filters, soft-delete filters and audit interception -- all of which
/// actively fight a migration. The cost of the set-based choice is that column mapping has to be derived from
/// metadata rather than from the C# type system; <see cref="ColumnMapper"/> pays that cost once.</para>
///
/// <para><b>How new identity values are captured.</b> A plain <c>INSERT ... SELECT</c> cannot tell you which
/// generated identity belongs to which source row. <c>MERGE ... WHEN NOT MATCHED THEN INSERT ... OUTPUT</c>
/// can, because MERGE's OUTPUT clause may reference columns of the USING source alongside <c>inserted.*</c>.
/// That single property is what makes a set-based, mapping-table-driven migration possible at all, and it is
/// why every table goes through MERGE rather than INSERT.</para>
/// </summary>
public sealed class TableMigrator
{
    private readonly string _targetConnectionString;
    private readonly string _sourceDatabase;
    private readonly int _tenantId;
    private readonly int _commandTimeoutSeconds;

    public TableMigrator(string targetConnectionString, string sourceDatabase, int tenantId, int commandTimeoutSeconds)
    {
        _targetConnectionString = targetConnectionString;
        _sourceDatabase = sourceDatabase;
        _tenantId = tenantId;
        _commandTimeoutSeconds = commandTimeoutSeconds;
    }

    public TableResult Migrate(
        TableSpec spec,
        TableInfo source,
        TableInfo target,
        IReadOnlyDictionary<string, TableSpec> planByTarget,
        IReadOnlyCollection<int> selectedEmployeeOids,
        Guid runId,
        bool dryRun,
        TextWriter sqlLog)
    {
        var result = new TableResult { SourceTable = spec.Source, TargetTable = spec.Target };
        var sw = Stopwatch.StartNew();

        var mappings = ColumnMapper.Map(spec, source, target, planByTarget, out var unmappedSource);
        result.DroppedSourceColumns.AddRange(unmappedSource);

        // NULL -> NOT NULL coercions are only worth reporting if the data actually contains NULLs; see
        // NullCoercionProbe for why the purely schema-derived list is useless noise.
        var coercionCandidates = mappings
            .Where(m => m.Kind == MappingKind.Direct && m.Note != null && m.SourceColumn != null)
            .ToDictionary(m => m.SourceColumn, m => m, StringComparer.OrdinalIgnoreCase);

        Dictionary<string, int> actualNullCounts = new(StringComparer.OrdinalIgnoreCase);
        if (coercionCandidates.Count > 0)
        {
            var probeWhere = BuildScopeWhere(spec, source, selectedEmployeeOids);
            actualNullCounts = NullCoercionProbe.Count(
                _targetConnectionString, _sourceDatabase, spec.Source, coercionCandidates.Keys, probeWhere);
        }

        foreach (var m in mappings)
        {
            if (m.Note == null)
            {
                continue;
            }

            if (m.Kind == MappingKind.DefaultedUnmapped)
            {
                result.DefaultedTargetColumns.Add($"{m.TargetColumn} ({m.Note})");
            }
            else if (m.Kind == MappingKind.Direct && m.SourceColumn != null && coercionCandidates.ContainsKey(m.SourceColumn))
            {
                if (actualNullCounts.TryGetValue(m.SourceColumn, out var n))
                {
                    result.Warnings.Add($"{m.TargetColumn}: {n} row(s) had NULL [{m.SourceColumn}] in legacy and were coerced to the target's NOT NULL default");
                }
            }
            else
            {
                result.Warnings.Add($"{m.TargetColumn}: {m.Note}");
            }
        }

        var insertable = mappings.Where(m => m.Expression != null).ToList();
        if (insertable.Count == 0)
        {
            result.Skipped = true;
            result.SkipReason = "no insertable columns";
            result.Duration = sw.Elapsed;
            return result;
        }

        var sql = BuildSql(spec, source, target, insertable, selectedEmployeeOids, out var requiredFkAliases);
        sqlLog?.WriteLine($"-- ================= {spec.Source} -> {spec.Target} =================");
        sqlLog?.WriteLine(sql);
        sqlLog?.WriteLine();

        if (dryRun)
        {
            result.Skipped = true;
            result.SkipReason = "dry run";
            result.Duration = sw.Elapsed;
            return result;
        }

        using var conn = new SqlConnection(_targetConnectionString);
        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            using (var cmd = new SqlCommand(sql, conn, tx) { CommandTimeout = _commandTimeoutSeconds })
            {
                cmd.Parameters.AddWithValue("@TenantId", _tenantId);
                cmd.Parameters.AddWithValue("@RunId", runId);
                cmd.Parameters.AddWithValue("@Entity", spec.Target);

                using var r = cmd.ExecuteReader();
                if (r.Read())
                {
                    result.RowsInScope = r.GetInt32(0);
                    result.RowsAlreadyMapped = r.GetInt32(1);
                    result.RowsSkippedUnresolvedFk = r.GetInt32(2);
                    result.RowsWritten = r.GetInt32(3);
                }
            }

            tx.Commit();
        }
        catch
        {
            try { tx.Rollback(); } catch { /* connection may already be dead */ }
            throw;
        }

        // Only worth reporting when it actually bit: the mandatory-FK list is otherwise present on nearly every
        // table and drowns out the real findings.
        if (result.RowsSkippedUnresolvedFk > 0)
        {
            result.Warnings.Add(
                $"{result.RowsSkippedUnresolvedFk} row(s) DROPPED because a NOT NULL foreign key could not be " +
                "resolved through LegacyIdMap. Candidates: " + string.Join(", ", requiredFkAliases));
        }

        result.Duration = sw.Elapsed;
        return result;
    }

    /// <summary>
    /// The row-selection predicate, shared by the extract, the idempotency count and the NULL probe so that
    /// all three can never drift apart.
    /// </summary>
    private static List<string> BuildScopePredicates(TableSpec spec, TableInfo source, IReadOnlyCollection<int> selectedEmployeeOids)
    {
        var predicates = new List<string>();

        if (source.Has("GCRecord"))
        {
            // XPO soft delete. Non-negotiable: 196 legacy tables carry GCRecord and a non-NULL value means the
            // row is deleted. Omitting this filter silently migrates deleted data.
            predicates.Add("s.[GCRecord] IS NULL");
        }

        switch (spec.Scope)
        {
            case ScopeKind.Employee when selectedEmployeeOids.Count > 0:
                predicates.Add($"s.[{spec.ScopeColumn}] IN ({string.Join(",", selectedEmployeeOids)})");
                break;
            case ScopeKind.Parent:
                predicates.Add(
                    $"EXISTS (SELECT 1 FROM {IdMapStore.MapTable} pm " +
                    $"WHERE pm.EntityName = N'{spec.ScopeParentEntity}' AND pm.LegacyOid = s.[{spec.ScopeColumn}])");
                break;
        }

        if (!string.IsNullOrWhiteSpace(spec.ExtraFilter))
        {
            predicates.Add("(" + spec.ExtraFilter + ")");
        }

        return predicates;
    }

    private static string BuildScopeWhere(TableSpec spec, TableInfo source, IReadOnlyCollection<int> selectedEmployeeOids)
    {
        var p = BuildScopePredicates(spec, source, selectedEmployeeOids);
        return p.Count > 0 ? "WHERE " + string.Join("\n  AND ", p) : string.Empty;
    }

    private string BuildSql(
        TableSpec spec,
        TableInfo source,
        TableInfo target,
        List<ColumnMapping> mappings,
        IReadOnlyCollection<int> selectedEmployeeOids,
        out List<string> requiredFkColumns)
    {
        requiredFkColumns = new List<string>();
        var sb = new StringBuilder();
        var srcRef = $"[{_sourceDatabase}].[dbo].[{spec.Source}]";

        // ---- scope predicate ------------------------------------------------------------------------
        var predicates = BuildScopePredicates(spec, source, selectedEmployeeOids);
        var scopeWhere = predicates.Count > 0 ? "WHERE " + string.Join("\n  AND ", predicates) : string.Empty;

        // ---- FK joins -------------------------------------------------------------------------------
        var joins = new StringBuilder();
        foreach (var m in mappings.Where(x => x.Kind == MappingKind.ForeignKey))
        {
            joins.AppendLine(
                $"  LEFT JOIN {IdMapStore.MapTable} {m.JoinAlias} " +
                $"ON {m.JoinAlias}.EntityName = N'{m.ReferencedTargetTable}' AND {m.JoinAlias}.LegacyOid = s.[{m.SourceColumn}]");
        }

        // A NOT NULL FK whose legacy value is present but unresolvable would violate the constraint; such rows
        // are excluded and counted rather than being silently coerced to a wrong parent.
        var fkGuards = new List<string>();
        foreach (var m in mappings.Where(x => x.Kind == MappingKind.ForeignKey && !x.TargetIsNullable))
        {
            fkGuards.Add($"{m.JoinAlias}.NewId IS NOT NULL");
            requiredFkColumns.Add($"{m.TargetColumn} -> {m.ReferencedTargetTable}");
        }

        var columnList = string.Join(", ", mappings.Select(m => $"[{m.TargetColumn}]"));
        var selectList = string.Join(",\n         ", mappings.Select(m => $"{m.Expression} AS [{m.TargetColumn}]"));

        sb.AppendLine("SET NOCOUNT ON;");
        sb.AppendLine("SET XACT_ABORT ON;");
        sb.AppendLine();

        // (a) how many live source rows are in scope at all
        sb.AppendLine("DECLARE @InScope int, @AlreadyMapped int, @SkippedFk int, @Written int;");
        sb.AppendLine();
        sb.AppendLine($"SELECT @InScope = COUNT(*) FROM {srcRef} s");
        sb.AppendLine(scopeWhere);
        sb.AppendLine(";");
        sb.AppendLine();

        // (b) how many of those are already mapped (the idempotency skip)
        sb.AppendLine($"SELECT @AlreadyMapped = COUNT(*) FROM {srcRef} s");
        sb.AppendLine($"  INNER JOIN {IdMapStore.MapTable} em ON em.EntityName = @Entity AND em.LegacyOid = s.[OID]");
        sb.AppendLine(scopeWhere);
        sb.AppendLine(";");
        sb.AppendLine();

        // (c) stage the remapped, not-yet-migrated rows
        var notAlreadyMapped =
            $"NOT EXISTS (SELECT 1 FROM {IdMapStore.MapTable} em WHERE em.EntityName = @Entity AND em.LegacyOid = s.[OID])";

        var stageWhere = new List<string>(predicates) { notAlreadyMapped };

        sb.AppendLine("IF OBJECT_ID('tempdb..#stage') IS NOT NULL DROP TABLE #stage;");
        sb.AppendLine($"SELECT s.[OID] AS __LegacyOid,");
        sb.AppendLine($"         {selectList}");
        sb.AppendLine("  INTO #stage");
        sb.AppendLine($"  FROM {srcRef} s");
        sb.Append(joins);
        sb.AppendLine("  WHERE " + string.Join("\n    AND ", stageWhere));
        sb.AppendLine(";");
        sb.AppendLine();

        sb.AppendLine("SELECT @SkippedFk = 0;");
        if (fkGuards.Count > 0)
        {
            // Recompute the same staged set with the FK guards applied, and delete the rest.
            sb.AppendLine("IF OBJECT_ID('tempdb..#keep') IS NOT NULL DROP TABLE #keep;");
            sb.AppendLine("SELECT s.[OID] AS __LegacyOid INTO #keep");
            sb.AppendLine($"  FROM {srcRef} s");
            sb.Append(joins);
            sb.AppendLine("  WHERE " + string.Join("\n    AND ", stageWhere.Concat(fkGuards)));
            sb.AppendLine(";");
            sb.AppendLine("DELETE st FROM #stage st WHERE NOT EXISTS (SELECT 1 FROM #keep k WHERE k.__LegacyOid = st.__LegacyOid);");
            sb.AppendLine("SET @SkippedFk = @@ROWCOUNT;");
            sb.AppendLine();
        }

        // (d) MERGE-insert, capturing legacy OID -> new identity
        sb.AppendLine("IF OBJECT_ID('tempdb..#newmap') IS NOT NULL DROP TABLE #newmap;");
        sb.AppendLine("CREATE TABLE #newmap (LegacyOid int NOT NULL PRIMARY KEY, NewId int NOT NULL);");
        sb.AppendLine();
        sb.AppendLine($"MERGE INTO [dbo].[{spec.Target}] WITH (HOLDLOCK) AS tgt");
        sb.AppendLine("USING #stage AS src");
        sb.AppendLine("   ON 1 = 0"); // never matches => every staged row is an insert, but OUTPUT can see src
        sb.AppendLine("WHEN NOT MATCHED BY TARGET THEN");
        sb.AppendLine($"  INSERT ({columnList})");
        sb.AppendLine($"  VALUES ({string.Join(", ", mappings.Select(m => $"src.[{m.TargetColumn}]"))})");
        sb.AppendLine("OUTPUT src.__LegacyOid, inserted.[Id] INTO #newmap (LegacyOid, NewId);");
        sb.AppendLine();
        sb.AppendLine("SET @Written = (SELECT COUNT(*) FROM #newmap);");
        sb.AppendLine();
        sb.AppendLine($"INSERT INTO {IdMapStore.MapTable} (EntityName, LegacyOid, NewId, RunId)");
        sb.AppendLine("SELECT @Entity, n.LegacyOid, n.NewId, @RunId FROM #newmap n;");
        sb.AppendLine();
        sb.AppendLine("DROP TABLE #stage;");
        sb.AppendLine("DROP TABLE #newmap;");
        if (fkGuards.Count > 0) sb.AppendLine("DROP TABLE #keep;");
        sb.AppendLine();
        sb.AppendLine("SELECT @InScope, @AlreadyMapped, @SkippedFk, @Written;");

        return sb.ToString();
    }
}
