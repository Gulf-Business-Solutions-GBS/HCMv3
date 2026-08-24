using System;
using System.ComponentModel.DataAnnotations;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.PayrollReport.Dto;

/// <summary>
/// Filter input for the Payroll Analysis By Department grid report (legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPayrollAnalysisByDept.ascx</c>).
///
/// ATYPICAL FILTER PANEL: legacy's panel is <c>txtMonth</c> and <c>txtYear</c> ONLY -- no dates, no
/// employee code, no employee status, no run type, no payroll area. <c>GetParameterStr</c>
/// (uscRptPayrollAnalysisByDept.ascx.cs:359-370) logs exactly Month and Year. The inherited base filters
/// are unused and the Angular screen hides them.
/// </summary>
public class GetPayrollAnalysisByDeptReportInput : GridReportInputBase
{
    /// <summary>
    /// Legacy <c>txtMonth</c> (line 297). Legacy did a bare <c>int.Parse</c> and threw on a blank box;
    /// null here means the CURRENT month, matching how every other Month/Year report in the family behaves.
    /// </summary>
    [Range(1, 12)]
    public int? Month { get; set; }

    /// <summary>Legacy <c>txtYear</c> (line 298). Null means the CURRENT year -- see <see cref="Month"/>.</summary>
    [Range(1900, 2999)]
    public int? Year { get; set; }

    /// <summary>
    /// Legacy grid was unsorted. Total amount descending is the natural reading order for a
    /// spend-by-department summary and is deterministic, which server-side paging requires.
    /// </summary>
    protected override string DefaultSorting => "TotalAmount DESC";
}

/// <summary>
/// One grid row of legacy <c>Table1()</c> (uscRptPayrollAnalysisByDept.ascx.cs:377-427).
///
/// ATYPICAL GRAIN -- THIS IS A SUMMARY, NOT A PER-EMPLOYEE SHEET. One row per
/// (department, <c>SalaryRegister.ForPeriod</c>), the value being the SUM of every employee's NET PAY
/// (wage type 9000) in that department for that period. Future ports should not copy the per-employee
/// paging assumptions of the rest of the family from this file.
///
/// Legacy columns: <c>Department</c>, <c>TotalAmount</c>, and a HIDDEN <c>Period</c>
/// (<c>ColumnMapping = MappingType.Hidden</c>, line 396) that exists only to keep two periods of the same
/// department on separate rows.
/// </summary>
public class PayrollAnalysisByDeptReportRowDto
{
    /// <summary>
    /// Legacy "Department" column. Legacy read <c>OrgService.GetOrgUnitForUC(employee)</c>, i.e. the
    /// DEEPEST non-null <c>Employee.OrgLevel7..OrgLevel1</c> field, and rendered its <c>NameEn</c>
    /// (line 411 -- never the Arabic name, even in Arabic).
    ///
    /// MODEL ADAPTATION: HCMv3 has no OrgLevel1..7 fields; the employee's unit is the single
    /// <c>EmployeeOrgAssignment.OrganizationUnitId</c> leaf in the real org tree, so "deepest non-null
    /// level" becomes "the deepest classified level of that leaf". Empty string when the employee has no
    /// current assignment or the leaf carries no level profile -- legacy also wrote <c>""</c>, never null.
    /// </summary>
    public string DepartmentName { get; set; }

    /// <summary>Legacy hidden "Period" column -- <c>SalaryRegister.ForPeriod</c>.</summary>
    public DateTime Period { get; set; }

    /// <summary>Legacy "TotalAmount" column -- SUM of wage type 9000 amounts across the department.</summary>
    public double TotalAmount { get; set; }
}
