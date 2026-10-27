using System.Threading.Tasks;

namespace HCMv3.Workflow.Services;

/// <summary>
/// Raises an amendment: a request to CHANGE a transaction that has already been approved.
///
/// <para>
/// This is the answer to the one thing the single-table design does not solve on its own. Everywhere else,
/// keeping the request and the transaction in one row is what stops them diverging -- there is no second row
/// to disagree with. But once a row is approved and live, someone editing it directly bypasses approval
/// entirely: payroll simply sees different numbers the next morning, with no record that anyone decided
/// anything. Legacy had the mirror-image version of this problem, where editing the transaction left the
/// request behind and the two drifted apart permanently.
/// </para>
///
/// <para>
/// An amendment closes it by routing the change through the same approval chain the original went through,
/// while leaving the live row alone until the change is granted. The proposed values ride on the request.
/// </para>
/// </summary>
public interface IWorkflowAmendmentService
{
    /// <summary>
    /// Creates and submits an amendment request against an approved transaction row.
    /// </summary>
    /// <param name="requestType">Catalog code of the transaction type, e.g. "EmployeeAbsence".</param>
    /// <param name="targetEntityId">Primary key of the row being amended.</param>
    /// <param name="employeeId">The employee the transaction belongs to.</param>
    /// <param name="amendmentJson">The proposed values, in whatever shape the type's handler reads.</param>
    /// <param name="actingEmployeeId">Who is raising the amendment.</param>
    /// <param name="requestSubTypeId">Sub-type for routing, when the type uses one.</param>
    /// <param name="requestAction">Which action this is -- a change, a cancellation or a return. Recorded on the
    /// request, and what lets each follow an approval chain of its own.</param>
    /// <returns>Id of the created <see cref="WorkflowRequest"/>.</returns>
    Task<int> CreateAndSubmitAsync(
        string requestType,
        int targetEntityId,
        int employeeId,
        string amendmentJson,
        int actingEmployeeId,
        int? requestSubTypeId = null,
        WorkflowRequestAction requestAction = WorkflowRequestAction.Amendment);

    /// <summary>
    /// Creates the amendment WITHOUT sending it: the requester's own draft, which only they can see and which
    /// they later submit (<see cref="ResubmitAsync"/>) or withdraw. It still counts as the one request in
    /// flight against the row, so a second cannot be raised beside it.
    /// </summary>
    Task<int> CreateDraftAsync(
        string requestType,
        int targetEntityId,
        int employeeId,
        string amendmentJson,
        int actingEmployeeId,
        int? requestSubTypeId = null,
        WorkflowRequestAction requestAction = WorkflowRequestAction.Amendment);

    /// <summary>Replaces the proposed values on a draft (or sent-back) amendment without submitting it.</summary>
    Task UpdateDraftAsync(int requestId, string amendmentJson, int actingEmployeeId, int? requestSubTypeId = null);

    /// <summary>
    /// Sends an amendment that was SENT BACK to its requester round again, with the values they corrected.
    /// The request keeps its number, its log and its comments; only the proposed values are replaced. The
    /// before-snapshot is left as taken when the change was first asked for.
    /// </summary>
    Task ResubmitAsync(int requestId, string amendmentJson, int actingEmployeeId);

    /// <summary>
    /// The amendment currently in flight against a row -- awaiting approval or sent back -- or null.
    /// </summary>
    Task<WorkflowRequest> GetInFlightAsync(string requestType, int targetEntityId);

    /// <summary>
    /// Whether this transaction TYPE can be amended at all: its handler supports amendment and the catalog
    /// has it active and workflow-enabled. Asked once per screen rather than once per row.
    /// </summary>
    Task<bool> IsAmendmentEnabledAsync(string requestType);

    /// <summary>
    /// Whether an amendment can be raised against this particular row right now -- as
    /// <see cref="IsAmendmentEnabledAsync"/>, and nothing already in flight against the same row.
    /// </summary>
    Task<bool> CanAmendAsync(string requestType, int targetEntityId);
}
