using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.PayrollManagement;

namespace HCMv3.GridReports.PayrollReport.Dto;

/// <summary>
/// Filters shared by the Payroll JV (Custom 016) "Main Format" / "Detail" reports -- legacy
/// <c>uscRptPayrollJV_Custom016.ascx</c>: <c>txtMonth</c>, <c>txtYear</c>, <c>cmbRunType</c>,
/// <c>txtEmpCode</c>. Like the already-ported Loan Format, the legacy <c>cmbPayrollArea</c> selection is
/// NOT exposed here: BMC and MYB produce structurally different column sets (legacy lines 1176-2079), so
/// each is its own report and the Payroll Area filter is resolved server-side by literal Name -- see
/// <see cref="PayrollJVCustom016MainFormatBmcReportAppService"/> /
/// <see cref="PayrollJVCustom016MainFormatMybReportAppService"/> / <see cref="PayrollJVCustom016DetailReportAppService"/>
/// remarks.
/// </summary>
public class GetPayrollJVCustom016MainFormatReportInput : GridReportInputBase
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
/// One row of the Payroll JV (Custom 016) "Main Format" (and, for BMC, identically "Main Format-New")
/// voucher for the payroll area literally named "BMC" -- legacy lines 1176-1524 (<c>IsDetail == false</c>).
/// </summary>
public class PayrollJVCustom016MainFormatBmcRowDto : EntityDto<int>
{
    /// <summary>Legacy "Oid": a 1-based running row number, not a database key.</summary>
    public int LineNumber { get; set; }

    /// <summary>Legacy "Journal Ref": always empty.</summary>
    public string JournalRef { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    /// <summary>Legacy "Account": cost-center bucket (or padded code) + " " + padded account code.</summary>
    public string Account { get; set; }

    /// <summary>Legacy "Suffix": "MMM/yyyy" upper-cased.</summary>
    public string Suffix { get; set; }

    public double Amount { get; set; }

    public string Narrative { get; set; }
}

/// <summary>
/// One row of the Payroll JV (Custom 016) "Main Format-New" voucher for every payroll area whose Name
/// contains "MYB" -- legacy lines 1578-2016 (<c>IsDetail == false</c>).
/// </summary>
public class PayrollJVCustom016MainFormatMybRowDto : EntityDto<int>
{
    /// <summary>Legacy "Oid": a 1-based running row number, not a database key.</summary>
    public int LineNumber { get; set; }

    /// <summary>Legacy "C.C.": literal "0000" on the summary rows, the real cost center on debit rows.</summary>
    public string CostCenter { get; set; }

    /// <summary>Legacy "Exp": the account code for this row, zero-padded.</summary>
    public string Exp { get; set; }

    public DateTime Date { get; set; }

    public DateTime DocDate { get; set; }

    /// <summary>Legacy "Srce": always the literal "PY".</summary>
    public string Source { get; set; } = "PY";

    /// <summary>Legacy "Co": always the literal "31".</summary>
    public string Company { get; set; } = "31";

    /// <summary>Legacy "Suffix": "MMM/yyyy" upper-cased.</summary>
    public string Suffix { get; set; }

    /// <summary>Legacy "SL/PL AC": "MMM/yyyy" upper-cased.</summary>
    public string SlPlAccount { get; set; }

    /// <summary>Legacy "Reference" = "MMM/yyyy SAL".</summary>
    public string Reference { get; set; }

    public string Narrative { get; set; }

    /// <summary>Legacy "Reconciled reference": always empty.</summary>
    public string ReconciledReference { get; set; } = string.Empty;

    /// <summary>Legacy "R": always empty.</summary>
    public string RColumn { get; set; } = string.Empty;

    public double Debit { get; set; }

    public double Credit { get; set; }
}

/// <summary>
/// One row of the Payroll JV (Custom 016) "Detail" voucher (SuperAdmin-only, legacy line 250-256) for the
/// payroll area literally named "BMC" -- legacy lines 1176-1445 (summary) + 1527-1572
/// (<c>IsDetail == true</c>, one row per un-grouped posting line rather than per cost center).
/// </summary>
public class PayrollJVCustom016DetailRowDto : EntityDto<int>
{
    /// <summary>Legacy "Oid": a 1-based running row number, not a database key.</summary>
    public int LineNumber { get; set; }

    /// <summary>Legacy "EmpCode": set only on detail rows, blank on the cost-center summary rows.</summary>
    public string EmployeeCode { get; set; }

    /// <summary>Legacy "WageType": set only on detail rows, blank on the cost-center summary rows.</summary>
    public string WageTypeName { get; set; }

    /// <summary>Legacy "CostCenter": raw (not zero-padded) code, set only on detail rows.</summary>
    public string CostCenterCode { get; set; }

    /// <summary>Legacy "Journal Ref": always empty.</summary>
    public string JournalRef { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    /// <summary>Legacy "Account": cost-center bucket (or padded code) + " " + padded account code.</summary>
    public string Account { get; set; }

    /// <summary>Legacy "Suffix": "MMM/yyyy" upper-cased.</summary>
    public string Suffix { get; set; }

    public double Amount { get; set; }

    public string Narrative { get; set; }
}
