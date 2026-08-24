using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.GridReports.Employees.Dto;

namespace HCMv3.GridReports.TimeReport.Dto;

/// <summary>
/// The legacy <c>cmbVarient</c> combo of <c>uscRptLateEarlyLog_Custom019.ascx</c>, filled at
/// <c>uscRptLateEarlyLog_Custom019.ascx.cs:298-304</c>. This is the ONLY Late/Early Log variant that has
/// one, and it changes both the grain and the column set of the report, not just a filter.
/// </summary>
public enum LateEarlyLogCustom019Variant
{
    /// <summary>Legacy "Detail" -- one row per (employee, day), the full column set.</summary>
    Detail = 0,

    /// <summary>
    /// Legacy "Summary" -- the detail rows regrouped by (Code, Employee, Shift Start, Shift End) with the
    /// four early/late durations and Total Hours summed (lines 1660-1730).
    /// </summary>
    Summary = 1,

    /// <summary>
    /// Legacy "Summary (LateIn &gt; 3 Hours)" -- the same summary, then keeping only groups whose SUMMED
    /// Late In exceeds three hours (line 1698).
    /// </summary>
    SummaryLateInOverThreeHours = 2
}

/// <summary>
/// Filter input for the Late/Early Log (Custom 019) analysis grid report (legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptLateEarlyLog_Custom019.ascx</c>).
///
/// ATYPICAL FILTER PANEL. Where the base control and the other three variants filter on
/// <c>cmbPayrollArea</c>, Custom019 replaces it with THREE org-hierarchy combos --
/// <c>cmbDepartment</c>/<c>cmbDivision</c>/<c>cmbSection</c>, matching legacy <c>OrgLevel2</c>,
/// <c>OrgLevel3</c> and <c>OrgLevel4</c> codes respectively (lines 776-800) -- plus the
/// <c>cmbVarient</c> combo. It has no payroll-area filter at all.
///
/// Like every Late/Early Log control there is NO employee-status combo: the query hard-codes
/// <c>Status == EmployeeStatus.Active</c>, so the inherited <c>EmployeeStatus</c> is UNUSED and the
/// Angular screen hides the field rather than forking the shared filter panel.
/// </summary>
public class GetLateEarlyLogCustom019ReportInput : GridReportInputBase
{
    /// <summary>
    /// Legacy <c>cmbDepartment</c> -- an <c>OrgLevel2</c> unit CODE. Null/empty means "all". Legacy
    /// matched an employee having an org assignment covering <c>DateTime.Now.Date</c> -- TODAY, not the
    /// report window -- whose OrgLevel2 has that code (lines 776-782).
    /// </summary>
    public string DepartmentCode { get; set; }

    /// <summary>Legacy <c>cmbDivision</c> -- an <c>OrgLevel3</c> unit CODE. See <see cref="DepartmentCode"/>.</summary>
    public string DivisionCode { get; set; }

    /// <summary>Legacy <c>cmbSection</c> -- an <c>OrgLevel4</c> unit CODE. See <see cref="DepartmentCode"/>.</summary>
    public string SectionCode { get; set; }

    /// <summary>
    /// Legacy <c>cmbVarient</c>, whose <c>SelectedIndex = 0</c> default is "Detail" (line 304).
    /// </summary>
    public LateEarlyLogCustom019Variant Variant { get; set; } = LateEarlyLogCustom019Variant.Detail;

    /// <summary>
    /// Legacy applied no sort: it filled the DataTable employee-by-employee then day-by-day. Employee code
    /// then date is used here so server-side paging is deterministic and reads in the order the legacy
    /// grid did. The summary variants have no Date column, so they sort by employee code alone.
    /// </summary>
    protected override string DefaultSorting =>
        Variant == LateEarlyLogCustom019Variant.Detail
            ? "EmployeeCode ASC, Date ASC"
            : "EmployeeCode ASC";
}

