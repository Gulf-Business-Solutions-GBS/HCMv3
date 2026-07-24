using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.GovtRelationsManagement;

/// <summary>
/// Effective-dated GovtJobTitle quota assignment for a <see cref="Sponsor"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.GovtRelationsManagement.SponsorJobTitle</c>, one of <see cref="Sponsor"/>'s
/// aggregated child collections -- see <see cref="Sponsor"/> remarks).
///
/// <see cref="CitizenQuotaAccrued"/>/<see cref="NonCitizenQuotaAccrued"/> are persisted but
/// server-computed (floor/ceiling of NoofPosition * Percent / 100) -- recomputed by
/// <c>SponsorJobTitlesAppService</c> on every Create/Update, never accepted as client input (mirrors the
/// legacy's read-only computed getters). <see cref="CitizenQuotaUsed"/>/<see cref="NonCitizenQuotaUsed"/>
/// are plain mutable ledger fields, intended to be incremented/decremented by a not-yet-ported
/// <c>EmployeeSponsor</c> app service later; this port just exposes them as normal fields.
/// <c>CitizenQuotaBalance</c>/<c>NonCitizenQuotaBalance</c> (Accrued - Used) are DTO-only computed
/// properties, not stored columns -- see <c>SponsorJobTitleDto</c>.
/// </summary>
public class SponsorJobTitle : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int SponsorId { get; set; }

    public virtual Sponsor Sponsor { get; set; }

    public virtual int GovtJobTitleId { get; set; }

    public virtual GovtJobTitle GovtJobTitle { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    public virtual int NoofPosition { get; set; }

    public virtual int CitizenPercent { get; set; }

    /// <summary>Server-computed: floor(NoofPosition * CitizenPercent / 100).</summary>
    public virtual int CitizenQuotaAccrued { get; set; }

    public virtual int CitizenQuotaUsed { get; set; }

    public virtual int NonCitizenPercent { get; set; }

    /// <summary>Server-computed: ceiling(NoofPosition * NonCitizenPercent / 100).</summary>
    public virtual int NonCitizenQuotaAccrued { get; set; }

    public virtual int NonCitizenQuotaUsed { get; set; }
}
