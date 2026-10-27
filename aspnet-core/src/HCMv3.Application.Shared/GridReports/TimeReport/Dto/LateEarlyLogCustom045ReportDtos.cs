using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.GridReports.Employees.Dto;

namespace HCMv3.GridReports.TimeReport.Dto;

/// <summary>
/// Filter input for the Custom045 Late/Early Log grid report (legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptLateEarlyLog_Custom045.ascx</c>).
///
/// Same panel as the base: dtFrom/dtTo/cmbPayrollArea/txtEmpCode, employees hard-filtered to
/// <c>EmployeeStatus.Active</c>, so the inherited <c>EmployeeStatus</c> is unused.
///
/// NOT PORTED -- THE ESS ENTRY POINT: the live legacy method is
/// <c>GetTimeLogData(objectSpace, organizationUnitType, organizationUnit, startDate, endDate)</c>
/// (lines 600-1486), a PUBLIC method the grid calls with all-null/default arguments (line 553) but that an
/// ESS screen also calls with an org-unit filter and an explicit window. Only the grid path is a report;
/// the org-unit branch (lines 645-668) belongs to whatever ESS screen calls it and is deliberately not
/// exposed here. When it is invoked, that branch also OVERWRITES dtFrom/dtTo with the caller's dates.
/// </summary>
public class GetLateEarlyLogCustom045ReportInput : GridReportInputBase
{
    /// <summary>
    /// Legacy <c>cmbPayrollArea</c> (value = <c>PayrollArea.Code</c>), matched against an org assignment
    /// covering TODAY (lines 649-656). Only consulted on the grid path.
    /// </summary>
    public string PayrollAreaCode { get; set; }

    protected override string DefaultSorting => "EmployeeCode ASC, Date ASC";
}

/// <summary>
/// One grid row of legacy <c>uscRptLateEarlyLog_Custom045.ascx.cs</c> <c>GetTimeLogData(...)</c>
/// (lines 600-1486).
///
/// RELATIONSHIP TO THE BASE ROW: Custom045 keeps the base's full column set (including the three break
/// columns) and ADDS TWO: <see cref="OvertimeTypeName"/> and <see cref="OvertimeTypeId"/>, both taken from
/// the resolved Duty's <c>OvertimeType</c> (lines 1018-1019).
///
/// The interesting difference is not the columns but HOW THE PUNCHES ARE CHOSEN -- see the app service
/// remarks. Custom045 abandons the base's clock-in/clock-out tolerance windows entirely in favour of a
/// single flexi window plus FOUR transaction-type codes, "3" and "4" being dedicated break-out and
/// break-in events that the base never reads.
///
/// DEAD LEGACY CODE NOT PORTED: <c>Table1_OptFinal()</c> (lines 1487-2331), <c>Table1_Old()</c>
/// (lines 2332-3151) and <c>GetTimeLogData_Shabbir(...)</c> (lines 3152-4015) are three unreferenced
/// alternative implementations left in the file; <c>GetTable()</c> calls only <c>GetTimeLogData</c>.
/// </summary>
public class LateEarlyLogCustom045ReportRowDto : EntityDto<int>, IEmployeeOrgLevelRow
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

    public string WeekDay { get; set; }

    /// <summary>Legacy "Shift Start". Carries the base's STICKY SHIFT DEFECT -- see the app service.</summary>
    public TimeSpan ShiftStart { get; set; }

    /// <summary>Legacy "Shift End". See <see cref="ShiftStart"/>.</summary>
    public TimeSpan ShiftEnd { get; set; }

    public TimeSpan? SignIn { get; set; }

    public TimeSpan? SignOut { get; set; }

    /// <summary>
    /// Legacy "Break Out" -- the break-start punch. Custom045 reads it from a DEDICATED transaction type,
    /// <c>TransType.Code == "3"</c> (line 1130), not from a clock-out punch inside a break window as the
    /// base does.
    /// </summary>
    public TimeSpan? BreakOut { get; set; }

    /// <summary>Legacy "Break In" -- the break-end punch, <c>TransType.Code == "4"</c> (line 1131).</summary>
    public TimeSpan? BreakIn { get; set; }

    /// <summary>Legacy "Break Hours" -- BreakIn minus BreakOut, only when both punches exist.</summary>
    public TimeSpan? BreakHours { get; set; }

    /// <summary>
    /// Legacy "Early In" -- TIME-OF-DAY comparison against the shift start (line 686), not the base's
    /// full date-time comparison. Reproduced and pinned by a test.
    /// </summary>
    public TimeSpan? EarlyIn { get; set; }

    /// <summary>Legacy "Early Out". Time-of-day comparison, as <see cref="EarlyIn"/>.</summary>
    public TimeSpan? EarlyOut { get; set; }

    /// <summary>Legacy "Late In". Time-of-day comparison, as <see cref="EarlyIn"/>.</summary>
    public TimeSpan? LateIn { get; set; }

    /// <summary>Legacy "Late Out". Time-of-day comparison, as <see cref="EarlyIn"/>.</summary>
    public TimeSpan? LateOut { get; set; }

    /// <summary>Legacy "Total Hours" -- punch-out minus punch-in, minus the measured break.</summary>
    public TimeSpan? TotalHours { get; set; }

    /// <summary>
    /// Legacy "Planned Hours" -- the raw shift length (bare duty times, night shift inverted) minus the
    /// scheduled break-shift length.
    /// </summary>
    public TimeSpan? PlannedHours { get; set; }

    public TimeSpan? OvertimeHours { get; set; }

    /// <summary>
    /// Legacy "Absence Hours". CUSTOM045 DELETED THE <c>absencehours = plannedHours</c> ASSIGNMENT on the
    /// full-day-leave branch (base line 470 vs Custom045 line 494), so a full-day leave now reports ZERO
    /// absence hours rather than the whole planned shift -- and the cell is written only when the absence
    /// type is UNPAID (lines 498-503). Reproduced and pinned by a test.
    /// </summary>
    public TimeSpan? AbsenceHours { get; set; }

    /// <summary>
    /// Legacy "Status". Unlike Custom040 this variant KEEPS the base's "Missed In"/"Missed Out"
    /// vocabulary; the customer's alternative single-punch wording is present but commented out
    /// (lines 767-774).
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

    /// <summary>
    /// Legacy "OvertimeType" column -- <c>duty.OvertimeType.NameEn</c> (line 1018). ALWAYS ENGLISH: legacy
    /// reads <c>NameEn</c> unconditionally on this column even in the Arabic aspect, unlike every other
    /// lookup name on the row. Reproduced.
    /// </summary>
    public string OvertimeTypeName { get; set; }

    /// <summary>
    /// Legacy "OvertimeTypeOid" column -- the technical id of the same OvertimeType, or <c>-1</c> when the
    /// duty has none (line 1019). Nothing in the legacy grid reads it; it exists for the ESS caller. It is
    /// carried on the row for contract fidelity but, like the base's row-sequence <c>Oid</c>, is NOT an
    /// exported column.
    /// </summary>
    public int OvertimeTypeId { get; set; }
}
