using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.Workflow;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// A piece of work ASSIGNED to an employee -- a subject, a description, a start and an end date, and a status
/// of Opened/Working/OnHold/Closed (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Workflow.General.TaskRequest</c>, legacy workflow request code "17").
///
/// <para>
/// <b>This is NOT <see cref="EmployeeTask"/>, and the two must not be merged.</b> The names sit close enough
/// together that unifying them looks like an obvious tidy-up, so here is why it is not:
/// <see cref="EmployeeTask"/> is the port of legacy
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeTask</c> -- an HR REMINDER against an
/// employee record, carrying <c>TaskTypeId</c>, <c>TaskDate</c>, <c>ReminderDate</c>, <c>MonitoringType</c>,
/// <c>MonitoringCount</c> and <c>MonitoringUnit</c>. It exists to make somebody in HR chase a visa renewal on
/// a schedule; it has no subject, no assignee-versus-creator distinction, and it never went near approval.
/// This entity is the port of a completely separate legacy class, in a different legacy folder, that
/// inherited <c>WorkflowRequest</c> and routed for approval. Same English word, two different things. Folding
/// either into the other would mean one table with two disjoint halves and a nullable column per field.
/// </para>
///
/// <para>
/// <b>One class: the task IS the request.</b> There is no <c>TaskRequest</c> shadow table here and there must
/// not be one. This is the cleanest case in the whole port for the single-table design, because legacy's
/// <c>TaskRequest</c> had NO approval side effect whatsoever -- nothing anywhere in <c>OnSaving</c>,
/// <c>WorkFlowServices</c> or <c>RequestProcessApproval</c> runs when the status becomes Approved; the only
/// type-specific approval-path code is comment concatenation on REJECTION. So there is no transaction row to
/// create on approval, and none is missing. Under this design that absence is not a gap to be filled: the row
/// is created hidden by the <c>ApprovedOnly</c> filter, and approval means precisely that it becomes visible
/// and effective. Nobody should later invent a side effect that never existed.
/// </para>
///
/// <para>
/// <b>Creator versus assignee.</b> <see cref="EmployeeId"/> is the ASSIGNEE -- the person expected to do the
/// work. Whoever raised it is already recorded by <c>FullAuditedEntity.CreatorUserId</c>, so no second
/// employee column is needed (legacy carried it as <c>SysCreator</c>). The distinction is load-bearing rather
/// than decorative: legacy's <c>IsCreator</c> property existed so that once the task was in the approval
/// chain the assignee could no longer rewrite the subject, description or dates of the job they had been
/// given. That rule is ported, but as a SERVER-SIDE check in
/// <c>EmployeeAssignedTasksAppService.UpdateAsync</c>. Legacy enforced it only as an XAF
/// <c>[Appearance(Enabled = false)]</c> UI rule, which greyed the boxes out on one screen and stopped
/// nothing at all -- any other view, import or direct save bypassed it entirely.
/// </para>
///
/// <para>
/// <b>Legacy defects deliberately not reproduced.</b> (1) Legacy's <c>OnSaving()</c> never calls
/// <c>base.OnSaving()</c>. In XPO the base hook is what completes the save pipeline, so this class has been
/// half-saving for years -- which is the reason its observed behaviour should not be treated as authoritative
/// when porting: what it appears to do and what it was written to do are not the same thing. (2) Its
/// <c>catch (Exception ex) { throw ex; }</c> resets the stack trace to the rethrow point, hiding where
/// failures came from. (3) <c>PerformValidations</c> threw raw <c>Exception</c> with hardcoded English
/// strings; every one of its rules is kept (subject, start date, end date, end-not-before-start) but as
/// <c>UserFriendlyException</c> with localized keys, in
/// <c>AssignedTaskWorkflowTransactionHandler.ValidateBeforeSubmitAsync</c> and on the DTO.
/// </para>
/// </summary>
public class EmployeeAssignedTask : FullAuditedEntity, IMustHaveTenant, IHasEmployeeId, IWorkflowEnabledEntity
{
    public int TenantId { get; set; }

    /// <summary>
    /// The ASSIGNEE -- who has to do the work. Not the person who raised it; that is
    /// <c>FullAuditedEntity.CreatorUserId</c>. See the class remarks on why the two are kept apart.
    /// </summary>
    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    /// <summary>
    /// What the task is, in one line.
    ///
    /// <para>
    /// Bounded at 200, where legacy left it unbounded (XPO's default maps to <c>nvarchar(100)</c> in some
    /// providers and <c>nvarchar(max)</c> in others, so the legacy column width was in practice an accident
    /// of configuration). A one-line subject that can hold a megabyte is a defect in both directions: it
    /// cannot be indexed, and it lets somebody paste an entire email into a grid column. 200 is the same
    /// bound this codebase uses for other short titles and is comfortably above any real subject line.
    /// </para>
    /// </summary>
    [StringLength(200)]
    public virtual string Subject { get; set; }

    /// <summary>The detail of what is being asked for. 500 matches legacy's <c>[Size(500)]</c>.</summary>
    [StringLength(500)]
    public virtual string Description { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    /// <summary>
    /// Progress on the work. This is the one field the ASSIGNEE may move once the task is in the chain --
    /// which is the whole point of the field, since they are the only one who knows.
    /// </summary>
    public virtual AssignedTaskStatus TaskStatus { get; set; }

    /// <summary>
    /// Legacy's <c>RequestComments</c>. Its companion <c>WorkflowComments</c> is not ported: that column was
    /// a hand-rolled string accumulation of every approver's remark ("comment + newline + name + newline +
    /// date", concatenated on each hop), which this port keeps properly as <c>WorkflowComment</c> rows
    /// against the request.
    /// </summary>
    [StringLength(1000)]
    public virtual string Comments { get; set; }

    /// <inheritdoc cref="IWorkflowEnabledEntity.WorkflowRequestId"/>
    public virtual int? WorkflowRequestId { get; set; }

    /// <inheritdoc cref="IWorkflowEnabledEntity.WorkflowStatus"/>
    public virtual RequestStatus? WorkflowStatus { get; set; }
}
