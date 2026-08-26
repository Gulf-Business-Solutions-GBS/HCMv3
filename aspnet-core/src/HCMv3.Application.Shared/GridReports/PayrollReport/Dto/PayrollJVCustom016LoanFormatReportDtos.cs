using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.PayrollManagement;

namespace HCMv3.GridReports.PayrollReport.Dto;

/// <summary>
/// Filters of the Payroll JV (Custom 016) "Loan Format" report -- legacy
/// <c>uscRptPayrollJV_Custom016.ascx</c>: <c>txtMonth</c>, <c>txtYear</c>, <c>cmbRunType</c>,
/// <c>txtEmpCode</c>. UNLIKE most of the JV family, the legacy <c>cmbPayrollArea</c> selection is NOT
/// exposed here: the BMC and MYB payroll areas produce two completely different column sets (legacy
/// lines 2091-2298), so each is its own report, and the Payroll Area filter is resolved server-side by
/// literal Name -- see <see cref="PayrollJVCustom016LoanFormatBmcReportAppService"/> /
/// <see cref="PayrollJVCustom016LoanFormatMybReportAppService"/> remarks (same convention as
/// <c>PayrollJVCustom019PayrollAreaScope</c>).
/// </summary>
public class GetPayrollJVCustom016LoanFormatReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>txtMonth</c>. Blank meant "current month".</summary>
    public int? Month { get; set; }

    /// <summary>Legacy <c>txtYear</c>. Blank meant "current year".</summary>
    public int? Year { get; set; }

    /// <summary>Legacy <c>cmbRunType</c>. Null/"ALL" means every run type.</summary>
    public PayrollRunType? PayrollRunType { get; set; }

    protected override string DefaultSorting => "LineNumber ASC";
}

/// <summary>
/// One row of the Payroll JV (Custom 016) "Loan Format - BMC" voucher (legacy lines 2091-2191): a single
/// debit/credit style row per (Employee, WageType, CostCenter, Account, OffsetAccount) group.
/// </summary>
public class PayrollJVCustom016LoanFormatBmcRowDto : EntityDto<int>
{
    /// <summary>Legacy "Oid": a 1-based running row number, not a database key.</summary>
    public int LineNumber { get; set; }

    /// <summary>Legacy "SL/PL acc" = "E8" + employee code.</summary>
    public string SlPlAccount { get; set; }

    /// <summary>Legacy "Narrative" / "Narrative " (both columns always carry the same value).</summary>
    public string Narrative { get; set; }

    /// <summary>Legacy "MCSR": always the literal "REV8-PY".</summary>
    public string Mcsr { get; set; } = "REV8-PY";

    public DateTime DocumentDate { get; set; }

    public DateTime DueDate { get; set; }

    /// <summary>Legacy "VAT": always 0.</summary>
    public double Vat { get; set; }

    /// <summary>Legacy "Base value" = -1 * CostAmount.</summary>
    public double BaseValue { get; set; }

    /// <summary>Legacy "E" column: always the literal "E".</summary>
    public string EColumn { get; set; } = "E";

    /// <summary>Legacy "NL C0DE" = cost-center bucket + offset account code, zero-padded to 8.</summary>
    public string NlCode { get; set; }

    /// <summary>Legacy "Suffix": same value as <see cref="SlPlAccount"/>.</summary>
    public string Suffix { get; set; }

    /// <summary>Legacy "vaT ": always the literal "S".</summary>
    public string VatCode { get; set; } = "S";

    /// <summary>Legacy "GoodsValue" = CostAmount.</summary>
    public double GoodsValue { get; set; }

    /// <summary>Legacy "Branch": "2900" when the cost center starts with "2", else "1900".</summary>
    public string Branch { get; set; }
}

/// <summary>
/// One row of the Payroll JV (Custom 016) "Loan Format - MYB" voucher (legacy lines 2194-2298): each
/// (Employee, WageType, Account, OffsetAccount) group produces a DEBIT row and a CREDIT row.
/// </summary>
public class PayrollJVCustom016LoanFormatMybRowDto : EntityDto<int>
{
    /// <summary>Legacy "Oid": a 1-based running row number, not a database key.</summary>
    public int LineNumber { get; set; }

    /// <summary>Legacy "C.C.": always the literal "0000" on this format.</summary>
    public string CostCenter { get; set; } = "0000";

    /// <summary>Legacy "Exp": the account code for this row, zero-padded to 6.</summary>
    public string Exp { get; set; }

    public DateTime Date { get; set; }

    public DateTime DocDate { get; set; }

    /// <summary>Legacy "Srce": always the literal "PY".</summary>
    public string Source { get; set; } = "PY";

    /// <summary>Legacy "Co": always the literal "31".</summary>
    public string Company { get; set; } = "31";

    /// <summary>Legacy "Suffix" = employee code.</summary>
    public string Suffix { get; set; }

    /// <summary>Legacy "SL/PL AC" = employee reference code.</summary>
    public string SlPlAccount { get; set; }

    /// <summary>Legacy "Reference" = "MMM/yyyy SAL".</summary>
    public string Reference { get; set; }

    /// <summary>Legacy "Narrative ".</summary>
    public string Narrative { get; set; }

    /// <summary>Legacy "Reconciled reference": always empty.</summary>
    public string ReconciledReference { get; set; } = string.Empty;

    /// <summary>Legacy "R": always empty.</summary>
    public string RColumn { get; set; } = string.Empty;

    public double Debit { get; set; }

    public double Credit { get; set; }
}
