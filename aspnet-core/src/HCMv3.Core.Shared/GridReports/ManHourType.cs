namespace HCMv3.GridReports;

/// <summary>
/// The two buckets of the Daily Man Hours Summary grid report (legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptDailyManHoursSummary.ascx</c>).
///
/// Legacy held these as the literal strings written into its <c>STATUS_KEY</c> column
/// (<c>Table1()</c>, lines 503-504) and branched on them in <c>GetTable()</c> to pick
/// <c>Table2()</c> (Regular) or <c>Table3()</c> (Overtime), lines 390-401. Each bucket has its OWN
/// detail column set, which is why the port exposes two separate detail endpoints.
/// </summary>
public enum ManHourType
{
    /// <summary>Legacy "Regular": employees with at least one time event on the day.</summary>
    Regular = 1,

    /// <summary>Legacy "Overtime": employees with an EmployeeOvertime record effective on the day.</summary>
    Overtime = 2
}
