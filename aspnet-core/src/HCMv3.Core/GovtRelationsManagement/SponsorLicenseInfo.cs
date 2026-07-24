using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.GovtRelationsManagement;

/// <summary>
/// Trade license info for a <see cref="Sponsor"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.GovtRelationsManagement.SponsorLicenseInfo</c>, one of
/// <see cref="Sponsor"/>'s aggregated child collections -- see <see cref="Sponsor"/> remarks). Legacy
/// <c>PerformValidation</c> parity (unique <see cref="TradeLicenseNum"/> per Sponsor) is ported in
/// <c>SponsorLicenseInfosAppService</c>.
/// </summary>
public class SponsorLicenseInfo : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int SponsorId { get; set; }

    public virtual Sponsor Sponsor { get; set; }

    /// <summary>"Trade License Number" in the legacy UI.</summary>
    [StringLength(256)]
    public virtual string TradeLicenseNum { get; set; }

    public virtual DateTime LicenseIssueDate { get; set; }

    public virtual DateTime LicenseExpiryDate { get; set; }

    public virtual long CivilId { get; set; }

    [StringLength(256)]
    public virtual string ActivityType { get; set; }
}