/// <summary>
/// One grid row of the Late/Early Log (Custom 019) report.
///
/// COLUMN SET (legacy lines 824-865, reading past the commented-out <c>table.Columns.Add</c> calls):
/// Oid, Code, Employee, OrgLevel2..OrgLevel7, Date, WeekDay, Shift Start, Shift End, Sign In, Sign Out,
/// Early In, Early Out, Late In, Late Out, Total Hours, Planned Hours, Overtime Hours, Absence Hours,
/// Status, LeaveType, PartialLeaveHours, Time Policy, WorkLocation.
///
/// Relative to the base control this variant DROPS eight columns that legacy commented out rather than
/// deleted: AccessId, OrgLevel1, Break Out, Break In, Break Hours, PunchInAccessUnit,
/// PunchOutAccessUnit, Country, PunchInLocation and PunchOutLocation. <see cref="OrgLevel1"/> and
/// <see cref="OrganizationUnitId"/> are still present on the DTO because
/// <see cref="IEmployeeOrgLevelRow"/> requires the whole set for the shared org-level stamping; OrgLevel1
/// is simply never rendered or exported.
///
/// In the SUMMARY variants only Code, Employee, Shift Start, Shift End, the four early/late durations and
/// Total Hours are populated (legacy built a narrower <c>sumTable</c>, lines 1660-1730); every other
/// property stays null.
///
/// Legacy emitted the punch/shift cells as PRE-FORMATTED STRINGS and the duration cells as
/// <c>TimeSpan</c>. Here punches and shift bounds are <see cref="TimeSpan"/> time-of-day values and the
/// durations are nullable (legacy left an unset duration cell as <c>DBNull</c>, which is null here, NOT
/// <c>TimeSpan.Zero</c>) -- formatting happens in the Angular grid, which is what makes server-side
/// sorting on them correct.
/// </summary>
public class LateEarlyLogCustom019ReportRowDto : EntityDto<int>, IEmployeeOrgLevelRow
{
    public string EmployeeCode { get; set; }

    /// <summary>FullNameAr when Arabic, else FullNameEn.</summary>
    public string EmployeeName { get; set; }

    /// <summary>Not rendered by this variant (legacy line 826 is commented out); see the class remarks.</summary>
    public string OrgLevel1 { get; set; }

    public string OrgLevel2 { get; set; }

    public string OrgLevel3 { get; set; }

    public string OrgLevel4 { get; set; }

    public string OrgLevel5 { get; set; }

    public string OrgLevel6 { get; set; }

    public string OrgLevel7 { get; set; }

    public long? OrganizationUnitId { get; set; }

    /// <summary>Null on the summary variants, which have no Date column.</summary>
    public DateTime? Date { get; set; }

    /// <summary>The day name, localized through legacy's hard-coded literal table. Null on summaries.</summary>
    public string WeekDay { get; set; }

    /// <summary>
    /// Legacy "Shift Start". See the app service remarks on the STICKY SHIFT DEFECT: on a day with no
    /// resolvable Duty legacy re-used the previously resolved shift bounds, and so does this port.
    /// </summary>
    public TimeSpan ShiftStart { get; set; }

    /// <summary>Legacy "Shift End". See <see cref="ShiftStart"/>.</summary>
    public TimeSpan ShiftEnd { get; set; }

    public TimeSpan? SignIn { get; set; }

    public TimeSpan? SignOut { get; set; }

    /// <summary>Shift start minus punch-in, only when the day is not a week off. Summed on a summary row.</summary>
    public TimeSpan? EarlyIn { get; set; }

    /// <summary>Shift end minus punch-out, only when the day is not a week off. Summed on a summary row.</summary>
    public TimeSpan? EarlyOut { get; set; }

    /// <summary>Punch-in minus shift start, only when the day is not a week off. Summed on a summary row.</summary>
    public TimeSpan? LateIn { get; set; }

    /// <summary>Punch-out minus shift end, only when the day is not a week off. Summed on a summary row.</summary>
    public TimeSpan? LateOut { get; set; }

    public TimeSpan? TotalHours { get; set; }

    public TimeSpan? PlannedHours { get; set; }

    public TimeSpan? OvertimeHours { get; set; }

    public TimeSpan? AbsenceHours { get; set; }

    /// <summary>
    /// One of the hard-coded literals "Leave"/"No Return Date"/"Week Off"/"Public Holiday" (each with an
    /// Arabic twin), or "Missed In"/"Missed Out"/"Missed In Missed Out" (English only in legacy --
    /// reproduced), or "" for a clean day.
    /// </summary>
    public string Status { get; set; }

    public string LeaveType { get; set; }

    /// <summary>Set only for an absence with non-zero AbsenceHours.</summary>
    public TimeSpan? PartialLeaveHours { get; set; }

    public string TimePolicyName { get; set; }

    public string WorkLocationName { get; set; }
}
