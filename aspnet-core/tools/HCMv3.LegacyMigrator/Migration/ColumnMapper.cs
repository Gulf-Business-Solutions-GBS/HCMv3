using HCMv3.LegacyMigrator.Plan;
using HCMv3.LegacyMigrator.Schema;

namespace HCMv3.LegacyMigrator.Migration;

public enum MappingKind
{
    Direct,
    ForeignKey,
    Tenant,
    AuditCreationTime,
    AuditModificationTime,
    AuditIsDeleted,
    ConstantNull,
    DefaultedUnmapped
}

public sealed class ColumnMapping
{
    public string TargetColumn { get; init; }
    public string SourceColumn { get; init; }
    public MappingKind Kind { get; init; }
    public string ReferencedTargetTable { get; init; }
    public string JoinAlias { get; init; }
    public bool TargetIsNullable { get; init; }
    public string Expression { get; init; }
    public string Note { get; init; }
}

/// <summary>
/// Derives, at run time, how every TARGET column is populated from the SOURCE row.
///
/// <para>Conventions applied, in priority order:</para>
/// <list type="number">
///   <item>explicit per-table override from the <see cref="TableSpec"/>;</item>
///   <item><c>TenantId</c> -&gt; the run's tenant parameter;</item>
///   <item>ABP audit columns -&gt; from the legacy <c>Sys*</c> columns where they exist, else sensible defaults;</item>
///   <item>same-named column on both sides -&gt; direct copy with an explicit CAST to the target type;</item>
///   <item>target <c>XxxId</c> where source has bare <c>Xxx</c> -&gt; FK remap through <c>LegacyIdMap</c>,
///         using the target's own FK constraint to learn which entity to resolve against;</item>
///   <item>anything else -&gt; left NULL, or type-defaulted if the target column is NOT NULL. Every use of this
///         last branch is reported, never silent.</item>
/// </list>
/// </summary>
public static class ColumnMapper
{
    private static readonly HashSet<string> AbpAuditColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "CreationTime", "CreatorUserId", "LastModificationTime", "LastModifierUserId",
        "IsDeleted", "DeleterUserId", "DeletionTime"
    };

    /// <summary>Legacy XPO bookkeeping columns that have no place in the target and must never be copied.</summary>
    public static readonly HashSet<string> LegacyInfrastructureColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "OID", "GCRecord", "OptimisticLockField", "SysCreator", "SysCreatedOn",
        "SysModifier", "SysModifiedOn", "SysTenant", "Locked"
    };

    public static List<ColumnMapping> Map(
        TableSpec spec,
        TableInfo source,
        TableInfo target,
        IReadOnlyDictionary<string, TableSpec> planByTarget,
        out List<string> unmappedSourceColumns)
    {
        var mappings = new List<ColumnMapping>();
        var consumedSourceColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var fkAliasSeq = 0;

        foreach (var t in target.Ordered)
        {
            if (t.IsIdentity || t.IsComputed)
            {
                continue; // Id is assigned by the target and captured into LegacyIdMap.
            }

            if (spec.IgnoreTargetColumns.Contains(t.Name))
            {
                mappings.Add(Unmapped(t, "explicitly ignored by plan"));
                continue;
            }

            // (1) explicit override
            if (spec.ColumnOverrides.TryGetValue(t.Name, out var overrideSource))
            {
                if (overrideSource == null)
                {
                    mappings.Add(Unmapped(t, "explicitly unmapped by plan"));
                    continue;
                }

                var src = source.Get(overrideSource);
                if (src == null)
                {
                    mappings.Add(Unmapped(t, $"override source column '{overrideSource}' not present in legacy table"));
                    continue;
                }

                consumedSourceColumns.Add(src.Name);
                mappings.Add(BuildDirectOrFk(spec, t, src, target, planByTarget, ref fkAliasSeq));
                continue;
            }

            // (2) tenant
            if (t.Name.Equals("TenantId", StringComparison.OrdinalIgnoreCase))
            {
                mappings.Add(new ColumnMapping
                {
                    TargetColumn = t.Name,
                    Kind = MappingKind.Tenant,
                    TargetIsNullable = t.IsNullable,
                    Expression = "@TenantId"
                });
                continue;
            }

            // (3) ABP audit
            if (AbpAuditColumns.Contains(t.Name))
            {
                mappings.Add(MapAuditColumn(t, source, consumedSourceColumns));
                continue;
            }

            // (4) direct same-name
            var direct = source.Get(t.Name);
            if (direct != null && !LegacyInfrastructureColumns.Contains(direct.Name))
            {
                consumedSourceColumns.Add(direct.Name);
                mappings.Add(BuildDirectOrFk(spec, t, direct, target, planByTarget, ref fkAliasSeq));
                continue;
            }

            // (5) XxxId <- Xxx
            if (t.Name.EndsWith("Id", StringComparison.Ordinal) && t.Name.Length > 2)
            {
                var bare = t.Name[..^2];
                var fkSource = source.Get(bare);
                if (fkSource != null)
                {
                    consumedSourceColumns.Add(fkSource.Name);
                    mappings.Add(BuildDirectOrFk(spec, t, fkSource, target, planByTarget, ref fkAliasSeq));
                    continue;
                }
            }

            mappings.Add(Unmapped(t, "no matching legacy column"));
        }

        unmappedSourceColumns = source.Ordered
            .Select(c => c.Name)
            .Where(n => !LegacyInfrastructureColumns.Contains(n) && !consumedSourceColumns.Contains(n))
            .ToList();

        return mappings;
    }

    private static ColumnMapping BuildDirectOrFk(
        TableSpec spec,
        ColumnInfo target,
        ColumnInfo source,
        TableInfo targetTable,
        IReadOnlyDictionary<string, TableSpec> planByTarget,
        ref int fkAliasSeq)
    {
        // Is this target column a foreign key we are able to remap?
        if (targetTable.ForeignKeys.TryGetValue(target.Name, out var referencedTable))
        {
            if (!planByTarget.ContainsKey(referencedTable))
            {
                // The referenced entity is not part of the migration closure -- we cannot invent an id for it.
                return Unmapped(target,
                    $"FK -> {referencedTable}, which is NOT in the migration plan; left NULL (legacy value {source.Name} discarded)");
            }

            var alias = $"fk{fkAliasSeq++}";
            return new ColumnMapping
            {
                TargetColumn = target.Name,
                SourceColumn = source.Name,
                Kind = MappingKind.ForeignKey,
                ReferencedTargetTable = referencedTable,
                JoinAlias = alias,
                TargetIsNullable = target.IsNullable,
                Expression = $"{alias}.NewId"
            };
        }

        // Plain scalar copy. CAST covers datetime -> datetime2, int -> bigint, widening decimals, etc.
        var expr = $"CAST(s.[{source.Name}] AS {target.CastType})";
        string note = null;

        if (!target.IsNullable && source.IsNullable)
        {
            expr = $"ISNULL({expr}, {DefaultLiteral(target)})";
            note = $"legacy [{source.Name}] is NULLable but target is NOT NULL; NULLs defaulted to {DefaultLiteral(target)}";
        }

        return new ColumnMapping
        {
            TargetColumn = target.Name,
            SourceColumn = source.Name,
            Kind = MappingKind.Direct,
            TargetIsNullable = target.IsNullable,
            Expression = expr,
            Note = note
        };
    }

    private static ColumnMapping MapAuditColumn(ColumnInfo t, TableInfo source, HashSet<string> consumed)
    {
        switch (t.Name.ToLowerInvariant())
        {
            case "creationtime":
                if (source.Has("SysCreatedOn"))
                {
                    consumed.Add("SysCreatedOn");
                    return new ColumnMapping
                    {
                        TargetColumn = t.Name,
                        SourceColumn = "SysCreatedOn",
                        Kind = MappingKind.AuditCreationTime,
                        TargetIsNullable = t.IsNullable,
                        Expression = $"ISNULL(CAST(s.[SysCreatedOn] AS {t.CastType}), SYSUTCDATETIME())"
                    };
                }

                return new ColumnMapping
                {
                    TargetColumn = t.Name,
                    Kind = MappingKind.AuditCreationTime,
                    TargetIsNullable = t.IsNullable,
                    Expression = "SYSUTCDATETIME()"
                };

            case "lastmodificationtime":
                if (source.Has("SysModifiedOn"))
                {
                    consumed.Add("SysModifiedOn");
                    return new ColumnMapping
                    {
                        TargetColumn = t.Name,
                        SourceColumn = "SysModifiedOn",
                        Kind = MappingKind.AuditModificationTime,
                        TargetIsNullable = t.IsNullable,
                        Expression = $"CAST(s.[SysModifiedOn] AS {t.CastType})"
                    };
                }

                return Unmapped(t, "no legacy SysModifiedOn");

            case "isdeleted":
                // Deleted legacy rows are excluded at extract time (GCRecord IS NULL), so everything landing
                // in the target is live by construction.
                return new ColumnMapping
                {
                    TargetColumn = t.Name,
                    Kind = MappingKind.AuditIsDeleted,
                    TargetIsNullable = t.IsNullable,
                    Expression = "CAST(0 AS bit)"
                };

            default:
                // CreatorUserId / LastModifierUserId / DeleterUserId / DeletionTime: legacy Sys* columns hold
                // XPO user references that have no HCMv3 AbpUsers equivalent, so they are intentionally dropped.
                return new ColumnMapping
                {
                    TargetColumn = t.Name,
                    Kind = MappingKind.ConstantNull,
                    TargetIsNullable = t.IsNullable,
                    Expression = t.IsNullable ? TypedNull(t) : DefaultLiteral(t),
                    Note = t.IsNullable ? null : "audit column is NOT NULL in target; type default used"
                };
        }
    }

    private static ColumnMapping Unmapped(ColumnInfo t, string reason) => new()
    {
        TargetColumn = t.Name,
        Kind = t.IsNullable ? MappingKind.ConstantNull : MappingKind.DefaultedUnmapped,
        TargetIsNullable = t.IsNullable,
        Expression = t.IsNullable ? TypedNull(t) : DefaultLiteral(t),
        Note = t.IsNullable ? reason : $"{reason}; target is NOT NULL so defaulted to {DefaultLiteral(t)}"
    };

    /// <summary>
    /// A NULL of the target column's exact type. The CAST is not cosmetic: the staging step is
    /// <c>SELECT ... INTO #stage</c>, and a bare <c>NULL</c> literal makes SQL Server type the staged column as
    /// <c>int</c>, which then fails the subsequent insert with "Operand type clash" for datetime2/uniqueidentifier
    /// /varbinary targets.
    /// </summary>
    public static string TypedNull(ColumnInfo c) => $"CAST(NULL AS {c.CastType})";

    public static string DefaultLiteral(ColumnInfo c)
    {
        var literal = c.SqlType.ToLowerInvariant() switch
        {
            "bit" => "0",
            "tinyint" or "smallint" or "int" or "bigint" => "0",
            "decimal" or "numeric" or "money" or "smallmoney" or "float" or "real" => "0",
            "nvarchar" or "nchar" or "varchar" or "char" or "text" or "ntext" => "N''",

            // Must be C#'s default(DateTime) == 0001-01-01, NOT a "looks empty" date like 1900-01-01.
            // Legacy stores NULL for "never happened" on columns the port made NOT NULL, and every payroll/time
            // engine guard tests `!= default(DateTime)`. A 1900-01-01 sentinel is a real date to those guards, so
            // it does not read as absent: EmployeePayrollStatus.EarliestMasterDataChange fails the run outright
            // ("EarliestRetroAccountingDate set to '16-12-1996'"), and DoNoAccountAfter is worse -- it silently
            // skips every period and reports the employee as SUCCESSFUL with no register written.
            "date" or "datetime2" => "'0001-01-01'",
            "datetimeoffset" => "'0001-01-01T00:00:00+00:00'",

            // datetime/smalldatetime cannot represent year 1 (their floor is 1753-01-01 and 1900-01-01
            // respectively), so default(DateTime) is unrepresentable and the guards above cannot be satisfied.
            // No such target column exists today -- every date column in HCMv3 is datetime2 -- so this branch is
            // unreachable; it is kept correct-as-possible rather than silently wrong if that ever changes.
            "datetime" or "smalldatetime" => "'1900-01-01'",

            "time" => "'00:00:00'",
            "uniqueidentifier" => "'00000000-0000-0000-0000-000000000000'",
            "varbinary" or "binary" or "image" => "0x",
            _ => null
        };

        return literal == null ? TypedNull(c) : $"CAST({literal} AS {c.CastType})";
    }
}
