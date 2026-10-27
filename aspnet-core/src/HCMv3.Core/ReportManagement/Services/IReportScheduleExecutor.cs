using System.Threading.Tasks;

namespace HCMv3.ReportManagement.Services;

/// <summary>Outcome of executing one schedule.</summary>
public class ReportScheduleExecutionResult
{
    public long RunId { get; set; }

    public int SliceCount { get; set; }

    public int RecipientCount { get; set; }

    public int DeliveredCount { get; set; }

    public int EmptyCount { get; set; }

    public int FailedCount { get; set; }

    public ReportRunStatus Status { get; set; }

    public string Message { get; set; }
}

/// <summary>
/// Runs one schedule end to end: resolve slices, resolve each recipient's scope, render, deliver, log.
///
/// ORDERING IS THE SECURITY CONTROL. Scope is resolved per recipient and applied BEFORE rendering, so
/// an out-of-scope row never reaches a file, let alone an outbox. Rendering once and filtering
/// afterwards would be faster and is deliberately not done -- it puts the whole dataset in memory next
/// to a delivery loop, and one wrong branch there is an unrecallable e-mail.
///
/// A failure delivering to one recipient must not abort the run. The rest of the department heads
/// still need their reports, so failures are recorded per slice and the run reports
/// PartiallySucceeded.
/// </summary>
public interface IReportScheduleExecutor
{
    /// <param name="scheduleId">The schedule to run.</param>
    /// <param name="isManual">True when a person pressed Run rather than the scheduler firing it.</param>
    Task<ReportScheduleExecutionResult> ExecuteAsync(int scheduleId, bool isManual);
}
