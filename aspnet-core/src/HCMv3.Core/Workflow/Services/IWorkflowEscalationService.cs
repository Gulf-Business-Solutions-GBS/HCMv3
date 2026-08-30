using System.Collections.Generic;
using System.Threading.Tasks;

namespace HCMv3.Workflow.Services;

/// <summary>
/// The time-based half of the workflow engine: moving requests that have been sitting too long, and nudging
/// approvers who have not acted.
///
/// <para>
/// Everything else in the engine happens because somebody clicked something. These two happen because nobody
/// did, which is why they need a periodic worker rather than a call site -- and why the configuration for
/// them (<c>ShouldEscalate</c>, <c>EscalateAfterDays</c>, <c>ReminderDays</c>) sat unread until now: there
/// was nothing running to read it.
/// </para>
/// </summary>
public interface IWorkflowEscalationService
{
    /// <summary>
    /// Escalates every open request whose current step has stalled past its <c>EscalateAfterDays</c>.
    /// </summary>
    /// <returns>The requests that were escalated, for logging by the caller.</returns>
    Task<List<WorkflowEscalationResult>> EscalateStalledRequestsAsync();

    /// <summary>
    /// Finds the approvers due a reminder today, without sending anything -- delivery belongs to the caller,
    /// which knows how to reach people.
    /// </summary>
    Task<List<WorkflowReminderDue>> GetRemindersDueAsync();
}

/// <summary>One escalation that happened, described well enough to log without re-querying.</summary>
public class WorkflowEscalationResult
{
    public int WorkflowRequestId { get; set; }

    public string RequestNumber { get; set; }

    public int FromStep { get; set; }

    public int ToStep { get; set; }

    /// <summary>True when the step advanced to the next level; false when it stayed put and moved to the
    /// step's nominated escalation approvers instead.</summary>
    public bool MovedToNextLevel { get; set; }

    public List<int> NewApproverEmployeeIds { get; set; } = new List<int>();
}

/// <summary>An approver who has been sitting on a request long enough to be reminded.</summary>
public class WorkflowReminderDue
{
    public int WorkflowRequestId { get; set; }

    public string RequestNumber { get; set; }

    public string SummaryEn { get; set; }

    public string SummaryAr { get; set; }

    public int WorkflowStep { get; set; }

    /// <summary>How long the step has been waiting, which is what makes the reminder worth reading.</summary>
    public int PendingDays { get; set; }

    public List<int> ApproverEmployeeIds { get; set; } = new List<int>();
}
