using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee gratuity/indemnity quota ledger record per entitlement period (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeGratuityQuota</c>, Wave 2 deferred
/// bucket).
///
/// The legacy <c>EntitlmentFrom</c>/<c>EntitlmentTo</c> spelling is corrected to
/// <see cref="EntitlementFrom"/>/<see cref="EntitlementTo"/> to match the already-corrected spelling used
/// elsewhere in this port (e.g. <c>HCMv3.PersonnelAdministration.EmployeeAbsenceQuota</c>).
///
/// <c>QuotaBalance</c> (legacy: <c>Math.Round(QuotaAccrued - (QuotaUsed + QuotaAdjusted), 3)</c>) is a
/// computed/<c>[NonPersistent]</c> value in the legacy source and is not a persisted column here; it is
/// exposed as a computed field on the DTO instead. The legacy <c>Locked</c>/<c>SysXxx</c> audit fields are
/// covered by <c>FullAuditedEntity</c> and not re-ported.
/// </summary>
public class EmployeeGratuityQuota : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual DateTime EntitlementFrom { get; set; }

    public virtual DateTime EntitlementTo { get; set; }

    public virtual DateTime DeductionFrom { get; set; }

    public virtual DateTime DeductionTo { get; set; }

    public virtual double QuotaAccrued { get; set; }

    public virtual double QuotaUsed { get; set; }

    public virtual double QuotaAdjusted { get; set; }
}
