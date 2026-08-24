using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.GridReports.Employees.Dto;

namespace HCMv3.GridReports.TimeReport.Dto;

/// <summary>
/// Filter input for LEVEL 1 of the Daily Man Hours Summary analysis grid report (legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptDailyManHoursSummary.ascx</c>) -- the
/// two-bucket (Regular / Overtime) headcount for ONE day.
///
/// SINGLE-DATE REPORT: legacy's panel is one date picker plus <c>cmbWorkLocation</c>, logged as
/// "Work Location" / "Effective Date" (<c>GetParameterStr()</c>, lines 429-439). The date maps onto
/// <see cref="GridReportInputBase.FromDate"/>. <see cref="GridReportInputBase.ToDate"/>,
/// <see cref="GridReportInputBase.EmployeeCode"/> and <see cref="GridReportInputBase.EmployeeStatus"/>
/// are INHERITED-BUT-UNUSED -- legacy has none of them on this screen.
/// </summary>
public class GetDailyManHoursSummaryReportInput : GridReportInputBase
{
    /// <summary>
    /// Legacy <c>cmbWorkLocation</c>, whose value was <c>WorkLocation.Code</c>. Null/blank means "all
    /// work locations". Legacy resolved it to the employees whose org assignment covering the reported
    /// day carries that work location (lines 450-463).
    /// </summary>
    public string WorkLocationCode { get; set; }

    /// <summary>Legacy emitted Regular then Overtime; sorting by the key preserves that order.</summary>
    protected override string DefaultSorting => "Type ASC";
}

/// <summary>
/// Filter input for LEVEL 2 -- shared by BOTH detail endpoints, because legacy's <c>Table2()</c>
/// (Regular) and <c>Table3()</c> (Overtime) take exactly the same filters and differ only in their
/// column set (lines 509-729).
/// </summary>
public class GetDailyManHoursSummaryDetailReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>cmbWorkLocation</c> -- see <see cref="GetDailyManHoursSummaryReportInput"/>.</summary>
    public string WorkLocationCode { get; set; }

    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>
/// LEVEL 1 row. Mirrors legacy <c>Table1()</c>'s three-column DataTable (lines 464-467):
/// STATUS_KEY (the grid key), Man Hour Type, Emps.
/// </summary>
public class DailyManHoursSummaryReportRowDto
{
    /// <summary>Legacy "STATUS_KEY" column and grid key (<c>GetKey()</c>, line 362).</summary>
    public ManHourType Type { get; set; }

    /// <summary>
    /// Legacy "Man Hour Type" column: the English literals "Regular" / "Overtime" (lines 503-504),
    /// written identically in both aspects.
    /// </summary>
    public string TypeName { get; set; }

    /// <summary>
    /// Legacy "Emps" column: the count of DISTINCT employees with a time event (Regular) or an
    /// EmployeeOvertime record (Overtime) on the day (lines 480-501).
    /// </summary>
    public int EmployeeCount { get; set; }
}

/// <summary>
/// LEVEL 2 row for the REGULAR bucket. Mirrors legacy <c>Table2()</c>'s DataTable (lines 545-557):
/// OID, EmployeeCode, EmployeeName, OrgLevel1..7, Sign In, Sign Out, Total Hours.
/// </summary>
public class DailyManHoursRegularDetailRowDto : EntityDto<int>, IEmployeeOrgLevelRow
{
    /// <summary>Legacy "EmployeeCode" column (line 607).</summary>
    public string EmployeeCode { get; set; }

    /// <summary>
    /// Legacy "EmployeeName" column (line 608). Legacy always used <c>FullNameEn</c>; this port resolves
    /// the name from the request culture, matching the rest of the ported family.
    /// </summary>
    public string EmployeeName { get; set; }

    /// <summary>
    /// Legacy "Sign In" column (line 616): the day's EARLIEST clock-in, rendered <c>hh\:mm</c> -- a
    /// 12-HOUR clock with no AM/PM marker, so 13:30 reads "01:30". Reproduced verbatim; "" when the day
    /// has no clock-in at all.
    /// </summary>
    public string SignIn { get; set; }

