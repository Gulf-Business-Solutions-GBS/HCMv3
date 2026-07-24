using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PayrollManagement;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Manual air-ticket quota adjustment record (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeTktAdjustment</c>, Wave 3 deferred
/// bucket). Reuses <see cref="AdjustmentType"/> (already ported in Wave 1) and
/// <see cref="TicketPaymentType"/> (ported this wave alongside <see cref="EmployeeTicket"/>).
///
/// The legacy cascade side effects (updating <see cref="EmployeeTicketQuota.QuotaAdjusted"/> on
/// save/delete, negative-balance validation against the linked quota record) are ported at the app-service
/// level in <c>EmployeeTktAdjustmentsAppService</c>, not on the entity (same approach as Wave 1's
/// <c>EmployeeAbsAdjustment</c>).
/// </summary>
public class EmployeeTktAdjustment : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int TicketTypeId { get; set; }

    public virtual TicketType TicketType { get; set; }

    public virtual AdjustmentType AdjustmentType { get; set; }

    public virtual DateTime EffectiveDate { get; set; }

    public virtual double QuotaAccrued { get; set; }

    public virtual double QuotaAdjusted { get; set; }

    public virtual TicketPaymentType TicketPaymentType { get; set; }

    [StringLength(500)]
    public virtual string Comments { get; set; }
}
