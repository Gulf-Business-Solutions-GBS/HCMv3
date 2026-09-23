using System.Threading.Tasks;

namespace HCMv3.Workflow.Services;

/// <summary>
/// Tells people what just happened to a request.
///
/// <para>
/// Every approval step carries its own notification settings, one per action (see <see cref="RequestDetail"/>),
/// and this is what reads them. Both channels go out together — the in-app bell and the email — to the same
/// list of people, so a recipient never sees one without the other.
/// </para>
///
/// <para>
/// Nothing here can fail an approval. A notification that cannot be sent — no configuration, no template, an
/// SMTP server that is down — is logged and stepped over: refusing to record an approval because an email
/// bounced would be a far worse outcome than a missing message, and the request itself is the record.
/// </para>
/// </summary>
public interface IWorkflowNotificationDispatcher
{
    /// <summary>
    /// Sends whatever the step at <paramref name="step"/> of this request's chain has configured for
    /// <paramref name="action"/>. Does nothing when that action has no configuration, which is the normal
    /// case for a chain nobody has set notifications up on.
    /// </summary>
    /// <returns>True when a configuration was found and acted on, so a caller with a default of its own --
    /// the reminder worker's -- knows whether to fall back to it.</returns>
    /// <param name="actingEmployeeId">Who performed the action, named in the message.</param>
    /// <param name="remark">Their comment, when the action carried one.</param>
    Task<bool> DispatchAsync(
        WorkflowRequest request,
        int step,
        WorkflowNotificationAction action,
        int? actingEmployeeId = null,
        string remark = null);
}
