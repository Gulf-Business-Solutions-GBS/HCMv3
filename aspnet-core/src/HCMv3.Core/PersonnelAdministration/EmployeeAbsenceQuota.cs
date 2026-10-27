using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PayrollManagement;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee leave quota ledger record per AbsenceType per entitlement period (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeAbsenceQuota</c>, Wave 1 deferred bucket).
///
/// The legacy <c>EntitlmentFrom</c>/<c>EntitlmentTo</c> spelling is corrected to
/// <see cref="EntitlementFrom"/>/<see cref="EntitlementTo"/> to match the already-corrected spelling used
/// elsewhere in this port (e.g. <c>HCMv3.PayrollManagement.LeaveEntitlement</c>,
/// <c>HCMv3.PayrollManagement.QuotaEntitlement</c>).
///
/// <c>QuotaBalance</c> (legacy: <c>QuotaAccrued - (QuotaUsed + QuotaAdjusted + QuotaForfeited)</c>) is a
/// computed/<c>[NonPersistent]</c> value in the legacy source and is not a persisted column here; it is
/// exposed as a computed field on the DTO instead. The legacy <c>OldQuotaAccured</c> (change-tracking helper)
/// and <c>Locked</c>/<c>SysXxx</c> audit fields are covered by <c>FullAuditedEntity</c> and not re-ported.
/// </summary>
public class EmployeeAbsenceQuota : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int AbsenceTypeId { get; set; }

    public virtual AbsenceType AbsenceType { get; set; }

    public virtual DateTime EntitlementFrom { get; set; }

    public virtual DateTime EntitlementTo { get; set; }

    public virtual DateTime DeductionFrom { get; set; }

    public virtual DateTime DeductionTo { get; set; }

    public virtual double QuotaAccrued { get; set; }

    /// <summary>
    /// Tracks the actual cumulative quota accrued over time, separate from <see cref="QuotaAccrued"/> which
    /// can be capped/reduced by max-limit rules.
    /// </summary>
    public virtual double ActualQuotaAccrued { get; set; }

    public virtual double QuotaUsed { get; set; }

    public virtual double QuotaAdjusted { get; set; }

    public virtual double QuotaForfeited { get; set; }

    public virtual bool NotificationSent { get; set; }
}
