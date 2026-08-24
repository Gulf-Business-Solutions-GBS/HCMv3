using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.GridReports.Employees.Dto;

namespace HCMv3.GridReports.TimeReport.Dto;

/// <summary>
/// Filter input for LEVEL 1 of the Daily Attendance Summary analysis grid report (legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptDailyAttendanceSummary.ascx</c>) -- the
/// five-bucket headcount for ONE day.
///
/// SINGLE-DATE REPORT: legacy's panel has one date picker, <c>dtFrom</c>, logged as "Effective Date"
/// (<c>GetParameterStr()</c>, line 379) and stored in <c>Session[AttendanceDateSession]</c>. It maps
/// onto <see cref="GridReportInputBase.FromDate"/> here (blank still means today, as everywhere in this
/// family). <see cref="GridReportInputBase.ToDate"/>, <see cref="GridReportInputBase.EmployeeCode"/> and
/// <see cref="GridReportInputBase.EmployeeStatus"/> are INHERITED-BUT-UNUSED -- legacy has none of them
/// and hard-codes the employee scope to <c>Status == Active</c> (line 390).
/// </summary>
public class GetDailyAttendanceSummaryReportInput : GridReportInputBase
{
    /// <summary>Legacy emitted the five buckets in a fixed order; sorting by the key preserves it.</summary>
    protected override string DefaultSorting => "Type ASC";
}

/// <summary>
/// Filter input for LEVEL 2 -- the employees behind one level-1 bucket. Legacy
/// <c>Table2(selectedType)</c> received the clicked <c>STATUS_KEY</c> string (lines 477/535-552).
/// </summary>
public class GetDailyAttendanceSummaryDetailReportInput : GridReportInputBase
{
    /// <summary>The level-1 bucket that was drilled into. Required.</summary>
    public DailyAttendanceType Type { get; set; }

    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>
/// LEVEL 1 row. Mirrors legacy <c>Table1()</c>'s three-column DataTable (lines 411-413):
/// STATUS_KEY (the grid key), Type, Total Emp.
/// </summary>
public class DailyAttendanceSummaryReportRowDto
{
    /// <summary>Legacy "STATUS_KEY" column and grid key (<c>GetKey()</c>, line 315).</summary>
    public DailyAttendanceType Type { get; set; }

    /// <summary>
    /// Legacy "Type" column. Legacy wrote the same ENGLISH literal ("On Duty", "Day Off", "Medical",
    /// "On Leave", "Absent") in both aspects -- only the column CAPTION was translated (lines 404-408).
    /// Reproduced: the value is the English literal.
    /// </summary>
    public string TypeName { get; set; }

    /// <summary>Legacy "Total Emp" column -- the bucket's employee count (lines 426-440).</summary>
    public int TotalEmployees { get; set; }
}

/// <summary>
/// LEVEL 2 row. Mirrors legacy <c>Table2()</c>'s DataTable (lines 504-518): OID, EmployeeCode,
/// EmployeeName, OrgLevel1..7, plus a "Leave Type" column that legacy adds ONLY for the On Leave bucket
/// (line 514-518). <see cref="LeaveTypeName"/> is therefore null for every other bucket, and the export
/// omits the column entirely for them.
/// </summary>
public class DailyAttendanceSummaryDetailRowDto : EntityDto<int>, IEmployeeOrgLevelRow
{
    /// <summary>Legacy "EmployeeCode" column (line 560).</summary>
    public string EmployeeCode { get; set; }

    /// <summary>
    /// Legacy "EmployeeName" column (line 561). Legacy always used <c>FullNameEn</c>, even under the
    /// Arabic aspect; this port resolves the name from the request culture, matching the decision
    /// already taken for the sibling <c>StaffHeadcountEmployeeReportRowDto</c>.
    /// </summary>
    public string EmployeeName { get; set; }

    /// <summary>
    /// Legacy "Leave Type" column (lines 569-574), present only in the On Leave view: the absence type
    /// of the FIRST full-day non-DayOff/non-Medical absence covering the day.
    /// </summary>
    public string LeaveTypeName { get; set; }

    public string OrgLevel1 { get; set; }

    public string OrgLevel2 { get; set; }

    public string OrgLevel3 { get; set; }

    public string OrgLevel4 { get; set; }

    public string OrgLevel5 { get; set; }

    public string OrgLevel6 { get; set; }

    public string OrgLevel7 { get; set; }

    /// <summary>
    /// Leaf org unit the OrgLevel columns are stamped from. Legacy read the seven discrete
    /// <c>Employee.OrgLevelN</c> FKs (lines 562-568); HCMv3 collapses them into the org assignment's
    /// single organization unit, so this is the unit of the assignment current on the REPORTED DAY.
    /// </summary>
    public long? OrganizationUnitId { get; set; }
}
