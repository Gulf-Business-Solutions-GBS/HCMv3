namespace HCMv3.GridReports;

/// <summary>
/// The five buckets of the Daily Attendance Summary grid report (legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptDailyAttendanceSummary.ascx</c>).
///
/// Legacy held these as the literal strings written into its <c>STATUS_KEY</c> column
/// (<c>Table1()</c>, lines 415-419) and passed the clicked cell's string back into
/// <c>Table2(selectedType)</c>'s switch (lines 535-552). This port passes the enum instead, so an
/// unknown bucket cannot silently select nothing the way a mistyped string would.
/// </summary>
public enum DailyAttendanceType
{
    /// <summary>Legacy "On Duty": the employee has at least one time event on the day.</summary>
    OnDuty = 1,

    /// <summary>Legacy "Day Off": an absence of the Day Off absence type covers the day.</summary>
    DayOff = 2,

    /// <summary>Legacy "Medical": an absence of the Medical absence type covers the day.</summary>
    Medical = 3,

    /// <summary>Legacy "On Leave": a full-day absence of any other absence type covers the day.</summary>
    OnLeave = 4,

    /// <summary>Legacy "Absent".</summary>
    Absent = 5
}
