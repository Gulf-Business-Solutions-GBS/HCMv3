using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// The shared parameter block of BOTH the costing-file and the post-JV screens. Replaces the legacy
/// 9-element <c>string[] args</c> array built in
/// <c>uscCostingFile.ascx.cs</c> lines 250-269 / <c>uscPostJV.ascx.cs</c> lines 340-360.
///
/// <para>Legacy <c>args[3]</c> (Test/Live) and <c>args[6]</c> (Sponsor) are NOT carried over: <c>args[3]</c>
/// is hardcoded to "0" (live) at <c>uscCostingFile.ascx.cs</c> line 259 with the test-run line commented
/// out, and <c>args[6]</c> is hardcoded to "" at line 265 and never read. <c>args[7]</c> (CurrentAspect) is
/// replaced by ABP's ambient culture.</para>
/// </summary>
public class CostingRunInputDto
{
    /// <summary>
    /// The payroll area NAME. Legacy matched on <c>PayrollArea.Name</c>, not <c>Code</c>
    /// (<c>uscCostingFile.ascx.cs</c> line 1770), so this is a name and not an id.
    /// </summary>
    [Required]
    [StringLength(256)]
    public string PayrollAreaName { get; set; }

    /// <summary>Optional comma-separated employee codes. Empty means "all employees in the payroll area".</summary>
    [StringLength(4096)]
    public string EmployeeCode { get; set; }

    /// <summary>The payroll period end date (legacy "End Date" box, <c>args[2]</c>).</summary>
    [Required]
    public DateTime PayInDate { get; set; }

    /// <summary>The GL posting date stamped on every JV line (legacy "Posting Date" box, <c>args[5]</c>).</summary>
    [Required]
    public DateTime PostingDate { get; set; }

    /// <summary>Legacy <c>args[0]</c>: Regular or OffCycle.</summary>
    public PayrollRunType PayrollRunType { get; set; }
}

/// <summary>One JV line as shown in the preview grid. Mirrors <c>HCMv3.PayrollManagement.Costing.JournalVoucherLine</c>.</summary>
public class JournalVoucherLineDto
{
    public string CompanyAccounts { get; set; }

    public string JournalBatchNumber { get; set; }

    public int RecId { get; set; }

    public int LineNumber { get; set; }

    public string LineNumberDate { get; set; }

    public string LineNumberAccountType { get; set; }

    public string LedgerDimensionMainAccount { get; set; }

    public string LedgerDimensionMainAccountName { get; set; }

    public string DefaultDimensionLocation { get; set; }

    public string DefaultDimensionEmployees { get; set; }

    public string LedgerDimensionLocation { get; set; }

    public string LedgerDimensionBrand { get; set; }

    public string LedgerDimensionDepartment { get; set; }

    public string LedgerDimensionEmployees { get; set; }

    public string Currency { get; set; }

    public double Debit { get; set; }

    public double Credit { get; set; }

    public string Description { get; set; }

    public string OffsetLedgerDimensionMainAccount { get; set; }

    /// <summary>"Earning" / "Deduction" / "Provision" -- drives the salary vs provision JV split.</summary>
    public string BillingType { get; set; }

    public bool IsMerged { get; set; }

    public DateTime TransDate { get; set; }

    public string EmpCode { get; set; }
}

/// <summary>The JV preview plus its balance check.</summary>
public class JournalVoucherPreviewDto
{
    public List<JournalVoucherLineDto> Lines { get; set; } = new List<JournalVoucherLineDto>();

    /// <summary>Sum of debits, rounded to WHOLE UNITS -- legacy <c>uscCostingFile.ascx.cs</c> line 1719.</summary>
    public double TotalDebit { get; set; }

    /// <summary>Sum of credits, rounded to WHOLE UNITS -- legacy line 1720.</summary>
    public double TotalCredit { get; set; }

    /// <summary>Legacy line 1722: <c>debitamt != creditamt</c> after whole-unit rounding.</summary>
    public bool IsBalanced { get; set; }
}

/// <summary>Outcome of the "Validate JV" button (legacy <c>btnValidateJV_Click</c> -> <c>ExportGrid(true)</c>).</summary>
public class JournalVoucherValidationResultDto
{
    public bool IsValid { get; set; }

    /// <summary>Newline-joined localized validation messages, matching legacy's accumulated message string.</summary>
    public string Message { get; set; }

    public double TotalDebit { get; set; }

    public double TotalCredit { get; set; }

    public int LineCount { get; set; }
}

/// <summary>Per-employee log row of a cost-engine run (legacy <c>ProjectLog</c>).</summary>
public class CostingRunLogEntryDto
{
    public int? EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    /// <summary>0 = success, 1 = failed (legacy <c>ProjectLog.Status</c>).</summary>
    public int Status { get; set; }

    public string Message { get; set; }
}

/// <summary>Aggregate outcome of a cost-engine run.</summary>
public class CostingRunResultDto
{
    public int TotalEmployees { get; set; }

    public int SuccessCount { get; set; }

    public int FailedCount { get; set; }

    /// <summary>True when a <c>ProjectLog.StopFlag</c> row aborted the run mid-loop.</summary>
    public bool Aborted { get; set; }

    public List<CostingRunLogEntryDto> Entries { get; set; } = new List<CostingRunLogEntryDto>();
}

/// <summary>
/// Live progress counters for the screen's status panel. Legacy computed these in
/// <c>CalculateProcessStatus</c> (<c>uscCostingFile.ascx.cs</c> lines 1574-1636) by counting
/// <c>ProjectLog</c> rows.
/// </summary>
public class CostingProcessStatusDto
{
    public int TotalRecords { get; set; }

    public int SuccessRecords { get; set; }

    public int FailedRecords { get; set; }

    /// <summary>True when a stop has been requested (legacy <c>ProjectLog.StopFlag</c>).</summary>
    public bool StopRequested { get; set; }
}

/// <summary>Payroll-area combo entry (legacy <c>BindPayrollArea</c>).</summary>
public class PayrollAreaLookupDto
{
    public int Id { get; set; }

    public string Code { get; set; }

    public string Name { get; set; }

    /// <summary>False when <c>FICompanyId</c> or <c>FIBankId</c> is missing -- both are hard preconditions.</summary>
    public bool IsFiConfigured { get; set; }
}

/// <summary>Result of posting the JV to the external finance system.</summary>
public class PostJvResultDto
{
    public bool Success { get; set; }

    /// <summary>Accumulated per-JV message, matching legacy's <c>message</c> string built at lines 837-857.</summary>
    public string Message { get; set; }

    /// <summary>Journal batch numbers returned by the finance system, one per posted journal header.</summary>
    public List<string> JournalBatchNumbers { get; set; } = new List<string>();

    public int SalaryLineCount { get; set; }

    public int ProvisionLineCount { get; set; }
}
