namespace HCMv3.ManpowerPlanningManagement.Dto;

/// <summary>
/// Backs the chart's search box: type a few characters and jump straight to a position instead of
/// stepping down to it through the hierarchy.
/// </summary>
public class SearchPositionOrgChartNodesInput
{
    /// <summary>Matched against the English name, the Arabic name and the position code.</summary>
    public string Filter { get; set; }

    /// <summary>
    /// Caps the dropdown. Clamped to <see cref="MaxAllowedResultCount"/> server-side; this is a type-ahead,
    /// not a report, so an unbounded request is never honoured.
    /// </summary>
    public int MaxResultCount { get; set; } = DefaultResultCount;

    public const int DefaultResultCount = 50;

    public const int MaxAllowedResultCount = 200;
}
