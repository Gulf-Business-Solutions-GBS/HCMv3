using HCMv3.GridReports.Dto;
using HCMv3.PayrollManagement;

namespace HCMv3.GridReports.PayrollReport.Dto;

/// <summary>
/// Filter input for the Payroll CTC grid report (legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPayrollCTC.ascx</c>).
/// </summary>
public class GetPayrollCTCReportInput : GridReportInputBase
{
    public int? Month { get; set; }

    public int? Year { get; set; }

    /// <summary>Legacy <c>cmbRunType</c>. Blank means Regular (legacy's fallback, lines 683-691).</summary>
    public PayrollRunType? PayrollRunType { get; set; }

    public int? PayrollAreaId { get; set; }

    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>
/// One grid row of legacy <c>Table1()</c> (lines 547-886): one row per Regular in-period
/// <c>SalaryRegister</c> for the reported month, widened with the employee's cost-to-company breakdown.
/// </summary>
public class PayrollCTCReportRowDto
{
    public int Id { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    public string EmployeeNameAr { get; set; }

    public string Status { get; set; }

    public System.DateTime JoiningDate { get; set; }

    public string PayrollMonth { get; set; }

    public int Year { get; set; }

    public string PayrollAreaName { get; set; }

    public string PayrollRunTypeName { get; set; }

    public string JobTitle { get; set; }

    public string OrgLevel1 { get; set; }

    public string OrgLevel2 { get; set; }

    public string OrgLevel3 { get; set; }

    public string OrgLevel4 { get; set; }

    public string OrgLevel5 { get; set; }

    public string OrgLevel6 { get; set; }

    public string OrgLevel7 { get; set; }

    /// <summary>Number of wage type 1000 (Basic Salary) in the salary register.</summary>
    public double WorkingDays { get; set; }

    /// <summary>Wage type 1000 amount from <c>EmployeeBasicPayDetail</c>, as of the reported month end.</summary>
    public double ActBasicSalary { get; set; }

    /// <summary>Wage type 1039 amount from <c>EmployeeBasicPayDetail</c>, as of the reported month end.</summary>
    public double ActFoodAllowance { get; set; }

    /// <summary>Wage type 1000 amount from the salary register.</summary>
    public double BasicSalary { get; set; }

    /// <summary>Wage type 7000 amount.</summary>
    public double ArrearsBasicSalary { get; set; }

    /// <summary>Sum of Number for wage types 1007/1008/1009/1042.</summary>
    public double OvertimeHours { get; set; }

    /// <summary>Sum of Amount for wage types 1007/1008/1009/1042.</summary>
    public double OvertimeAmount { get; set; }

    /// <summary>Sum of Amount for wage types 7007/7008/7009/7042.</summary>
    public double OvertimeAmountArrears { get; set; }

    public double SickLeavePayDays { get; set; }

    public double SickLeavePay { get; set; }

    public double SickLeavePayArrear { get; set; }

    public double AirTicketPay { get; set; }

    public double OtherAdjust { get; set; }

    public double LeaveProvisionAmount { get; set; }

    public double IndemnityProvisionAmount { get; set; }

    public double TotalLeaveProvisionAmount { get; set; }

    public double TotalIndemnityProvisionAmount { get; set; }
}
