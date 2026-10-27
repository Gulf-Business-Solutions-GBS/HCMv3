using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;

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

    /// <summary>
    /// Posting only: the <see cref="JournalVoucherPreviewDto.PreviewFingerprint"/> of the preview the
    /// operator checked. Post JV refuses when the JV rebuilt at posting time no longer has this fingerprint.
    /// Ignored everywhere else.
    /// </summary>
    [StringLength(128)]
    public string PreviewFingerprint { get; set; }
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

    /// <summary>
    /// Fingerprint of exactly this preview (its inputs and every posted value of every line). Send it back
    /// as <see cref="CostingRunInputDto.PreviewFingerprint"/> when posting.
    /// </summary>
    public string PreviewFingerprint { get; set; }
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

    /// <summary>Same as <see cref="JournalVoucherPreviewDto.PreviewFingerprint"/> for the JV that was validated.</summary>
    public string PreviewFingerprint { get; set; }
}

/// <summary>
/// Where a cost-engine or Post-JV run stands, as legacy's status label put it
/// (<c>uscCostingFile.ascx.cs</c> <c>ASPxCallbackPanel_Callback</c>, lines 2117-2184).
/// </summary>
public enum FinanceRunStatus
{
    /// <summary>Nothing has been run on this screen yet (legacy "NotStarted").</summary>
    NotStarted = 0,

    /// <summary>Queued or in progress (legacy "Process Started" / "Running").</summary>
    Running = 1,

    /// <summary>Legacy "... ran successfully": finished with no failed row.</summary>
    Completed = 2,

    /// <summary>Legacy "... completed with some errors": finished with at least one failed row.</summary>
    CompletedWithErrors = 3,

    /// <summary>Legacy "... stopped successfully": the Stop button ended it.</summary>
    Stopped = 4,

    /// <summary>
    /// Still marked as running, but nothing has been heard from it for a long time: the server restarted or
    /// the job died. Not in legacy, whose status was a static variable that a restart simply reset.
    /// </summary>
    Interrupted = 5
}

/// <summary>
/// The status panel. Legacy <c>CalculateProcessStatus</c> (<c>uscCostingFile.ascx.cs</c> lines 2484-2585):
/// the All / Successful / Failed counters, the progress ring, the employee being processed, the speed and
/// the estimated finish, all worked out from the <c>ProjectLog</c> rows.
/// </summary>
public class CostingProcessStatusDto
{
    /// <summary>Legacy <c>SummaryAll</c>: every <c>ProjectLog</c> row.</summary>
    public int TotalRecords { get; set; }

    /// <summary>Legacy <c>SummarySuccess</c>: rows with status 0.</summary>
    public int SuccessRecords { get; set; }

    /// <summary>Legacy <c>SummaryFailed</c>: all minus successful, never below zero.</summary>
    public int FailedRecords { get; set; }

    /// <summary>True when a stop has been requested (legacy <c>ProjectLog.StopFlag</c>, or the run's own stop request).</summary>
    public bool StopRequested { get; set; }

    public FinanceRunStatus RunStatus { get; set; }

    /// <summary>True while a run is queued or in progress; the screen polls while this is set.</summary>
    public bool IsRunning { get; set; }

    /// <summary>The <c>ExecutionLog</c> row of the latest run on this screen, if any.</summary>
    public int? ExecutionLogId { get; set; }

    public DateTime? StartedAt { get; set; }

    public string StartedBy { get; set; }

    /// <summary>Legacy <c>NoOfEmployee</c> of the newest log row: the total the progress ring counts towards.</summary>
    public int NoOfEmployee { get; set; }

    /// <summary>Legacy <c>progressPercent</c>, 0-100.</summary>
    public int ProgressPercent { get; set; }

    /// <summary>Legacy <c>lblProgressCurrentEmp</c>: code and name of the newest row's employee, cut at 30 characters.</summary>
    public string CurrentEmployee { get; set; }

    /// <summary>Legacy <c>lblProgressSpeed</c>: rows per minute over the last minute of activity.</summary>
    public double SpeedPerMinute { get; set; }

    /// <summary>Legacy <c>remainingMin</c>; <c>null</c> when the speed is not known yet.</summary>
    public double? RemainingMinutes { get; set; }

