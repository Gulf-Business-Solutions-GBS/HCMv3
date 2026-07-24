using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee attendance quota ledger record (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeAttendanceQuota</c>, Wave 1 deferred
/// bucket).
///
/// NOTE (legacy quirk, intentionally NOT "fixed"): unlike <see cref="EmployeeAbsenceQuota"/>, this quota is
/// scoped to the Employee only -- there is no AttendanceType FK in the legacy source, so a given employee has
/// at most one attendance quota ledger regardless of attendance type.
///
/// The legacy <c>EntitlmentFrom</c>/<c>EntitlmentTo</c> spelling is corrected to
/// <see cref="EntitlementFrom"/>/<see cref="EntitlementTo"/>, consistent with <see cref="EmployeeAbsenceQuota"/>.
/// </summary>
public class EmployeeAttendanceQuota : FullAuditedEntity, IMustHaveTenant
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
}
