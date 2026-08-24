using System;
using System.Collections.Generic;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.PayrollManagement;

namespace HCMv3.GridReports.PayrollReport.Dto;

/// <summary>
/// Filters of the Payroll JV (Custom 018) report -- legacy
/// <c>uscRptPayrollJV_Custom018.ascx</c> lines 19-33: <c>txtMonth</c>, <c>txtYear</c>,
/// <c>cmbRunType</c> and <c>cmbPayrollArea</c>.
///
/// The inherited <see cref="GridReportInputBase.FromDate"/>/<see cref="GridReportInputBase.ToDate"/>,
/// <see cref="GridReportInputBase.EmployeeCode"/> and <see cref="GridReportInputBase.EmployeeStatus"/>
/// are NOT part of this legacy panel and are ignored; the Angular screen hides all three.
/// </summary>
public class GetPayrollJVCustom018ReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>txtMonth</c>. Blank meant "current month" (legacy <c>GetTable</c> line 453).</summary>
    public int? Month { get; set; }

    /// <summary>Legacy <c>txtYear</c>. Blank meant "current year" (legacy <c>GetTable</c> line 455).</summary>
    public int? Year { get; set; }

    /// <summary>Legacy <c>cmbRunType</c> (<c>Session[RunType]</c>), applied at legacy line 584.</summary>
    public PayrollRunType? PayrollRunType { get; set; }

    /// <summary>
    /// Legacy <c>cmbPayrollArea</c>. PRESENT BUT INERT -- see
    /// <see cref="PayrollJVCustom018ReportRowDto"/> remarks and the pinning test: legacy stashed the
    /// selection in <c>Session[payrollArea]</c> and printed it in the execution-log parameter string,
    /// but <c>Table1()</c> never reads it back, so it filters nothing. Reproduced verbatim.
    /// </summary>
    public int? PayrollAreaId { get; set; }

    protected override string DefaultSorting => "LineNumber ASC";
}

/// <summary>
/// One journal line of the Payroll JV (Custom 018) voucher -- legacy <c>Table1()</c>
/// (<c>uscRptPayrollJV_Custom018.ascx.cs</c> lines 530-911), whose <c>DataTable</c> has exactly ten
/// columns: Date, Reference, G/L Account, Description, Amount, Job ID, Used for Reimbursable Expenses,
/// Transaction Period, Transaction Number and Consolidated Transaction (legacy lines 542-561).
///
/// This report is NOT one row per employee: it emits a FIXED list of 13 GL lines whose amounts are
/// aggregates of the month's salary register, bucketed by the employee's org-unit names.
/// </summary>
public class PayrollJVCustom018ReportRowDto : EntityDto<int>
{
    /// <summary>Row ordinal; the legacy grid had no key column, so this is the stable sort key.</summary>
    public int LineNumber { get; set; }

    /// <summary>Legacy <c>Date</c> column: always the month-end date (legacy line 677).</summary>
    public DateTime Date { get; set; }

    /// <summary>Legacy <c>Reference</c> column: always empty string (legacy line 678).</summary>
    public string Reference { get; set; } = string.Empty;

    /// <summary>Legacy <c>G/L Account</c> column: a hard-coded account number per line.</summary>
    public string GLAccount { get; set; }

    /// <summary>Legacy <c>Description</c> column: caption + " " + month-end in "MMMM yyyy".</summary>
    public string Description { get; set; }

    /// <summary>Legacy <c>Amount</c> column (double).</summary>
    public double Amount { get; set; }

    /// <summary>Legacy <c>Job ID</c> column: always empty string.</summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>Legacy <c>Used for Reimbursable Expenses</c>: the literal "FALSE" on every line.</summary>
    public string UsedForReimbursableExpenses { get; set; } = "FALSE";

    /// <summary>Legacy <c>Transaction Period</c> column: always empty string.</summary>
    public string TransactionPeriod { get; set; } = string.Empty;

    /// <summary>Legacy <c>Transaction Number</c> column: always empty string.</summary>
    public string TransactionNumber { get; set; } = string.Empty;

    /// <summary>Legacy <c>Consolidated Transaction</c>: the literal "FALSE" on every line.</summary>
    public string ConsolidatedTransaction { get; set; } = "FALSE";
}

/// <summary>Paged result plus the server-side column list that drives both the grid and the xlsx.</summary>
public class PayrollJVCustom018ReportOutput : PagedResultDto<PayrollJVCustom018ReportRowDto>
{
    public List<PayrollJVReportColumnDto> Columns { get; set; } = new();
}
