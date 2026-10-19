using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.Workflow;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Leave an employee intends to take: the dates, how many days that is, and what their annual leave balance is
/// projected to be when it starts (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Workflow.Leave.LeavePlanner</c>, a <c>WorkflowRequest</c> subclass with
/// request code "14").
///
/// <para>
/// <b>A plan is information, not leave.</b> Approving one books nothing, charges no quota and creates no
/// <see cref="EmployeeAbsence"/>. Legacy's only approval block on this object (<c>LeavePlanner.OnSaving</c>,
/// the <c>RequestStatus.Approved</c> region) is commented out and was copy-pasted from the asset request -- it
/// would have created an <c>EmployeeAsset</c>. What approval buys is visibility: an approved plan is what the
/// Leave Planner shows a manager as "planned" rather than "asked for".
/// </para>
///
/// <para>
/// No overlap rule, also as legacy. <c>LeavePlanner.PerformValidations</c> checks only that the employee and
/// both dates are present and that the end is not before the start; the leave-conflict banner
/// (<c>LeaveConflictController.SetConflictText</c>) only ever ran for leave requests, never for plans.
/// </para>
///
/// <para>
/// One row, not two. Legacy's plan WAS its workflow request; here the plan is its own row and
/// <see cref="WorkflowStatus"/> keeps it out of the planner's "approved" view until it is signed off -- see
/// <see cref="IWorkflowEnabledEntity"/>.
/// </para>
/// </summary>
public class LeavePlan : FullAuditedEntity, IMustHaveTenant, IHasEmployeeId, IWorkflowEnabledEntity
{
    public const int MaxCommentsLength = 1000;

    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    /// <summary>
    /// Calendar days from start to end, both included -- legacy <c>PlannedDays</c>
    /// (<c>EndDate.Subtract(StartDate).Days + 1</c>), labelled "Planned Calendar Days" on the legacy ESS form.
    /// Deliberately NOT the leave-day count: a plan names no leave type, so there is no type whose weekend and
    /// holiday rules could be applied. Stored, as legacy stored it at submit.
    /// </summary>
    public virtual double PlannedDays { get; set; }

    /// <summary>
    /// The employee's annual leave (type code "1") balance projected to <see cref="StartDate"/>, through
    /// <c>IAccrualService.GetAsOnDateQuotaWithProjectionAsync</c> -- legacy <c>ProjectedLeaveBalance</c>.
    ///
    /// <para>
    /// Legacy computed it live on every read and never stored it. Here it is a snapshot taken when the plan
    /// is saved and again when it is submitted, so the approver and the list show the figure the employee
    /// saw rather than one that drifts as accrual moves on. Null when no annual leave type is configured.
    /// </para>
    /// </summary>
    public virtual double? ProjectedLeaveBalance { get; set; }

    /// <summary>Legacy <c>RequestComments</c>: what the employee says to the approver.</summary>
    public virtual string Comments { get; set; }

    /// <inheritdoc/>
    public virtual int? WorkflowRequestId { get; set; }

    /// <inheritdoc/>
    public virtual RequestStatus? WorkflowStatus { get; set; }
}
