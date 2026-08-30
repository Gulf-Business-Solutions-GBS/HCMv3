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
    /// <returns>Id of the created <see cref="WorkflowRequest"/>.</returns>
    Task<int> CreateAndSubmitAsync(
        string requestType,
        int targetEntityId,
        int employeeId,
        string amendmentJson,
        int actingEmployeeId,
        int? requestSubTypeId = null);

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
