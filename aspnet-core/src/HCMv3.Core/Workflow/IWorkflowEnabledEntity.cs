namespace HCMv3.Workflow;

/// <summary>
/// Marks a transaction entity that can be put through the approval workflow *in place*, rather than being
/// shadowed by a second "request" table the way legacy HRMS did it (<c>LeaveRequest</c> mirroring
/// <c>EmployeeLeave</c>, and ~50 more pairs). One row is both the request and the transaction; these two
/// columns are the entire cost of that.
///
/// <para>
/// Workflow participation is configuration, not a property of the entity: whether a given transaction type
/// runs through approval is decided by its <c>WorkflowTransactionType</c> catalog row, not by a flag here.
/// An entity implementing this interface is one that *can* be workflow-managed; whether a particular row
/// actually is shows up as a non-null <see cref="WorkflowStatus"/>.
/// </para>
/// </summary>
public interface IWorkflowEnabledEntity
{
    /// <summary>
    /// The <see cref="WorkflowRequest"/> currently governing this row, or <c>null</c> when the row was
    /// created directly (workflow disabled for its type, or created by HR through an admin screen).
    /// </summary>
    int? WorkflowRequestId { get; set; }

    /// <summary>
    /// Denormalised copy of the governing request's status, kept on the row itself so that the
    /// <c>ApprovedOnly</c> data filter can be a plain indexable column comparison rather than a join to
    /// <c>WorkflowRequests</c> on every query of every workflow-enabled table.
    ///
    /// <para>
    /// <c>null</c> means "not workflow-managed" and is treated as effective -- which is what makes this
    /// change backward compatible: every row that exists today keeps behaving exactly as it does now.
    /// </para>
    /// <para>
    /// Written only by the workflow engine (and by the app service that creates a row with workflow
    /// disabled). Nothing else should assign it; the engine is the single writer that keeps it in step with
    /// <c>WorkflowRequest.RequestStatus</c>.
    /// </para>
    /// </summary>
    RequestStatus? WorkflowStatus { get; set; }
}
