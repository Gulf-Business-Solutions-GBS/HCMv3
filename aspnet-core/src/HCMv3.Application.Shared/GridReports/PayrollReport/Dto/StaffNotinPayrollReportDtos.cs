using HCMv3.GridReports.Dto;
using HCMv3.PayrollManagement;

namespace HCMv3.GridReports.PayrollReport.Dto;

/// <summary>
/// Filter input for the Staff Not in Payroll grid report (legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptStaffNotinPayroll.ascx</c>).
/// Legacy panel: Month / Year / Run Type (unused, see remarks) / Payroll Area -- no date range, employee
/// code or status.
/// </summary>
public class GetStaffNotinPayrollReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>txtMonth</c>. Blank means the current month.</summary>
    public int? Month { get; set; }

    /// <summary>Legacy <c>txtYear</c>. Blank means the current year.</summary>
    public int? Year { get; set; }

    /// <summary>
    /// Legacy <c>cmbRunType</c> -- present on the panel and logged in <c>GetParameterStr</c>, but
    /// <c>Session[RunType]</c> is never read inside <c>Table1()</c>. Kept purely to reproduce the logged
    /// parameter string; has no filtering effect (legacy has no run-type concept for on-hold records).
    /// </summary>
    public PayrollRunType? PayrollRunType { get; set; }

    /// <summary>Legacy <c>cmbPayrollArea</c>: filters via the org assignment current as of EndDate.</summary>
    public int? PayrollAreaId { get; set; }

    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>One grid row of legacy <c>Table1()</c> (lines 498-625): an employee on payroll hold.</summary>
public class StaffNotinPayrollReportRowDto
{
    public int Id { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    public System.DateTime JoiningDate { get; set; }

    public string JobTitle { get; set; }

    public string PayrollAreaName { get; set; }

    public string OrgLevel1 { get; set; }

    public string OrgLevel2 { get; set; }

    public string OrgLevel3 { get; set; }

    public string OrgLevel4 { get; set; }

    public string OrgLevel5 { get; set; }

    public string OrgLevel6 { get; set; }

    public string OrgLevel7 { get; set; }
}
