using System.Threading.Tasks;

namespace HCMv3.Workflow.Services;

/// <summary>
/// Generic workflow approval engine operating on the standalone, non-abstract <see cref="WorkflowRequest"/>
/// table (ported from legacy XAF <c>HCM10.Module.Controllers.WFRequest.RequestProcessApproval</c>'s
/// <c>Submit_Execute</c>/<c>Approve_Execute</c>/<c>Reject_Execute</c>/<c>SendBack_Execute</c>/
/// <c>recAssign_Execute</c> action handlers, plus the underlying step/approver-resolution logic from
/// <c>HCM10.Module.BusinessObjects.Workflow.WorkflowSetup.WorkFlowServices</c> (<c>SaveWorkflowRequest</c>,
/// <c>AssignWF</c>, <c>GetApprover</c>, <c>workflowApproved</c>) and
/// <c>HCM10.Module.BusinessObjects.Workflow.Services.RequestActionService</c> (<c>AssignNewApprover</c>).
///
/// SCOPE (deliberate, matching <see cref="WorkflowRequest"/>'s own documented scope decision): the legacy
/// action handlers are dominated by ~2000 lines of per-concrete-request-type (LeaveRequest, AbsenceRequest,
/// LoanRequest, DisciplinaryRequest, ... ~30 legacy subclasses) field-level validation and
/// "WorkflowComments"-column bookkeeping. None of those concrete subclasses were ported (see
/// <see cref="WorkflowRequest"/> remarks) so none of that per-type logic applies here; this port implements
/// only the generic, type-agnostic step-advancing engine shared by every request type: resolving the
/// approver(s) for a step from <see cref="RequestMaster"/>/<see cref="RequestDetail"/>/
/// <see cref="EmployeeWorkflowRole"/>, creating/consuming <see cref="WorkFlowApprover"/> rows, advancing
/// <see cref="WorkflowRequest.WorkflowStep"/>, and writing <see cref="RequestLog"/> audit rows. Legacy
/// notification/email side effects (<c>CreateNotification</c>, via the unported <c>NotificationConfig</c>/
/// <c>NotificationService</c>/<c>EmailService</c>) are dropped, matching the "blocked cross-module
/// dependency" pattern used throughout this port.
///
/// APPROVER RESOLUTION (simplification): legacy resolves the <see cref="RequestMaster"/> for a request via
/// reflection over the request's runtime CLR type plus an optional <c>WorkflowGroup</c>/org-based
/// override (<c>WorkflowGeneralService.GetRequestMaster</c>). Since <see cref="WorkflowRequest.RequestType"/>
/// is a plain string (no CLR subclass), this port matches <see cref="RequestMaster.Code"/> against it
/// instead. Per-step approver resolution supports the two most common legacy
/// <see cref="WorkflowApproverPersonType"/> modes: <c>Person</c> (via <see cref="EmployeeWorkflowRole"/>
/// active-today assignments to <see cref="RequestDetail.WorkflowRole"/>) and <c>PersonManager</c> (via the
/// requesting employee's active <c>EmployeeManager</c> row). The legacy <c>ManagerLevel2..7</c> modes
/// (walking N managers up the chain) and the <c>WorkflowGroup</c>/org-scoped override path are not
/// implemented -- both fall back to <see cref="RequestMaster.DefaultApprover"/> if set, else the step fails
/// to resolve an approver (mirrors legacy's own "Approver not found" exception).
/// </summary>
public interface IWorkflowApprovalService
{
    /// <summary>
    /// Moves a <see cref="WorkflowRequest"/> from <see cref="RequestStatus.Draft"/> into the approval chain:
    /// sets <c>WorkflowStep = 1</c>, resolves and assigns the step-1 approver(s), and sets
    /// <see cref="RequestStatus.Open"/>. Ported from legacy <c>Submit_Execute</c> (generic portion only).
    /// </summary>
    Task SubmitAsync(int requestId, int actingEmployeeId);

    /// <summary>
    /// Records the acting employee's approval of the current step. If the step's
    /// <see cref="ApprovalRequire"/> rule is now satisfied, either advances to the next step (resolving its
    /// approver(s)) or -- if this was the last step -- finalizes the request as
    /// <see cref="RequestStatus.Approved"/>. Ported from legacy <c>Approve_Execute</c> /
    /// <c>WorkFlowServices.AssignWF</c>/<c>workflowApproved</c> (generic portion only).
    /// </summary>
    Task ApproveAsync(int requestId, int actingEmployeeId, string remark);

    /// <summary>
    /// Sets <see cref="RequestStatus.Rejected"/> and logs the remark. Ported from legacy
    /// <c>Reject_Execute</c> (generic portion only -- the ~15 per-request-type
    /// <c>RequestStatus = RequestStatus.Rejected</c>/<c>WorkflowComments</c> blocks collapse to one
    /// generic assignment since there is no concrete subclass in this port).
    /// </summary>
    Task RejectAsync(int requestId, int actingEmployeeId, string remark);

    /// <summary>
    /// Returns the request to the submitter for correction: resets <c>WorkflowStep = 1</c> and
    /// <see cref="RequestStatus.Draft"/>, clearing any in-flight <see cref="WorkFlowApprover"/> rows.
    /// ADAPTED from legacy <c>SendBack_Execute</c>: the legacy handler delegates entirely to
    /// <c>WorkFlowServices.SaveWorkflowRequest(..., "SendBack", ...)</c>, whose generic (non-per-type)
    /// SendBack semantics were not recoverable from the legacy source within this port's scope; "return to
    /// draft for the submitter to correct and resubmit" is the standard, documented interpretation used
    /// here.
    /// </summary>
    Task SendBackAsync(int requestId, int actingEmployeeId, string remark);

    /// <summary>
    /// Delegates the current step's pending approval(s) from the acting employee to
    /// <paramref name="newApproverEmployeeId"/>. Ported from legacy <c>recAssign_Execute</c> /
    /// <c>RequestActionService.AssignNewApprover</c>.
    /// </summary>
    Task ReassignAsync(int requestId, int actingEmployeeId, int newApproverEmployeeId, string remark);

    /// <summary>
    /// Records that this employee opened the request, for steps whose configuration asks for it
    /// (<c>RequestDetail.EnableReadLog</c>). A no-op otherwise, and never throws -- viewing must not fail
    /// because logging the view failed.
    /// </summary>
    Task MarkAsReadAsync(int requestId, int actingEmployeeId);
}