    /// <summary>Legacy <c>DateTime.Now.AddMinutes(remainingMin)</c>.</summary>
    public DateTime? EstimatedCompletion { get; set; }
}

/// <summary>Returned when a cost-engine or Post-JV run has been queued.</summary>
public class FinanceRunStartedDto
{
    public int ExecutionLogId { get; set; }

    public string Message { get; set; }
}

/// <summary>Legacy <c>rdLogAll</c> / <c>rdLogSuccess</c> / <c>rdLogFailed</c> on the execution-log popup.</summary>
public enum FinanceRunLogFilter
{
    All = 0,
    Successful = 1,
    Failed = 2
}

/// <summary>The "Show details" grid (legacy <c>popupPayrollLog</c> / <c>grid_Payroll</c>).</summary>
public class GetFinanceRunLogInput : PagedAndSortedResultRequestDto
{
    public FinanceRunLogFilter StatusFilter { get; set; }

    /// <summary>Optional text matched against the employee code, the employee name and the message.</summary>
    [StringLength(256)]
    public string Filter { get; set; }
}

/// <summary>One row of the "Show details" grid (legacy <c>LogPopupModel</c>).</summary>
public class FinanceRunLogDto
{
    public int Id { get; set; }

    public string EmpCode { get; set; }

    /// <summary>Employee name in the user's language (legacy picked Arabic for ar-KW).</summary>
    public string Employee { get; set; }

    public DateTime RunTimeStamp { get; set; }

    /// <summary>0 = successful, 1 = failed.</summary>
    public int Status { get; set; }

    /// <summary>The full message: for a failed D365 batch, the request, D365's whole reply and the lines sent.</summary>
    public string Message { get; set; }

    public DateTime InPeriod { get; set; }
}

/// <summary>A page of the grid plus the three filter counts (legacy <c>RefreshPayrollLogCounts</c>).</summary>
public class FinanceRunLogOutput : PagedResultDto<FinanceRunLogDto>
{
    public int AllCount { get; set; }

    public int SuccessCount { get; set; }

    public int FailedCount { get; set; }
}

/// <summary>
/// Legacy <c>SetCurrentDates</c> (<c>uscCostingFile.ascx.cs</c> lines 1510-1525): the payroll area's control
/// record fills the period, and the posting date defaults to the period end.
/// </summary>
public class CostingPeriodContextDto
{
    public string PayrollAreaName { get; set; }

    public bool HasControlRecord { get; set; }

    /// <summary>Why nothing could be filled in (no area chosen, no control record); otherwise null.</summary>
    public string Message { get; set; }

    public int? PayrollMonth { get; set; }

    public int? PayrollYear { get; set; }

    public DateTime? CalendarMonthStart { get; set; }

    /// <summary>The period end: what legacy put in its read-only End Date box and sent as the pay-in date.</summary>
    public DateTime? CalendarMonthEnd { get; set; }

    /// <summary>Legacy: <c>txtPostingDate = CalendarMonthEnd</c> when the posting date box is empty.</summary>
    public DateTime? DefaultPostingDate { get; set; }
}

/// <summary>Legacy <c>SetDates</c> for the "Other" period (lines 1499-1508).</summary>
public class CostingPeriodDatesDto
{
    public int PayrollMonth { get; set; }

    public int PayrollYear { get; set; }

    public DateTime CalendarMonthStart { get; set; }

    public DateTime CalendarMonthEnd { get; set; }
}

/// <summary>
/// The employee popup (legacy <c>BuildEmployeePopupList</c>, <c>uscCostingFile.ascx.cs</c> lines 2632-2775).
/// The result rows reuse the Payroll Engine picker's <see cref="PayrollRunCandidateDto"/> shape.
/// </summary>
public class GetCostingFileCandidatesInput : PagedAndSortedResultRequestDto
{
    [Required]
    [StringLength(256)]
    public string PayrollAreaName { get; set; }

    public PayrollRunType PayrollRunType { get; set; }

    /// <summary>The period end date (legacy <c>txtEndDate</c>).</summary>
    public DateTime PayInDate { get; set; }

    public PayrollRunCandidateFilter StatusFilter { get; set; }

    [StringLength(256)]
    public string Filter { get; set; }

    /// <summary>Comma-separated codes currently ticked, so the Selected filter and count work.</summary>
    public string SelectedEmployeeCodes { get; set; }
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
