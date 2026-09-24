using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.Workflow;

/// <summary>
/// Current approver assignment for a <see cref="WorkflowRequest"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Workflow.General.WorkFlowApprover</c> -- note the legacy file lives
/// under the <c>General</c> folder despite belonging conceptually to the core workflow engine). Legacy
/// computed <c>JobTitle</c> (<c>Approver.CurrentJobtitle.NameEn</c>) is a display-only helper, not ported
/// as entity state.
/// </summary>
public class WorkFlowApprover : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int WorkflowRequestId { get; set; }

    public virtual WorkflowRequest WorkflowRequest { get; set; }

    public virtual int? ApproverId { get; set; }

    public virtual Employee Approver { get; set; }

    public virtual bool IsApproved { get; set; }

    /// <summary>
    /// The step this row was assigned for.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// Added so the table can KEEP its history. It used to hold only the step a request was sitting on
    /// right now, because assigning a step deleted every row for the request before inserting the next
    /// step's. The manager who approved at step 1 therefore had no row the instant it reached step 2, and
    /// with it lost the request itself -- opening it again answered AbpAuthorizationException. Who approved
    /// what, and when, survived only in RequestLog, which is why the approvers panel had to reconstruct its
    /// own list from the action trail and mark the result as guessed.
    /// </para>
    /// <para>
    /// With the step recorded, rows persist and "who is this waiting on" becomes a filter rather than a
    /// side effect of deletion: the current approvers are the rows whose step matches the request's.
    /// Everything else on the request is history, and history is the thing a workflow is asked about six
    /// months later.
    /// </para>
    /// <para>
    /// Existing rows are backfilled to their request's current step, which is exactly what they meant
    /// before this column existed -- they were only ever the current step's rows.
    /// </para>
    /// </remarks>
    public virtual int WorkflowStep { get; set; }
}
