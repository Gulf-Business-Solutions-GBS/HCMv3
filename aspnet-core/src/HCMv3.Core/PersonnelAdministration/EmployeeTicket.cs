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
/// <item><description>The legacy <c>OnSaving</c> logic (auto-computed <c>Amount</c> via
/// <c>TicketFare.GetAirTicketRate</c>, age-limit/dependant/benefit-exception checks, quota balance) lives in
/// <c>Services.EmployeeTicketCalculationService</c>, called by the admin Ticket screen on every save; the
/// <c>prevXxx</c> fields become that service's previous-value parameters.</description></item>
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