    /// <summary>Legacy "Sign Out" column (line 617): the day's LATEST clock-out, same format.</summary>
    public string SignOut { get; set; }

    /// <summary>
    /// Legacy "Total Hours" column (lines 618-625): <c>lastOut - firstIn</c>, i.e. the span from first
    /// in to last out including every break, and null (legacy <c>DBNull</c>) when either end is missing.
    /// Rendered with <c>TimeSpan.ToString()</c>, e.g. "08:30:00".
    /// </summary>
    public string TotalHours { get; set; }

    /// <summary>Raw earliest clock-in behind <see cref="SignIn"/>; null when there is none.</summary>
    public DateTime? FirstIn { get; set; }

    /// <summary>Raw latest clock-out behind <see cref="SignOut"/>; null when there is none.</summary>
    public DateTime? LastOut { get; set; }

    public string OrgLevel1 { get; set; }

    public string OrgLevel2 { get; set; }

    public string OrgLevel3 { get; set; }

    public string OrgLevel4 { get; set; }

    public string OrgLevel5 { get; set; }

    public string OrgLevel6 { get; set; }

    public string OrgLevel7 { get; set; }

    /// <summary>
    /// Leaf org unit the OrgLevel columns are stamped from. Legacy read the seven discrete
    /// <c>Employee.OrgLevelN</c> FKs (lines 609-615); HCMv3 resolves them from the org assignment
    /// current on the reported day.
    /// </summary>
    public long? OrganizationUnitId { get; set; }
}

/// <summary>
/// LEVEL 2 row for the OVERTIME bucket. Mirrors legacy <c>Table3()</c>'s DataTable (lines 667-678):
/// OID, EmployeeCode, EmployeeName, OrgLevel1..7, OvertimeHours, AccumulatedOTHours.
///
/// GRAIN WARNING: one row per <c>EmployeeOvertime</c> RECORD, not per employee -- legacy loops the
/// overtime records (line 692) and writes the employee id into the OID column, so an employee with two
/// overtime records on the day yields two rows carrying the same <see cref="EntityDto{TPrimaryKey}.Id"/>.
/// Reproduced.
/// </summary>
public class DailyManHoursOvertimeDetailRowDto : EntityDto<int>, IEmployeeOrgLevelRow
{
    /// <summary>Legacy "EmployeeCode" column (line 713).</summary>
    public string EmployeeCode { get; set; }

    /// <summary>
    /// Legacy "EmployeeName" column (line 714). Legacy always used <c>FullNameEn</c>; this port resolves
    /// the name from the request culture, matching the rest of the ported family.
    /// </summary>
    public string EmployeeName { get; set; }

    /// <summary>
    /// Legacy "OvertimeHours" column (line 722): this record's <c>ApprovedOvertimeHours</c> -- the
    /// APPROVED figure, not the actual or requested one.
    /// </summary>
    public double OvertimeHours { get; set; }

    /// <summary>
    /// Legacy "AccumulatedOTHours" column (lines 702-708/723): the employee's total approved overtime
    /// from the FIRST OF THE REPORTED MONTH through the reported day. Note it is NOT narrowed by the
    /// work-location filter -- legacy queried it without that predicate. Reproduced.
    /// </summary>
    public double AccumulatedOvertimeHours { get; set; }

    public string OrgLevel1 { get; set; }

    public string OrgLevel2 { get; set; }

    public string OrgLevel3 { get; set; }

    public string OrgLevel4 { get; set; }

    public string OrgLevel5 { get; set; }

    public string OrgLevel6 { get; set; }

    public string OrgLevel7 { get; set; }

    /// <summary>Leaf org unit the OrgLevel columns are stamped from -- see the Regular row DTO.</summary>
    public long? OrganizationUnitId { get; set; }
}
