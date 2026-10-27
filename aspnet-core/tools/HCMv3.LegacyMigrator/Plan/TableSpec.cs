namespace HCMv3.LegacyMigrator.Plan;

public enum ScopeKind
{
    /// <summary>Migrate every live (GCRecord IS NULL) row. Used for lookup/master tables.</summary>
    All,

    /// <summary>Migrate only rows whose <see cref="TableSpec.ScopeColumn"/> is one of the selected legacy employee OIDs.</summary>
    Employee,

    /// <summary>Migrate only rows whose <see cref="TableSpec.ScopeColumn"/> points at an already-migrated row of <see cref="TableSpec.ScopeParentEntity"/>.</summary>
    Parent
}

public sealed class TableSpec
{
    /// <summary>Target (HCMv3) table name. Also the entity key used in <c>LegacyIdMap</c>.</summary>
    public string Target { get; init; }

    /// <summary>Source (legacy) table name.</summary>
    public string Source { get; init; }

    public ScopeKind Scope { get; init; } = ScopeKind.All;

    /// <summary>Source column carrying the scoping FK (legacy naming, i.e. un-suffixed).</summary>
    public string ScopeColumn { get; init; }

    /// <summary>For <see cref="ScopeKind.Parent"/>: the TARGET table name of the parent, as recorded in <c>LegacyIdMap</c>.</summary>
    public string ScopeParentEntity { get; init; }

    /// <summary>
    /// Extra raw predicate applied to the source rows, referencing the source alias <c>s</c>.
    /// Kept out of the conventions so a customer-specific cutoff can be supplied without code changes.
    /// </summary>
    public string ExtraFilter { get; init; }

    /// <summary>
    /// Target-column -&gt; source-column overrides for the cases where the naming convention
    /// (<c>Foo</c> -&gt; <c>FooId</c>) does not hold. Value <c>null</c> means "leave unmapped deliberately".
    /// </summary>
    public Dictionary<string, string> ColumnOverrides { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Target columns to force to NULL / default even if a same-named source column exists.</summary>
    public HashSet<string> IgnoreTargetColumns { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Target column -&gt; referenced TARGET table, for columns that are foreign keys in MEANING but carry no
    /// FK CONSTRAINT in the HCMv3 schema.
    ///
    /// <para><b>Why this exists.</b> <see cref="Migration.ColumnMapper"/> learns which entity to remap a
    /// column against from the target's own <c>sys.foreign_keys</c>. A logical FK with no constraint is
    /// therefore invisible to it and falls through to the plain scalar branch, which copies the LEGACY OID
    /// verbatim into a column whose values are HCMv3 identities. Nothing fails, nothing is reported, and the
    /// column looks perfectly populated -- it just points at a different row, or at no row at all.</para>
    ///
    /// <para><b>This is strictly worse than the OvertimeTypes ordering bug</b> documented in
    /// <see cref="MigrationPlan"/>. There a constrained-but-too-early FK resolved to NULL, and NULL at least
    /// READS as "absent". Here a wrong non-NULL value is written, and every consumer treats it as real.
    /// Confirmed damage: <c>AbsenceTypes.WageTypeId</c> held legacy OIDs 7/80/82/... while HCMv3's
    /// <c>WageTypes</c> ids run 976-1300, so the ENTIRE leave-pay wage-type dispatch matched nothing.</para>
    ///
    /// <para>Declared per column deliberately. Do NOT infer this from the <c>*Id</c> suffix: the schema has
    /// several non-FK columns that end in <c>Id</c> (<c>Employees.CivilID</c>, <c>Employees.LoginID</c>,
    /// <c>Employees.AccessID</c>, <c>ManagePayrollSettingses.ClientId</c> -- an nvarchar(max)), and blanket
    /// mapping would null them out.</para>
    /// </summary>
    public Dictionary<string, string> LogicalForeignKeys { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public override string ToString() => $"{Source} -> {Target}";
}
