using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PayrollManagement;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee air-ticket quota ledger record per TicketType per entitlement period (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeTicketQuota</c>, Wave 3 deferred bucket).
/// Same shape as Wave 1's <see cref="EmployeeAbsenceQuota"/>.
///
/// The legacy <c>EntitlmentFrom</c>/<c>EntitlmentTo</c> spelling is corrected to
/// <see cref="EntitlementFrom"/>/<see cref="EntitlementTo"/> for consistency with the rest of this port.
/// <c>QuotaBalance</c> (legacy: <c>QuotaAccrued - (QuotaUsed + QuotaAdjusted)</c>) is a computed/
/// <c>[NonPersistent]</c> value in the legacy source and is not a persisted column here; it is exposed as a
/// computed field on the DTO instead.
/// </summary>
public class EmployeeTicketQuota : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int TicketTypeId { get; set; }

    public virtual TicketType TicketType { get; set; }

    public virtual DateTime EntitlementFrom { get; set; }

    public virtual DateTime EntitlementTo { get; set; }

    public virtual DateTime DeductionFrom { get; set; }

    public virtual DateTime DeductionTo { get; set; }

    public virtual double QuotaAccrued { get; set; }

    public virtual double QuotaUsed { get; set; }

    public virtual double QuotaAdjusted { get; set; }
}
