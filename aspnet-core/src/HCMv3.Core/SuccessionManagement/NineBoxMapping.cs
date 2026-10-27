using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.SuccessionManagement;

/// <summary>
/// Maps a <see cref="SuccessionRating"/> to its corresponding <see cref="NineBoxRating"/> quadrant (ported
/// from legacy XAF <c>HCM10.Module.BusinessObjects.SuccessionManagement.NineBoxMapping</c>).
/// </summary>
public class NineBoxMapping : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int RatingId { get; set; }

    public virtual SuccessionRating Rating { get; set; }

    public virtual int NineBoxRatingId { get; set; }

    public virtual NineBoxRating NineBoxRating { get; set; }
}
