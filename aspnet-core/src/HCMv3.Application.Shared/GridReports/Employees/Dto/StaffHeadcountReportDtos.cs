using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.Employees.Dto;

/// <summary>
/// Filter input for LEVEL 1 of the Staff Headcount analysis grid report (legacy
/// <c>uscRptStaffHeadcount.ascx</c>) -- the department headcount summary.
///
/// This report is a genuine MULTI-LEVEL DRILL-DOWN, unlike the reference report: legacy defines
/// <c>Table1()</c> / <c>Table2(selectedColumnValue)</c> / <c>Table3(selectedColumnValue)</c> with keys
/// "Department" / "Employee" / "OID" (uscRptStaffHeadcount.ascx.cs:381-400) and pushes the clicked cell
/// value onto a session stack to move between them. Levels 1 and 2 are ported as two endpoint pairs;
/// see <see cref="StaffHeadcountEmployeeReportRowDto"/> for level 2 and the app service remarks for why
/// level 3 is not ported.
///
/// INHERITED-BUT-UNUSED: <see cref="GridReportInputBase.FromDate"/>/<see cref="GridReportInputBase.ToDate"/>
/// are NOT used. Legacy's <c>Table1()</c> applies only the status and employee-code filters (lines
/// 476-491) -- it has no date predicate whatsoever, even though the control does render the date
/// pickers and logs them in the parameter string (line 466-467).
/// </summary>
public class GetStaffHeadcountReportInput : GridReportInputBase
{
    /// <summary>Legacy grouped and displayed by department name (lines 496-501).</summary>
    protected override string DefaultSorting => "DepartmentName ASC";
}

/// <summary>
/// Filter input for LEVEL 2 -- the employees making up one level-1 headcount row. Legacy
/// <c>Table2(selectedColumnValue)</c> received the clicked DEPARTMENT NAME as a string and re-queried
/// with <c>r.CurrentDepartment.NameEn == selectedColumnValue</c> (lines 537-538). This port passes the
/// org unit's ID instead of its display name, which is the same selection without the
/// duplicate-name ambiguity a name match has.
/// </summary>
public class GetStaffHeadcountEmployeeReportInput : GridReportInputBase
{
    /// <summary>The level-1 row that was drilled into. Required.</summary>
    public long OrganizationUnitId { get; set; }

    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>
/// LEVEL 1 row. Mirrors legacy <c>Table1()</c>'s two-column DataTable
/// (uscRptStaffHeadcount.ascx.cs:493-494): "Department", "Employees".
///
/// DEVIATION -- legacy grouped by <c>Employee.CurrentDepartment</c>, which resolved the ancestor org
/// unit classified as OrganizationUnitType "Department" over the legacy's 7 discrete OrgLevel1..7 FKs
/// (legacy <c>Employee.cs:1352</c> -&gt; <c>OrgService.GetCurrentOrgUnit</c>). HCMv3 collapses those 7
/// FKs into a single <c>EmployeeOrgAssignment.OrganizationUnitId</c> (see that entity's class remarks),
/// so the grouping here is by the org unit ON the assignment current today, without the
/// "of type Department" ancestor walk -- that walk is a recursive tree climb and is not expressible in
/// the single translatable query this report's paging and export share.
/// </summary>
public class StaffHeadcountReportRowDto : EntityDto<long>
{
    /// <summary>
    /// Legacy "Department" column and grid KEY (line 390). Here the display name of the employee's
    /// current org unit.
    /// </summary>
    public string DepartmentName { get; set; }

    /// <summary>Legacy "Employees" column -- <c>g.Count()</c> (line 501).</summary>
    public int EmployeeCount { get; set; }
}

/// <summary>
/// LEVEL 2 row. Mirrors legacy <c>Table2()</c>'s two-column DataTable
/// (uscRptStaffHeadcount.ascx.cs:533-534): "Code", "Employee".
/// </summary>
public class StaffHeadcountEmployeeReportRowDto : EntityDto<int>
{
    /// <summary>Legacy "Code" column (line 561).</summary>
    public string EmployeeCode { get; set; }

    /// <summary>
    /// Legacy "Employee" column and grid KEY for the next drill level (line 562). Legacy always used
    /// <c>FullNameEn</c> here even in the Arabic aspect; this port follows the rest of the family and
    /// resolves the name from the request culture instead.
    /// </summary>
    public string EmployeeName { get; set; }
}
