using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Where a payroll area's period stands, as the Payroll Engine screen shows it. Legacy had these as the three
/// texts of <c>PayrollControlRecord.PayrollStatus</c> plus "Payroll control record is empty".
/// </summary>
public enum PayrollRunPeriodStatus
{
    /// <summary>The payroll area has no control record. Nothing can be run.</summary>
    NoControlRecord = 0,

    /// <summary>Neither locked nor closed: "Data Correction for {month}". No run is suggested.</summary>
    DataCorrection = 1,

    /// <summary>Locked, not closed: "Regular of {month}". Legacy selects Regular and colours it green.</summary>
    Regular = 2,

    /// <summary>Closed: "Offcycle of {next month}". Legacy selects OffCycle and colours it yellow.</summary>
    OffCycle = 3,

    /// <summary>Both flags set, which legacy never produces; shown blank as legacy does.</summary>
    Unknown = 4
}

/// <summary>What the Payroll Engine screen fills in when a payroll area is chosen (legacy <c>SetCurrentDates</c>).</summary>
public class PayrollRunContextDto
{
    public string PayrollAreaName { get; set; }

    public bool HasControlRecord { get; set; }

    /// <summary>"Payroll control record is empty" when there is none, otherwise null.</summary>
    public string Message { get; set; }

    public int? PayrollMonth { get; set; }

    public int? PayrollYear { get; set; }

    public DateTime? CalendarMonthStart { get; set; }

    public DateTime? CalendarMonthEnd { get; set; }

    public bool PeriodLocked { get; set; }

    public bool PeriodClosed { get; set; }

    public PayrollRunPeriodStatus PeriodStatus { get; set; }

    /// <summary>Legacy's status text, e.g. "Regular of October-2026".</summary>
    public string PeriodStatusText { get; set; }

    /// <summary>Regular when locked, OffCycle when closed, null when neither (legacy leaves the choice alone).</summary>
    public PayrollRunType? SuggestedRunType { get; set; }

    /// <summary>A regular run is accepted: locked and not closed.</summary>
    public bool RegularRunAllowed { get; set; }

    /// <summary>An off-cycle run is accepted: closed.</summary>
    public bool OffCycleRunAllowed { get; set; }

    /// <summary>
    /// The pay-in date a REGULAR run must carry: the control record's <c>CalendarMonthEnd</c>
    /// (<c>PayrollControlRecordGuard.EnsureRegularRunAllowed</c>).
    /// </summary>
    public DateTime? RegularPayInDate { get; set; }

    /// <summary>
    /// The pay-in date an OFF-CYCLE run ends up with: the month end of the period after the control record
    /// (<c>PayrollControlRecordGuard.NextPeriodPayInDate</c>). The engine works this out itself; it is shown so the
    /// operator can see which month the off-cycle lands in.
    /// </summary>
    public DateTime? OffCyclePayInDate { get; set; }

    /// <summary>Time setting <c>EnableTimeEngineAutorun</c>.</summary>
    public bool EnableTimeEngineAutorun { get; set; }

    /// <summary>
    /// Whether the off-cycle payment date is asked for. Legacy hides it when time-engine autorun is on, because the
    /// engine then ignores it.
    /// </summary>
    public bool ShowOffCyclePaymentDate { get; set; }

    /// <summary>The engine accepts an off-cycle payment date only inside the next period; these are its first and last days.</summary>
    public DateTime? OffCyclePaymentDateFrom { get; set; }

    public DateTime? OffCyclePaymentDateTo { get; set; }

    /// <summary>Suggested payment date: today when the payroll setting picks up to today and today is in that period, otherwise its end.</summary>
    public DateTime? DefaultOffCyclePaymentDate { get; set; }
}

/// <summary>Start and end of a payroll month for a payroll area (legacy <c>SetDates</c>, used by simulation's Other period).</summary>
public class PayrollRunPeriodDatesDto
{
    public int PayrollMonth { get; set; }

    public int PayrollYear { get; set; }

    public DateTime CalendarMonthStart { get; set; }

    public DateTime CalendarMonthEnd { get; set; }
}

/// <summary>One choice of the off-cycle reason dropdown (legacy <c>BindOffReasonType</c>, the PayrollReasonType table).</summary>
public class PayrollRunOffCycleReasonDto
{
    public int? Id { get; set; }

    public string Code { get; set; }

    /// <summary>
    /// What to send as <c>RunPayrollEngineInput.OffCycleReason</c>: always the ENGLISH name, because the engine
    /// matches it by value against "End Of Service" / "Advance Leave" / "Leave Encashment".
    /// </summary>
    public string Value { get; set; }

    /// <summary>The name in the user's language.</summary>
    public string DisplayName { get; set; }

    /// <summary>False for a reason the engine has no special handling for; the employee picker lists nobody for it.</summary>
    public bool IsKnownToEngine { get; set; }
}

/// <summary>Processed or Pending, as legacy's employee popup shows it.</summary>
public enum PayrollRunCandidateStatus
{
    Pending = 0,
    Processed = 1
}

/// <summary>The four filters of legacy's employee popup.</summary>
public enum PayrollRunCandidateFilter
{
    All = 0,
    Processed = 1,
    Pending = 2,
    Selected = 3
}

public class GetPayrollRunCandidatesInput : PagedAndSortedResultRequestDto
{
    [Required]
    [StringLength(256)]
    public string PayrollAreaName { get; set; }

    public PayrollRunType PayrollRunType { get; set; }

    /// <summary>The reason's English name (the <see cref="PayrollRunOffCycleReasonDto.Value"/>). Off-cycle only.</summary>
    [StringLength(256)]
    public string OffCycleReason { get; set; }

    public PayrollRunCandidateFilter StatusFilter { get; set; }

    /// <summary>Text search on employee code and name.</summary>
    [StringLength(256)]
    public string Filter { get; set; }

    /// <summary>The comma-separated codes currently in the Employee Code box; they come back marked as selected.</summary>
    public string SelectedEmployeeCodes { get; set; }

    /// <summary>
    /// Simulation on another period: list the employees as at that month's end instead of the control record's.
    /// Both must be set to take effect.
    /// </summary>
    [Range(1, 12)]
    public int? SimulationMonth { get; set; }

    [Range(1900, 9999)]
    public int? SimulationYear { get; set; }
}

public class PayrollRunCandidateDto
{
    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    public PayrollRunCandidateStatus PayrollStatus { get; set; }

    public bool IsSelected { get; set; }
}

/// <summary>A page of candidates plus the counts legacy shows on its filter buttons (over the whole list, not the page).</summary>
public class PayrollRunCandidatesOutput : PagedResultDto<PayrollRunCandidateDto>
{
    public int AllCount { get; set; }

    public int ProcessedCount { get; set; }

    public int PendingCount { get; set; }

    public int SelectedCount { get; set; }

    /// <summary>The period end the list was built for.</summary>
    public DateTime? PayrollDate { get; set; }

    public string PeriodStatusText { get; set; }
}

/// <summary>Which run's per-employee results to export, and with which of the grid's filters.</summary>
public class GetPayrollRunEmployeesToExcelInput
{
    /// <summary>Null = the latest Payroll Engine run, as the grid shows.</summary>
    public long? RunId { get; set; }

    [StringLength(256)]
    public string Filter { get; set; }

    /// <summary>true = failures only, false = successes only, null = everyone.</summary>
    public bool? OnlyFailed { get; set; }
}
