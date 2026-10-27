using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.Workflow;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Movement (withdraw/return) log for an <see cref="EmployeeDocument"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeDocumentMvmt</c>). Legacy has an
/// overlap-validation block but it is commented out (dead code) -- only blank-field validation is
/// ported.
///
/// <para>
/// This is also the passport-withdrawal transaction. Legacy had a separate <c>PassportWithdrawalRequest</c>
/// table that, on final approval, wrote a row into this one -- request and transaction kept apart, and kept
/// in step by hand. Here there is one row: the employee's request IS the movement log entry, hidden by the
/// <c>ApprovedOnly</c> filter until it is approved. That is why <see cref="IWorkflowEnabledEntity"/> and the
/// three legacy request-only fields (<see cref="EmployeeId"/>, <see cref="GuarantorId"/>,
/// <see cref="WithdrawalReason"/>) live here rather than on a shadow table.
/// </para>
/// </summary>
public class EmployeeDocumentMvmt : FullAuditedEntity, IMustHaveTenant, IHasEmployeeId, IWorkflowEnabledEntity
{
    public int TenantId { get; set; }

    /// <summary>
    /// The employee the document belongs to. Denormalised from <see cref="EmployeeDocument"/> rather than
    /// reached through it, because self-service scoping (<c>IHasEmployeeId</c>) has to be a column on the
    /// row being scoped: a query that had to join out to the document to find out whose row it is could not
    /// be filtered generically, and every ESS screen would hand-write the join instead.
    /// </summary>
    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int EmployeeDocumentId { get; set; }

    public virtual EmployeeDocument EmployeeDocument { get; set; }

    /// <summary>
    /// Who vouches for the document coming back, when the tenant asks for one. Optional: legacy only
    /// required a guarantor for some populations, and a hard requirement here would block every withdrawal
    /// for tenants that never used the field.
    /// </summary>
    public virtual int? GuarantorId { get; set; }

    public virtual Employee Guarantor { get; set; }

    public virtual DateTime WithdrawDate { get; set; }

    public virtual DateTime ReturnDate { get; set; }

    /// <summary>
    /// Default(DateTime) means the document hasn't actually been returned yet -- see
    /// <see cref="HCMv3.PersonnelAdministration.DocumentMvmtStatus"/> (computed on the parent document).
    /// </summary>
    public virtual DateTime ActualReturnDate { get; set; }

    /// <summary>
    /// Why the employee needs the document out of the safe. Whether it is mandatory is a tenant setting
    /// (<c>ManageSelfServiceSettings.IsWithdrawalReasonMandatory</c>), not a property of the column.
    /// </summary>
    [StringLength(500)]
    public virtual string WithdrawalReason { get; set; }

    [StringLength(500)]
    public virtual string Comments { get; set; }

    // ------------------------------------------------------------------ the custody trail
    //
    // Who physically held the document and when. All nullable: a movement recorded before these existed, or
    // one whose hand-over nobody has recorded yet, simply has no trail rather than an invented one.

    /// <summary>
    /// The day the document was physically handed to the employee. May differ from the requested
    /// <see cref="WithdrawDate"/>: the request says when the employee asked for it, this says when it left
    /// the safe.
    /// </summary>
    public virtual DateTime? ActualWithdrawDate { get; set; }

    /// <summary>The user who recorded the hand-over.</summary>
    public virtual long? HandedOverByUserId { get; set; }

    /// <summary>
    /// The user who took the document back: HR recording the return, or the approver who approved the
    /// employee's own return request.
    /// </summary>
    public virtual long? ReceivedByUserId { get; set; }

    /// <summary>Remarks or condition noted when the document came back.</summary>
    [StringLength(MaxReturnRemarksLength)]
    public virtual string ReturnRemarks { get; set; }

    /// <summary>
    /// The last day an overdue reminder went out for this movement, so the reminder run sends at most one a
    /// day however often it polls.
    /// </summary>
    public virtual DateTime? OverdueReminderSentOn { get; set; }

    /// <summary>
    /// The movement this one extended: the document was already out on that movement when this one was
    /// approved, so this row replaced it. The replaced row is soft-deleted and stays reachable through this
    /// column, which is how the earlier due dates remain readable. Null for an ordinary withdrawal.
    /// </summary>
    public virtual int? ExtendsMovementId { get; set; }

    /// <summary>
    /// The leave (<see cref="EmployeeAbsence"/>) this withdrawal was raised from automatically, when the
    /// tenant has passport withdrawal switched on for leave (<c>ManageSelfServiceSettings.IsPassportWithdrawalRequired</c>).
    /// Null for a withdrawal somebody asked for themselves.
    ///
    /// <para>
    /// A plain id with no navigation and no foreign key, deliberately. It is what makes the automatic raise
    /// happen at most once per leave -- including after a send-back and a second approval at the same step,
    /// and after the employee has withdrawn the request, whose row is then soft-deleted but still counted --
    /// and a cascade from the leave must never be able to remove a record of who holds whose passport.
    /// </para>
    /// </summary>
    public virtual int? SourceEmployeeAbsenceId { get; set; }

    public const int MaxReturnRemarksLength = 500;

    public const int MaxWithdrawalReasonLength = 500;

    public const int MaxCommentsLength = 500;

    /// <summary>
    /// Whether the movement counts: recorded by HR (no workflow status), approved, or closed. The same three
    /// states the <c>ApprovedOnly</c> filter lets through, spelled out for code that has to switch that
    /// filter off to see pending rows as well.
    /// </summary>
    public virtual bool IsInEffect()
    {
        return WorkflowStatus is null or RequestStatus.Approved or RequestStatus.Closed;
    }

    /// <summary>Still out: <see cref="ActualReturnDate"/> has not been stamped.</summary>
    public virtual bool IsReturned()
    {
        return ActualReturnDate != default;
    }

    /// <summary>In effect, not returned, and past the date it was due back.</summary>
    public virtual bool IsOverdueOn(DateTime today)
    {
        return IsInEffect() && !IsReturned() && ReturnDate.Date < today.Date;
    }

    /// <inheritdoc cref="IWorkflowEnabledEntity.WorkflowRequestId"/>
    public virtual int? WorkflowRequestId { get; set; }

    /// <inheritdoc cref="IWorkflowEnabledEntity.WorkflowStatus"/>
    public virtual RequestStatus? WorkflowStatus { get; set; }
}
