using HCMv3.GridReports.Dto;
using HCMv3.PayrollManagement;

namespace HCMv3.GridReports.PayrollReport.Dto;

/// <summary>
/// Filter input for the Salary Retro Result grid report (legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptSalaryRetroResult.ascx</c>).
/// </summary>
public class GetSalaryRetroResultReportInput : GridReportInputBase
{
    public int? Month { get; set; }

    public int? Year { get; set; }

    /// <summary>Legacy <c>cmbRunType</c>. Blank means Regular (legacy's default selected index).</summary>
    public PayrollRunType? PayrollRunType { get; set; }

    /// <summary>Legacy <c>cmbType</c> (Billing Type). Null means "all billing types".</summary>
    public BillingType? BillingType { get; set; }

    public int? PayrollAreaId { get; set; }

    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>
/// One grid row of legacy <c>Table1()</c> (lines 739-927): compares a REGULAR in-period register's wage
/// line against the RETRO (arrear) register that recalculated the same period, for every arrear-source
/// wage type touched. Emitted only when the Rate, Number OR Amount differ.
/// </summary>
public class SalaryRetroResultReportRowDto
{
    public int Id { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    public string PayrollAreaName { get; set; }

    public string PayrollRunTypeName { get; set; }

    /// <summary>The CURRENT period the retro register was posted IN (<c>InPeriod</c>).</summary>
    public string CurrentMonthYear { get; set; }

    /// <summary>The arrear wage type whose retro line triggered this comparison.</summary>
    public string WageTypeName { get; set; }

    /// <summary>The historical period being retro-corrected (<c>ForPeriod</c> of the retro register).</summary>
    public string RetroMonthYear { get; set; }

    public double RateRegular { get; set; }

    public double RateArrear { get; set; }

    public double RateDifference { get; set; }

    public double NumberRegular { get; set; }

    public double NumberArrear { get; set; }

    public double NumberDifference { get; set; }

    public double AmountRegular { get; set; }

    public double AmountArrear { get; set; }

    public double AmountDifference { get; set; }
}
