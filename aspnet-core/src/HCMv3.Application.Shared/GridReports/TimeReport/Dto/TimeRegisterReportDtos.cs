using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.GridReports.Employees.Dto;

namespace HCMv3.GridReports.TimeReport.Dto;

/// <summary>
/// Filter input for the Time Register grid report (legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptTimeRegister.ascx</c>).
///
/// Uses only the common filters -- <c>dtFrom</c>, <c>dtTo</c>, <c>cmbEmpStatus</c>, <c>txtEmpCode</c>
/// (uscRptTimeRegister.ascx.cs:253-260) -- so it adds no fields of its own. Legacy's
/// <c>GetParameterStr()</c> (lines 446-457) logs employee code plus the two dates.
/// </summary>
public class GetTimeRegisterReportInput : GridReportInputBase
{
    /// <summary>
    /// Legacy left the grid in database order. Employee code then day date is the natural reading order
    /// for a day-grain attendance sheet and is deterministic, which server-side paging requires.
    /// </summary>
    protected override string DefaultSorting => "EmployeeCode ASC, DayDate ASC";
}

/// <summary>
/// One grid row of legacy <c>Table1()</c> (uscRptTimeRegister.ascx.cs:461-700).
///
/// Legacy column set, in order: <c>OID</c>, <c>Code</c>, <c>Name</c>, <c>JobTitle</c>,
/// <c>OrgLevel1</c>..<c>OrgLevel7</c>, <c>Date</c>, <c>ShiftStart</c>, <c>ActualSignIn</c>,
/// <c>ShiftEnd</c>, <c>ActualSignOut</c>, <c>TotalWorkingHours</c>, <c>LateIn</c>,
/// <c>DeductionLateIn</c>, <c>EarlyOut</c>, <c>DeductionEarlyOut</c>,
/// <c>DeductionAfterMonthlyTolerance</c>, <c>TotalLateMinutes</c>, <c>AbsentDay</c>, <c>Country</c>,
/// <c>WorkLocation</c>.
///
/// SIX OF THOSE COLUMNS ARE STRINGS, NOT NUMBERS, IN LEGACY. <c>ShiftStart</c>, <c>ActualSignIn</c>,
/// <c>ShiftEnd</c>, <c>ActualSignOut</c>, <c>LateIn</c>, <c>DeductionLateIn</c>, <c>EarlyOut</c> and
/// <c>DeductionEarlyOut</c> all fall back to the FRIDAY/SATURDAY DUTY NAME (e.g. "Weekly Off") when the
/// underlying value is unset/zero and the day is a Friday or a Saturday (lines 597-641). That mixed
/// string/number cell is the legacy contract and is reproduced verbatim; the raw numbers are also
/// exposed so the grid can sort on them.
/// </summary>
public class TimeRegisterReportRowDto : EntityDto<int>, IEmployeeOrgLevelRow
{
    /// <summary>Legacy "Code" column -- <c>rec.Employee.Code</c>.</summary>
    public string EmployeeCode { get; set; }

    /// <summary>
    /// Legacy "Name" column. LEGACY FIDELITY: line 583 reads <c>rec.Employee.FullNameEn</c>
    /// UNCONDITIONALLY -- unlike every other column on this screen it is never switched to the Arabic
    /// name. Reproduced as-is (pinned by a test).
    /// </summary>
    public string EmployeeName { get; set; }

    /// <summary>
    /// Legacy "JobTitle" column -- from the org assignment current as of TODAY (line 578 keys the lookup
    /// on <c>DateTime.Now</c>, not on the report window). Empty string, not null, when absent.
    /// </summary>
    public string JobTitleName { get; set; }

    /// <summary>
    /// Legacy <c>OrgLevel1</c>..<c>OrgLevel7</c> columns. HCMv3 stores ONE
    /// <c>EmployeeOrgAssignment.OrganizationUnitId</c> into Zero's hierarchical OrganizationUnit tree, so
    /// each level is the ancestor-or-self of that leaf classified at the matching level. Stamped after
    /// materialization by <c>EmployeeOrgLevelNameResolver</c> and therefore NOT server-sortable.
    /// </summary>
    public string OrgLevel1 { get; set; }

    public string OrgLevel2 { get; set; }

    public string OrgLevel3 { get; set; }

    public string OrgLevel4 { get; set; }

    public string OrgLevel5 { get; set; }

    public string OrgLevel6 { get; set; }

    public string OrgLevel7 { get; set; }

    /// <summary>Leaf org unit the seven levels are resolved from. See <see cref="IEmployeeOrgLevelRow"/>.</summary>
    public long? OrganizationUnitId { get; set; }

    /// <summary>Legacy "Date" column -- <c>rec.DayDate</c>.</summary>
    public DateTime DayDate { get; set; }

    /// <summary>Legacy "ShiftStart" column -- see the class remarks on the string/number fallback.</summary>
    public string ShiftStart { get; set; }

