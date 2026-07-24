using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.GovtRelationsManagement;

/// <summary>
/// Authorized-signatory assignment for a <see cref="Sponsor"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.GovtRelationsManagement.SponsorSignatory</c>, one of <see cref="Sponsor"/>'s
/// aggregated child collections -- see <see cref="Sponsor"/> remarks). Legacy <c>PerformValidation</c> parity
/// (no duplicate Signatory per Sponsor) is ported in <c>SponsorSignatoriesAppService</c>.
/// </summary>
public class SponsorSignatory : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int SponsorId { get; set; }

    public virtual Sponsor Sponsor { get; set; }

    public virtual int SignatoryId { get; set; }

    public virtual Signatory Signatory { get; set; }
}
