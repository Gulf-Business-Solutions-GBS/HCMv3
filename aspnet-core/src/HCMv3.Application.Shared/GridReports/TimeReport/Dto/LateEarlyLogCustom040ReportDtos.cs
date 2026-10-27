using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.GridReports.Employees.Dto;

namespace HCMv3.GridReports.TimeReport.Dto;

/// <summary>
/// Filter input for the Custom040 Late/Early Log grid report (legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptLateEarlyLog_Custom040.ascx</c>).
///
/// The filter panel is IDENTICAL to the base control's: dtFrom/dtTo/cmbPayrollArea/txtEmpCode, with no
/// employee-status combo (the query hard-codes <c>Status == EmployeeStatus.Active</c>,
/// uscRptLateEarlyLog_Custom040.ascx.cs:759). The inherited <c>EmployeeStatus</c> is therefore unused and
/// the Angular screen hides it rather than forking the shared filter panel.
/// </summary>
public class GetLateEarlyLogCustom040ReportInput : GridReportInputBase
{
    /// <summary>
    /// Legacy <c>cmbPayrollArea</c> (value = <c>PayrollArea.Code</c>). Null/empty means "all payroll
    /// areas". Legacy matched an employee having an org assignment covering <c>DateTime.Now.Date</c> --
    /// TODAY, not the report window (lines 761-768).
    /// </summary>
    public string PayrollAreaCode { get; set; }

    /// <summary>
    /// Legacy applied no sort; it filled the DataTable employee-by-employee then day-by-day. Employee code
    /// then date keeps server-side paging deterministic and matches the legacy reading order.
    /// </summary>
    protected override string DefaultSorting => "EmployeeCode ASC, Date ASC";
}

/// <summary>
/// One grid row of legacy <c>uscRptLateEarlyLog_Custom040.ascx.cs</c> <c>Table1()</c> (lines 622-1400).
///
/// RELATIONSHIP TO THE BASE ROW: Custom040 is an OLDER, break-less fork of the base control. Against
/// <see cref="LateEarlyLogReportRowDto"/> it DROPS three columns -- <c>Break Out</c>, <c>Break In</c> and
/// <c>Break Hours</c> (they are simply not added to the DataTable, lines 708-721, and the whole
/// "BreakIn &amp; BreakOut From TimeEvent" region is absent) -- and adds none. Everything else is the same
/// column set in the same order: Oid, Code, Employee, AccessId, OrgLevel1..7, Date, WeekDay, Shift Start,
/// Shift End, Sign In, Sign Out, Early In, Early Out, Late In, Late Out, Total Hours, Planned Hours,
/// Overtime Hours, Absence Hours, Status, LeaveType, LeaveHours, PunchInAccessUnit, PunchOutAccessUnit,
/// Time Policy, WorkLocation, Country, PunchInLocation, PunchOutLocation.
///
/// The VALUES behind several of those columns differ from the base -- see the app service remarks -- most
/// visibly <see cref="Status"/>, which uses this customer's own vocabulary.
/// </summary>
public class LateEarlyLogCustom040ReportRowDto : EntityDto<int>, IEmployeeOrgLevelRow
{
    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    public string AccessId { get; set; }

    public string OrgLevel1 { get; set; }

    public string OrgLevel2 { get; set; }

    public string OrgLevel3 { get; set; }

    public string OrgLevel4 { get; set; }

    public string OrgLevel5 { get; set; }

    public string OrgLevel6 { get; set; }

    public string OrgLevel7 { get; set; }

    public long? OrganizationUnitId { get; set; }

    public DateTime Date { get; set; }

    /// <summary>
    /// Legacy "WeekDay" -- the day name through a hard-coded English/Arabic literal table, not a resource
    /// lookup. Reproduced.
    /// </summary>
    public string WeekDay { get; set; }

    /// <summary>
    /// Legacy "Shift Start". Custom040 keeps the base's STICKY SHIFT DEFECT (the local lives outside both
    /// loops and is only reassigned when a Duty resolves) -- see the app service remarks.
    /// </summary>
    public TimeSpan ShiftStart { get; set; }

    /// <summary>Legacy "Shift End". See <see cref="ShiftStart"/>.</summary>
    public TimeSpan ShiftEnd { get; set; }

    public TimeSpan? SignIn { get; set; }

    public TimeSpan? SignOut { get; set; }

    /// <summary>
    /// Legacy "Early In" -- shift start minus punch-in. Custom040 compares TIME-OF-DAY only
    /// (line 610: <c>shiftStartTime.TimeOfDay &gt; te_PunchIn.TransTime.TimeOfDay</c>) where the base
    /// compares full date-times, so a night shift's cross-midnight punch is measured against the wrong
    /// day. Reproduced and pinned by a test.
    /// </summary>
    public TimeSpan? EarlyIn { get; set; }

    /// <summary>Legacy "Early Out". Time-of-day comparison, as <see cref="EarlyIn"/>.</summary>
    public TimeSpan? EarlyOut { get; set; }

    /// <summary>Legacy "Late In". Time-of-day comparison, as <see cref="EarlyIn"/>.</summary>
    public TimeSpan? LateIn { get; set; }

    /// <summary>Legacy "Late Out". Time-of-day comparison, as <see cref="EarlyIn"/>.</summary>
    public TimeSpan? LateOut { get; set; }

    /// <summary>Legacy "Total Hours" -- punch-out minus punch-in, with NO break deduction.</summary>
    public TimeSpan? TotalHours { get; set; }

    /// <summary>
    /// Legacy "Planned Hours" -- the raw shift length, with NO break deduction and computed from the
    /// duty's bare start/end times rather than from day-anchored date-times.
    /// </summary>
    public TimeSpan? PlannedHours { get; set; }

    public TimeSpan? OvertimeHours { get; set; }

    public TimeSpan? AbsenceHours { get; set; }

    /// <summary>
    /// Legacy "Status". THE SIGNATURE CUSTOM040 CHANGE (lines 678-690): instead of the base's
    /// "Missed In"/"Missed Out" pair this customer reports <c>"Present with Single Punch"</c> when exactly
    /// one of the two punch lists is non-empty and <c>"No Punch Found"</c> when both are empty. The
    /// week-off / public-holiday / leave / no-return-date literals are unchanged from the base.
    /// </summary>
    public string Status { get; set; }

    public string LeaveType { get; set; }

    public TimeSpan? PartialLeaveHours { get; set; }

    public string PunchInAccessUnit { get; set; }

    public string PunchOutAccessUnit { get; set; }

    public string TimePolicyName { get; set; }

    public string WorkLocationName { get; set; }

    public string CountryName { get; set; }

    public string PunchInLocation { get; set; }

    public string PunchOutLocation { get; set; }
}
