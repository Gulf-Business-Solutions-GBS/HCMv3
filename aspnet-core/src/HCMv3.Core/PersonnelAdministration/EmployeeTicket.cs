using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PayrollManagement;

using HCMv3.Workflow;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee air-ticket transaction record (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeTicket</c>, Wave 3 deferred bucket).
///
/// DELIBERATE SIMPLIFICATIONS / DROPPED FIELDS:
/// <list type="bullet">
/// <item><description>The legacy <c>DocAttachment</c> FK is dropped per instruction (document-attachment
/// infrastructure is not ported).</description></item>
/// <item><description>The legacy <c>prevXxx</c> change-tracking fields and the entire quota-balance
/// cascade (<c>UpdateQuotaBalance</c> against <see cref="EmployeeTicketQuota"/>, age-limit/dependant/benefit
/// exception checks, auto-computed <c>Amount</c> via <c>TicketFare.GetAirTicketRate</c>) are NOT ported
/// on the entity (same cross-entity-cascade omission approach as Wave 1's <c>EmployeeAbsAdjustment</c>);
/// <c>Amount</c> here is a plain persisted field, accepted as client input rather than
/// server-recomputed.</description></item>
/// </list>
/// </summary>
public class EmployeeTicket : FullAuditedEntity, IMustHaveTenant, IHasEmployeeId, IWorkflowEnabledEntity
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int TicketTypeId { get; set; }

    public virtual TicketType TicketType { get; set; }

    public virtual DateTime TicketDate { get; set; }

    public virtual DateTime PaymentDate { get; set; }

    public virtual double Compensate { get; set; }

    public virtual TicketPaymentType TicketPaymentType { get; set; }

    public virtual double Amount { get; set; }

    public virtual int AdvancedTicketPaid { get; set; }

    [StringLength(4000)]
    public virtual string Comments { get; set; }

    public virtual bool OverrideAmount { get; set; }

    /// <inheritdoc cref="IWorkflowEnabledEntity.WorkflowRequestId"/>
    public virtual int? WorkflowRequestId { get; set; }

    /// <inheritdoc cref="IWorkflowEnabledEntity.WorkflowStatus"/>
    public virtual RequestStatus? WorkflowStatus { get; set; }
}
