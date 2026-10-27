using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.GridReports.Employees.Dto;

namespace HCMv3.GridReports.TimeReport.Dto;

/// <summary>
/// Filter input for the Time Schedule analysis grid report (legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptTimeSchedule.ascx</c>).
///
/// The legacy panel is dtFrom/dtTo/txtEmpCode only -- its status combo filter is COMMENTED OUT in
/// <c>Table1()</c> (uscRptTimeSchedule.ascx.cs:562-568) and <c>GetParameterStr()</c> (lines 539-550) never
/// logs it. The inherited <c>EmployeeStatus</c> is therefore UNUSED by this report and the Angular screen
/// hides the field rather than forking the shared filter panel.
/// </summary>
public class GetTimeScheduleReportInput : GridReportInputBase
{
    /// <summary>
    /// Legacy filled the DataTable date-by-date and, within a date, in the natural order of the
    /// EmployeeDutyPeriod query (no explicit sort). Date then employee code is used here so server-side
    /// paging is deterministic and reads like the legacy grid did.
    /// </summary>
    protected override string DefaultSorting => "Date ASC, EmployeeCode ASC";
}

/// <summary>
/// One grid row -- one (day, employee duty period) pair. Mirrors the legacy <c>DataTable</c> built in
/// <c>uscRptTimeSchedule.ascx.cs:480-634</c> column-for-column: OID, Code, Employee, JobTitle,
/// OrgLevel1..OrgLevel7, Date, Day, DutyPeriod, Duty, StartTime, EndTime, Country, WorkLocation.
///
/// LEGACY DEFECT REPRODUCED (line 589): the "Employee" cell is filled from <c>emp.FullNameEn</c>
/// unconditionally, so this one report shows the ENGLISH name even in the Arabic aspect while every other
/// column on it switches language. Pinned by
/// <c>Should_Always_Use_English_Employee_Name_Reproducing_Legacy_Defect</c>.
///
/// OrgLevel1..OrgLevel7 are kept as SEVEN columns, exactly as legacy rendered them; see
/// <c>EmployeeOrgLevelNameResolver</c> for why they are resolved after materialization.
///
/// Legacy emitted <c>StartTime</c>/<c>EndTime</c> as pre-formatted <c>ToShortTimeString()</c> strings; they
/// are real <see cref="TimeSpan"/> values here (HCMv3's <c>Duty.StartTime</c>/<c>EndTime</c> are
/// <c>TimeSpan</c>) and are formatted in the Angular grid instead.
/// </summary>
public class TimeScheduleReportRowDto : EntityDto<int>, IEmployeeOrgLevelRow
{
    /// <summary>
    /// Legacy "Code" column -- <c>emp.Code</c>. (The inherited <c>Id</c> carries the legacy "OID" column,
    /// which on this report is <c>emp.Oid</c>, the EMPLOYEE id rather than the duty-period row id -- line
    /// 587. It is therefore NOT unique across rows here, one row per employee per day.)
    /// </summary>
    public string EmployeeCode { get; set; }

    /// <summary>Legacy "Employee" column. Always the English name -- see the class remarks.</summary>
    public string EmployeeName { get; set; }

    /// <summary>Legacy "JobTitle" column -- empty string (not null) when there is none.</summary>
    public string JobTitleName { get; set; }

    public string OrgLevel1 { get; set; }

    public string OrgLevel2 { get; set; }

    public string OrgLevel3 { get; set; }

    public string OrgLevel4 { get; set; }

    public string OrgLevel5 { get; set; }

    public string OrgLevel6 { get; set; }

    public string OrgLevel7 { get; set; }

    public long? OrganizationUnitId { get; set; }

    /// <summary>The calendar day this row was generated for.</summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Legacy "Day" column -- <c>dtFromDate.DayOfWeek.ToString()</c> (line 601), i.e. the INVARIANT English
    /// day name with no localization even in the Arabic aspect. Reproduced verbatim.
    /// </summary>
    public string Day { get; set; }

    /// <summary>Legacy "DutyPeriod" column -- empty string (not null) when there is none.</summary>
    public string DutyPeriodName { get; set; }

    /// <summary>
    /// Legacy "Duty" column. Null when no duty could be resolved for the day (legacy left the cell
    /// untouched, i.e. <c>DBNull</c>, in that case -- lines 604-609).
    /// </summary>
    public string DutyName { get; set; }

    /// <summary>Null when no duty could be resolved for the day; see <see cref="DutyName"/>.</summary>
    public TimeSpan? StartTime { get; set; }

    /// <summary>Null when no duty could be resolved for the day; see <see cref="DutyName"/>.</summary>
    public TimeSpan? EndTime { get; set; }

    /// <summary>
    /// Legacy "Country" column -- the PayrollArea Country of the org assignment current AS OF THIS ROW'S
    /// DAY (lines 611-614). Null, not empty string, when there is none (legacy wrote null here).
    /// </summary>
    public string CountryName { get; set; }

    /// <summary>
    /// Legacy "WorkLocation" column -- from the same assignment as <see cref="CountryName"/>, which legacy
    /// selected with <c>PayrollArea != null</c> in the predicate (line 615), so an assignment without a
    /// payroll area yields no work location either. Null, not empty string, when there is none.
    /// </summary>
    public string WorkLocationName { get; set; }
}
