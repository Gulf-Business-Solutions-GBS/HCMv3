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

    public override string ToString() => $"{Source} -> {Target}";
}
