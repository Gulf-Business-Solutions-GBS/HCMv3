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

    /// <inheritdoc cref="IWorkflowEnabledEntity.WorkflowRequestId"/>
    public virtual int? WorkflowRequestId { get; set; }

    /// <inheritdoc cref="IWorkflowEnabledEntity.WorkflowStatus"/>
    public virtual RequestStatus? WorkflowStatus { get; set; }
}
