using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.Workflow;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// A one-off payment raised by HR for MANY employees at once, put forward for approval as a single document
/// (ported from legacy XAF <c>HCM10.Module.BusinessObjects.Workflow.Payroll.PaymentRequest</c> and its
/// <c>PaymentRequestDetail</c>).
///
/// <para>
/// <b>Why this type gets a new entity when the rest of this port exists to delete exactly that.</b> The rule
/// everywhere else is one class: the transaction row IS the request, and a shadow <c>*Request</c> table is
/// deleted rather than ported -- <c>LeaveRequest</c>/<c>EmployeeAbsence</c> were 1:1 mirrors of one another,
/// so keeping both could only ever produce an approved request with no leave, or leave for a request that was
/// later refused. This is not that shape. A payment batch is a HEADER OVER MANY EMPLOYEES: one approval
/// produces N <see cref="EmployeePayment"/> rows, one per detail line. <see cref="EmployeePayment"/> is
/// per-employee and per-line, so there is no single pre-existing transaction row to carry
/// <see cref="WorkflowRequestId"/> and <see cref="WorkflowStatus"/>, and
/// <c>WorkflowRequest.TargetEntityId</c> is one integer. This row exists to BE that one integer. It is the
/// PARENT of <see cref="EmployeePayment"/>, not a duplicate of it -- the same argument that gives
/// <c>TimeLogApproval</c> its header/detail pair.
/// </para>
///
/// <para>
/// So: do not "consolidate" this away by moving <see cref="WorkflowRequestId"/> onto
/// <see cref="EmployeePayment"/>. Doing that would mean either N requests for one HR decision (N approver
/// inbox rows for one keystroke) or an arbitrary "primary" payment row standing in for the batch. The batch
/// is the thing that is approved because the batch is the thing a human decided.
/// </para>
///
/// <para>
/// <b>What approval buys.</b> Nothing exists in <see cref="EmployeePayment"/> until the chain completes:
/// <c>PaymentBatchWorkflowTransactionHandler.OnApprovedAsync</c> is the only code that creates payment rows
/// from a batch, and it creates them exactly once. Legacy created them from <c>OnSaving</c> whenever the
/// request happened to be saved while its status read Approved, so re-saving an approved batch duplicated
/// every single payment in it -- real money, paid twice.
/// </para>
///
/// <para>
/// <b>No <c>IHasEmployeeId</c>, deliberately.</b> A batch has no one employee; its employees are its
/// <see cref="Details"/>. Rather than bolt a meaningless <c>EmployeeId</c> onto the header to satisfy the
/// generic constraint on <c>IAdminTransactionSubmitter.CreateAndSubmitAsync</c>, the submitter grew a
/// variant that takes the subject employee explicitly -- see
/// <c>IAdminTransactionSubmitter.CreateAndSubmitForSubjectAsync</c>. The subject of a payment batch is the
/// HR person who raised it, which is also whose workflow group routes it.
/// </para>
/// </summary>
public class EmployeePaymentBatch : FullAuditedEntity, IMustHaveTenant, IWorkflowEnabledEntity
{
    public int TenantId { get; set; }

    /// <summary>
    /// The wage type every line in the batch is paid under. One per batch, exactly as in legacy -- the
    /// selectable set is narrowed by <c>EmployeePaymentBatchesAppService.GetSelectableWageTypes</c> rather
    /// than by a property on this entity, so the rule can be queried by the UI before anything is saved.
    /// </summary>
    public virtual int WageTypeId { get; set; }

    public virtual WageType WageType { get; set; }

    public virtual DateTime PaymentDate { get; set; }

    /// <summary>Whether the line amounts are money, days or hours. Copied onto each payment on approval.</summary>
    public virtual UnitType UnitType { get; set; }

    /// <summary>
    /// Nullable to match <see cref="EmployeePayment.CurrencyId"/>. Legacy defaulted it to the tenant's
    /// default currency at construction; here the app service does that, because a default chosen in a
    /// constructor cannot be overridden by an import or a test without looking like a bug.
    /// </summary>
    public virtual int? CurrencyId { get; set; }

    public virtual Currency Currency { get; set; }

    /// <summary>
    /// Legacy <c>RequestComments</c>. Legacy declared it <c>[NonPersistent]</c>, so whatever the requester
    /// typed to justify the payment was thrown away the moment the form closed; it is a real column here.
    /// </summary>
    [StringLength(4000)]
    public virtual string Comments { get; set; }

    /// <inheritdoc cref="IWorkflowEnabledEntity.WorkflowRequestId"/>
    public virtual int? WorkflowRequestId { get; set; }

    /// <inheritdoc cref="IWorkflowEnabledEntity.WorkflowStatus"/>
    public virtual RequestStatus? WorkflowStatus { get; set; }

    /// <summary>
    /// The employees being paid and how much each gets. Never empty and never naming the same employee twice
    /// -- both refused for real, in <see cref="Services.IEmployeePaymentBatchService"/>. Legacy defined those
    /// two rules in <c>PaymentRequestDetail.PerformValidation</c> and then never called the method from
    /// anywhere, so neither has ever run in production.
    /// </summary>
    public virtual ICollection<EmployeePaymentBatchDetail> Details { get; set; }
}
