namespace HCMv3.Workflow.Services;

/// <summary>
/// Says which client the action being processed arrived from, for <see cref="RequestLog.Platform"/>.
/// </summary>
public interface IWorkflowPlatformProvider
{
    /// <summary>
    /// The platform for the action in flight. Never throws and never returns null -- an unrecognised or
    /// absent client is reported as <see cref="WorkflowPlatform.Core"/> or
    /// <see cref="WorkflowPlatform.ESS"/> rather than failing an approval over a log column.
    /// </summary>
    WorkflowPlatform Current { get; }
}
