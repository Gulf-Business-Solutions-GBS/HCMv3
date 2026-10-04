using System.Threading.Tasks;

namespace HCMv3.Workflow.Services;

/// <summary>
/// Decides which approval configuration (<see cref="RequestMaster"/>) governs a given request, and whether
/// a transaction type runs through workflow at all.
///
/// <para>
/// Replaces two things. First, the resolution that <c>WorkflowApprovalService</c> was doing inline --
/// matching <c>RequestMaster.Code</c> directly against <see cref="WorkflowRequest.RequestType"/>, a
/// documented simplification that ignored workflow groups and sub-types entirely, so every employee got the
/// same chain and "annual leave needs three approvals, sick leave needs one" could not be expressed.
/// Second, legacy <c>WorkflowGeneralService.GetRequestMaster</c>'s fallback: a 28-case switch statement
/// mapping type names to magic <c>RequestMaster.Code</c> strings ("LeaveRequest" -> "4"), now a
/// <see cref="WorkflowTransactionType"/> row.
/// </para>
///
/// <para><b>Resolution order</b>, most specific first, mirroring legacy:</para>
/// <list type="number">
/// <item><description>
/// If <c>ManageWorkflowSettings.EnableWorkflowGroup</c> is on, find the employee's
/// <see cref="WorkflowGroup"/> from their current-dated <c>EmployeeOrgAssignment.WorkflowGroupId</c>,
/// falling back to the group flagged <c>IsDefault</c>. Within that group, try
/// <c>WorkflowGroupDetail</c> three ways, loosening each time: request type + sub-type + custom sub-type,
/// then type + sub-type, then type alone.
/// </description></item>
/// <item><description>
/// Otherwise (or if nothing matched), use the type's
/// <see cref="WorkflowTransactionType.DefaultRequestMasterId"/>.
/// </description></item>
/// </list>
///
/// <para>
/// Unlike legacy, nothing here is swallowed. <c>GetRequestMaster</c> wrapped its whole body in a
/// <c>catch { }</c> with the rethrow commented out, so a misconfiguration returned null and surfaced later
/// as an unrelated failure. A configuration problem here throws where it happens.
/// </para>
/// </summary>
public interface IRequestMasterResolver
{
    /// <summary>
    /// Resolves the approval configuration for an existing request. Throws
    /// <c>UserFriendlyException</c> when the type is unknown, inactive, or has no configuration reachable by
    /// any route -- a request that cannot be routed must fail loudly at submit, not sit in limbo.
    /// </summary>
    Task<RequestMaster> ResolveAsync(WorkflowRequest request);

    /// <summary>
    /// Resolution without an existing request, for deciding the chain before one is created (and for
    /// showing the employee who will approve their request before they submit it).
    /// <paramref name="employeeId"/> may be null, which skips the workflow-group step.
    /// </summary>
    /// <paramref name="requestAction"/> picks the chain configured for that action (amendment, cancellation, return)
    /// where one is; with none configured, every action follows the chain a new request would.
    Task<RequestMaster> ResolveAsync(
        string requestType,
        int? employeeId,
        int? requestSubTypeId,
        int? customSubTypeId = null,
        WorkflowRequestAction requestAction = WorkflowRequestAction.New);

    /// <summary>The catalog row for <paramref name="code"/>, or null when the type is not registered.</summary>
    Task<WorkflowTransactionType> GetTransactionTypeAsync(string code);

    /// <summary>
    /// Whether records of this type must be routed for approval. False means the caller saves the row
    /// directly and leaves <c>WorkflowStatus</c> null, making it immediately effective. This is the switch
    /// that lets a tenant run workflow on leave but not on asset issue, without a code change.
    /// </summary>
    Task<bool> IsWorkflowEnabledAsync(string requestType);
}
