using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.PayrollManagement;

namespace HCMv3.GridReports.PayrollReport.Dto;

/// <summary>
/// Filters shared by the Payroll JV (Custom 016) "Leave Provision" / "Indemnity Provision" reports --
/// legacy <c>uscRptPayrollJV_Custom016.ascx</c>: <c>cmbPayrollArea</c>, <c>txtMonth</c>, <c>txtYear</c>,
/// <c>cmbRunType</c>. UNLIKE the Main Format / Loan Format family, the Payroll Area filter IS a normal
/// (required) user filter here -- legacy's outer <c>if (emppayrollArea != null)</c> gate (line 595) wraps
/// every <c>cmbType</c> branch including these two, and <c>dsProv</c> itself is additionally narrowed to
/// that same payroll area (legacy lines 2430-2435 / 2492-2497).
/// </summary>
public class GetPayrollJVCustom016ProvisionDiffReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>cmbPayrollArea</c> (Code). Required -- legacy produced no rows at all without it.</summary>
    public string PayrollAreaCode { get; set; }

    /// <summary>Legacy <c>txtMonth</c>. Blank meant "current month".</summary>
    public int? Month { get; set; }

    /// <summary>Legacy <c>txtYear</c>. Blank meant "current year".</summary>
    public int? Year { get; set; }

    /// <summary>Legacy <c>cmbRunType</c>. Null/"ALL" means every run type.</summary>
    public PayrollRunType? PayrollRunType { get; set; }

    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>
/// One row of the Payroll JV (Custom 016) "Leave Provision" / "Indemnity Provision" report: an employee
/// whose Regular-run salary register carries a non-zero Annual Leave / Indemnity Provision amount that did
/// NOT make it into the JV posting lines for the same period and payroll area -- legacy lines 2397-2518.
///
/// LEGACY ONLY EVER EMITS "NOT IN JV" ROWS (legacy's <c>dsNotInJV</c> is never joined back against a
/// matched JV amount), so <see cref="AmountJV"/> is always 0 and <see cref="Difference"/> always equals
/// <see cref="AmountReg"/> -- both fields are kept for column-for-column fidelity with the legacy grid.
/// </summary>
public class PayrollJVCustom016ProvisionDiffRowDto : EntityDto<int>
{
    /// <summary>Legacy "Oid": a 1-based running row number, not a database key.</summary>
    public int LineNumber { get; set; }

    /// <summary>Legacy "EmpCode".</summary>
    public string EmployeeCode { get; set; }

    /// <summary>Legacy "AmountReg": the raw <c>SalaryRegisterDetail.Amount</c> (not CostAmount).</summary>
    public double AmountReg { get; set; }

    /// <summary>Legacy "AmountJV": always 0 -- see class remarks.</summary>
    public double AmountJV { get; set; }

    /// <summary>Legacy "Difference" = AmountReg - AmountJV, always equal to <see cref="AmountReg"/>.</summary>
    public double Difference { get; set; }
}
