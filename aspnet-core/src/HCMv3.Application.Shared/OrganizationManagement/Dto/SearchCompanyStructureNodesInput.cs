namespace HCMv3.OrganizationManagement.Dto;

/// <summary>
/// Backs the toolbar's node-search dropdown: type a few characters, optionally narrowed to one
/// organization unit type, and jump straight to that node instead of drilling down level by level.
/// </summary>
public class SearchCompanyStructureNodesInput
{
    /// <summary>Matched against the English name, the Arabic name and the business code.</summary>
    public string Filter { get; set; }

    /// <summary>Optional narrowing to a single <c>OrganizationUnitType</c> (the "Search" dropdown).</summary>
    public int? OrganizationUnitTypeId { get; set; }

    /// <summary>
    /// When set, only nodes inside that root's subtree are returned -- so the search obeys whichever
    /// company structure the user currently has selected.
    /// </summary>
    public long? RootOrganizationUnitId { get; set; }

    /// <summary>
    /// Caps the dropdown. Clamped to <see cref="MaxAllowedResultCount"/> server-side; this is a
    /// type-ahead, not a report, so an unbounded request is never honoured.
    /// </summary>
    public int MaxResultCount { get; set; } = DefaultResultCount;

    public const int DefaultResultCount = 50;

    public const int MaxAllowedResultCount = 200;
}
