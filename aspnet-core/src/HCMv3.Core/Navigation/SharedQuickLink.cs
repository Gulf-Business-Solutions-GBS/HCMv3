using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.Navigation;

/// <summary>
/// A quick link an administrator publishes to everyone in the tenant -- the "Shared" band of the
/// dashboard tile.
///
/// SEPARATE FROM <see cref="UserScreenUsage"/> ON PURPOSE. It was tempting to reuse that table with a
/// null UserId meaning "everyone", but the two are different things with different lifecycles: a shared
/// link is curated, ordered and outlives any individual, while a usage row is a by-product of one
/// person browsing. Folding them together would mean every query for "my favourites" had to remember to
/// exclude the tenant-wide rows, and the day someone forgot, an admin's link would appear inside a
/// user's personal list where they could not remove it.
///
/// SHARED LINKS ONLY, NOT SHARED FAVOURITES. An administrator may offer a link; they may not reach into
/// someone's personal favourites and pin something there.
/// </summary>
public class SharedQuickLink : FullAuditedEntity, IMayHaveTenant
{
    /// <summary>
    /// Nullable, NOT IMustHaveTenant. The host administrator has no tenant and still navigates, and ABP
    /// refuses TenantId 0 on IMustHaveTenant -- which made every page change return HTTP 500 for them.
    /// Null here means a host-level row, which is exactly what a host user needs.
    /// </summary>
    public virtual int? TenantId { get; set; }

    /// <summary>
    /// Points at <see cref="Screen.ScreenKey"/>, never at the code, so re-mapping a code leaves the
    /// tenant's published links intact.
    /// </summary>
    [Required]
    [StringLength(ScreenCodeConsts.MaxKeyLength)]
    public virtual string ScreenKey { get; set; }

    /// <summary>
    /// Position in the tile. Explicit because the order is an editorial decision -- the admin is saying
    /// "start here" -- and any implicit ordering would rearrange it on them.
    /// </summary>
    public virtual int DisplayOrder { get; set; }
}
