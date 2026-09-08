using System;

namespace HCMv3.PersonnelAdministration.Dto;

/// <summary>
/// Backs the chart's search box: type a few characters and jump straight to that person instead of
/// drilling down through the reporting lines to find them.
/// </summary>
public class SearchEmployeeOrgChartNodesInput
{
    /// <summary>Matched against the English name, the Arabic name and the employee code.</summary>
    public string Filter { get; set; }

    /// <summary>Reporting lines are date-bound, so the search obeys the chart's own date. Null means today.</summary>
    public DateTime? AsOfDate { get; set; }

    /// <summary>
    /// Caps the dropdown. Clamped to <see cref="MaxAllowedResultCount"/> server-side; this is a type-ahead,
    /// not a report, so an unbounded request is never honoured.
    /// </summary>
    public int MaxResultCount { get; set; } = DefaultResultCount;

    public const int DefaultResultCount = 50;

    public const int MaxAllowedResultCount = 200;
}
