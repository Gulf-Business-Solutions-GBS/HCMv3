using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.TimeReport.Dto;

/// <summary>
/// Filter input for the Monthly Time Attendance grid report (legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptMonthlyTimeAttendance.ascx</c>).
///
/// Uses only the common filters -- <c>dtFrom</c>, <c>dtTo</c>, <c>cmbEmpStatus</c>, <c>txtEmpCode</c> --
/// so it adds no fields of its own. Legacy's <c>GetParameterStr()</c> (lines 465-476) logs employee code
/// plus the two dates.
/// </summary>
public class GetMonthlyTimeAttendanceReportInput : GridReportInputBase
{
    /// <summary>
    /// Legacy left the grid unsorted. Head count descending puts the worst-affected departments first,
    /// which is the point of this summary, and is deterministic enough for server-side paging when
    /// combined with the department name.
    /// </summary>
    protected override string DefaultSorting => "NoOfEmployees DESC, DepartmentName ASC";
}

/// <summary>
/// One grid row of legacy <c>Table1()</c> (uscRptMonthlyTimeAttendance.ascx.cs:480-539).
///
/// SUMMARY GRAIN, NOT PER EMPLOYEE: one row per department, counting the DISTINCT EMPLOYEES in that
/// department who had at least one day in the window with a late-in, an early-out, either deduction, or a
/// deduction after monthly tolerance. Legacy columns: <c>Department</c> and <c>No. of Employees</c>
/// (lines 511-512).
/// </summary>
public class MonthlyTimeAttendanceReportRowDto
{
    /// <summary>
    /// Legacy "Department" column -- <c>g.Key.NameEn</c> over <c>rec.Employee.CurrentDepartment</c>
    /// (line 521), i.e. ENGLISH even in Arabic.
    ///
    /// MODEL ADAPTATION: HCMv3's <c>Employee</c> has no <c>CurrentDepartment</c> computed property (its
    /// backing org fields were replaced by the single <c>EmployeeOrgAssignment.OrganizationUnitId</c> leaf
    /// into ABP's hierarchical org tree). "Current department" therefore becomes "the deepest classified
    /// level of the leaf on the employee's org assignment current as of today", the same resolution used
    /// by <c>TimeAnalysisByDeptReportAppService</c> and <c>PayrollAnalysisByDeptReportAppService</c>.
    /// </summary>
    public string DepartmentName { get; set; }

    /// <summary>
    /// Legacy "No. of Employees" column -- <c>g.GroupBy(a =&gt; a.Employee).Count()</c> (line 522), i.e.
    /// the number of DISTINCT employees behind the department's rows, not the number of rows.
    /// </summary>
    public int NoOfEmployees { get; set; }
}