    /// <summary>Legacy "ActualSignIn" column.</summary>
    public string ActualSignIn { get; set; }

    /// <summary>Legacy "ShiftEnd" column.</summary>
    public string ShiftEnd { get; set; }

    /// <summary>Legacy "ActualSignOut" column.</summary>
    public string ActualSignOut { get; set; }

    /// <summary>
    /// Legacy "TotalWorkingHours" column -- <c>ActualWorkingMins != 0 ? Round(ActualWorkingMins/60, 2) : 0</c>
    /// (line 617).
    /// </summary>
    public double TotalWorkingHours { get; set; }

    /// <summary>Legacy "LateIn" column (string/duty-name fallback).</summary>
    public string LateIn { get; set; }

    /// <summary>Legacy "DeductionLateIn" column (string/duty-name fallback).</summary>
    public string DeductionLateIn { get; set; }

    /// <summary>Legacy "EarlyOut" column (string/duty-name fallback).</summary>
    public string EarlyOut { get; set; }

    /// <summary>Legacy "DeductionEarlyOut" column (string/duty-name fallback).</summary>
    public string DeductionEarlyOut { get; set; }

    /// <summary>
    /// Legacy "DeductionAfterMonthlyTolerance" column -- <c>DedAfterMonthlyTol.ToString("#.##")</c> when
    /// non-zero, else EMPTY STRING (line 643; note this one does NOT get the duty-name fallback).
    /// </summary>
    public string DeductionAfterMonthlyTolerance { get; set; }

    /// <summary>
    /// Legacy "TotalLateMinutes" column -- <c>LateIn + EarlyOut</c> when both are non-zero, else whichever
    /// is non-zero, else 0 (lines 638-640).
    /// </summary>
    public double TotalLateMinutes { get; set; }

    /// <summary>
    /// Legacy "AbsentDay" column, a BOOLEAN (line 644): when
    /// <c>ManageTimeSettings.EnableTimeRegisterApproval</c> is OFF it is <c>AbsentDay == 1</c>, when ON it
    /// is <c>ApprovedAbsentDay == 1</c>.
    /// </summary>
    public bool AbsentDay { get; set; }

    /// <summary>
    /// Legacy "Country" column -- the PayrollArea country of the org assignment current as of TODAY.
    /// Empty string, not null, when absent.
    /// </summary>
    public string CountryName { get; set; }

    /// <summary>
    /// Legacy "WorkLocation" column -- from the same org assignment. Empty string, not null, when absent.
    /// </summary>
    public string WorkLocationName { get; set; }

    // --- raw values behind the mixed string/number columns (not legacy columns; see class remarks) ---

    /// <summary>Raw <c>TimeRegister.LateIn</c> minutes behind <see cref="LateIn"/>.</summary>
    public double LateInMinutes { get; set; }

    /// <summary>Raw <c>TimeRegister.DeductionLateIn</c> minutes behind <see cref="DeductionLateIn"/>.</summary>
    public double DeductionLateInMinutes { get; set; }

    /// <summary>Raw <c>TimeRegister.EarlyOut</c> minutes behind <see cref="EarlyOut"/>.</summary>
    public double EarlyOutMinutes { get; set; }

    /// <summary>Raw <c>TimeRegister.DeductionEarlyOut</c> minutes behind <see cref="DeductionEarlyOut"/>.</summary>
    public double DeductionEarlyOutMinutes { get; set; }

    /// <summary>Raw <c>TimeRegister.DedAfterMonthlyTol</c> minutes.</summary>
    public double DedAfterMonthlyTolMinutes { get; set; }

    /// <summary>Raw <c>TimeRegister.ShiftStartTime1</c> behind <see cref="ShiftStart"/>.</summary>
    public DateTime ShiftStartTime { get; set; }

    /// <summary>Raw <c>TimeRegister.ActualStartTime1</c> behind <see cref="ActualSignIn"/>.</summary>
    public DateTime ActualStartTime { get; set; }

    /// <summary>Raw <c>TimeRegister.ShiftEndTime1</c> behind <see cref="ShiftEnd"/>.</summary>
    public DateTime ShiftEndTime { get; set; }

    /// <summary>Raw <c>TimeRegister.ActualEndTime1</c> behind <see cref="ActualSignOut"/>.</summary>
    public DateTime ActualEndTime { get; set; }

    /// <summary>
    /// The <c>DutyPeriod.FridayDuty</c>/<c>SaturdayDuty</c> name used as the fallback text of the eight
    /// mixed string/number columns, or empty on any other weekday. Legacy read
    /// <c>rec.DutyPeriod.FridayDuty.NameEn</c>/<c>SaturdayDuty.NameEn</c> inline in each of those eight
    /// ternaries (lines 597-641); it is resolved once here and applied during the same post-materialization
    /// stamping pass that fills OrgLevel1..7.
    /// </summary>
    public string WeekendDutyName { get; set; }
}
