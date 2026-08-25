using HCMv3.GridReports.Dto;
using HCMv3.PayrollManagement;

namespace HCMv3.GridReports.PayrollReport.Dto;

/// <summary>
/// Filter input for the Payroll Net Pay / Contract Diff grid report (legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPayrollNetPayContractDiff.ascx</c>).
/// </summary>
public class GetPayrollNetPayContractDiffReportInput : GridReportInputBase
{
    public int? Month { get; set; }

    public int? Year { get; set; }

    public PayrollRunType? PayrollRunType { get; set; }

    public int? PayrollAreaId { get; set; }

    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>
/// One grid row of legacy <c>Table1()</c> (lines 507-666): compares the employee's sponsor CONTRACT salary
/// against the reported NET PAY (wage type 9000) for the month.
/// </summary>
public class PayrollNetPayContractDiffReportRowDto
{
    public int Id { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    public string JobTitle { get; set; }

    public string OrgLevel1 { get; set; }

    public string OrgLevel2 { get; set; }

    public string OrgLevel3 { get; set; }

    public string OrgLevel4 { get; set; }

    public string OrgLevel5 { get; set; }

    public string OrgLevel6 { get; set; }

    public string OrgLevel7 { get; set; }

    public System.DateTime PayDate { get; set; }

    /// <summary>The current sponsor's contract salary (0 when the employee has no current sponsor).</summary>
    public double ContractPay { get; set; }

    /// <summary>Wage type 9000 (Net Pay) for the reported month.</summary>
    public double NetPay { get; set; }

    public double Difference { get; set; }
}
