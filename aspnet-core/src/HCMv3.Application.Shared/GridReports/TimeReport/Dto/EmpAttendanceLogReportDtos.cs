using System;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.TimeReport.Dto;

/// <summary>
/// Filter input for the Employee Attendance Log analysis grid report (legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptEmpAttendanceLog.ascx</c>).
///
/// Legacy's filter panel is dtFrom / dtTo ONLY (uscRptEmpAttendanceLog.ascx.cs:74-104) -- there is no
/// employee-code box and no status combo on this screen. <see cref="GridReportInputBase.EmployeeCode"/>
/// and <see cref="GridReportInputBase.EmployeeStatus"/> are therefore INHERITED-BUT-UNUSED: legacy
/// hard-codes the employee scope to <c>Status == Active &amp;&amp; CurrentDepartment != null</c>
/// (line 160-161).
/// </summary>
public class GetEmpAttendanceLogReportInput : GridReportInputBase
{
    /// <summary>
    /// Legacy emitted rows in (day, employee, punch) order and the grid had no key at all
    /// (<c>ASPxGridView.DataSource</c> is bound straight to the DataTable, line 116-124). Day then code
    /// is the stable equivalent for server-side paging.
    /// </summary>
    protected override string DefaultSorting => "DayDate ASC, EmployeeCode ASC";
}

/// <summary>
/// One row of the Employee Attendance Log report -- ONE IN/OUT PUNCH PAIR of one employee-day, not one
/// employee-day. Mirrors legacy <c>Table1()</c>'s seven-column DataTable
/// (uscRptEmpAttendanceLog.ascx.cs:191-197): Code, Employee, Date, Sign In, Sign Out, Working Hours,
/// Total Working Hours.
///
/// ATYPICAL FOR THIS FAMILY -- this report is not a query, it is a PUNCH-PAIRING STATE MACHINE over raw
/// <c>TimeEvent</c> rows (lines 205-305), so several of its behaviours are emergent rather than
/// declared. Later TimeReport ports should not copy this shape blindly; see the app service remarks for
/// the exact emergent rules (including the dropped trailing punch).
/// </summary>
public class EmpAttendanceLogReportRowDto
{
    /// <summary>
    /// The employee the pair belongs to. NOT a row key -- an employee-day yields as many rows as it has
    /// punch pairs, and legacy's grid genuinely had no key column.
    /// </summary>
    public int EmployeeId { get; set; }

    /// <summary>Legacy column 0 (line 250).</summary>
    public string EmployeeCode { get; set; }

    /// <summary>
    /// Legacy column 1 (line 254). Legacy always wrote <c>FullNameEn</c>, even under the Arabic aspect;
    /// this port resolves the name from the request culture, matching the decision already taken for the
    /// sibling <c>StaffHeadcountEmployeeReportRowDto</c>.
    /// </summary>
    public string EmployeeName { get; set; }

    /// <summary>The day the pair was punched on -- the loop variable behind <see cref="DateText"/>.</summary>
    public DateTime DayDate { get; set; }

    /// <summary>
    /// Legacy column 2 (line 255): the day rendered <c>"dd MMMM yy"</c> in the CURRENT UI CULTURE. Kept
    /// as a string because that formatting is the legacy column's actual content.
    /// </summary>
    public string DateText { get; set; }

    /// <summary>Legacy column 3 (line 258): the IN punch as "HH:mm".</summary>
    public string SignIn { get; set; }

    /// <summary>Legacy column 4 (line 273): the OUT punch closing this pair, as "HH:mm".</summary>
    public string SignOut { get; set; }

    /// <summary>
    /// Legacy column 5 (line 283): this pair's duration as <c>H:M:S</c> -- UNPADDED, so 8h5m reads
    /// "8:5:0". Reproduced verbatim.
    /// </summary>
    public string WorkingHours { get; set; }

    /// <summary>
    /// Legacy column 6 (lines 284/293): the running total for the WHOLE employee-day, written only onto
    /// the row closed by the day's LAST punch; every other row of the day carries "". Rendered with
    /// <c>TimeSpan.ToString()</c>, i.e. "08:05:00" (and "1.08:05:00" past 24 hours).
    /// </summary>
    public string TotalWorkingHours { get; set; }
}
