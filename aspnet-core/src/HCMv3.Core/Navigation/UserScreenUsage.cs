using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.Navigation;

/// <summary>
/// What one user does with one screen: whether they pinned it, and when they last opened it.
///
/// ONE ROW FOR THREE FEATURES. Favourites, "recently visited" and the dashboard's quick links are three
/// views of the same fact -- this person's relationship with this screen -- so they share a row rather
/// than getting a table each. Three tables would need three writes on every navigation and would drift:
/// a screen renamed in one and not the others, a favourite that no longer appears in recents.
///
/// KEYED BY THE IMMUTABLE SCREEN KEY, NOT THE CODE. Screen Codes are re-mappable by an administrator,
/// so keying favourites by code would mean a rename silently orphaned everyone's pinned screens -- a
/// failure that surfaces weeks later as "my favourites disappeared" with nothing to connect it to the
/// rename. The key never changes. A key with no matching Screen row is still possible -- a screen
/// removed between visits -- so reading code MUST tolerate it rather than assume every row resolves.
/// </summary>
public class UserScreenUsage : CreationAuditedEntity, IMayHaveTenant
{
    /// <summary>
    /// Nullable, NOT IMustHaveTenant. The host administrator has no tenant and still navigates, and ABP
    /// refuses TenantId 0 on IMustHaveTenant -- which made every page change return HTTP 500 for them.
    /// Null here means a host-level row, which is exactly what a host user needs.
    /// </summary>
    public virtual int? TenantId { get; set; }

    /// <summary>Owner. Every query is filtered by this: one person's rail is nobody else's business.</summary>
    public virtual long UserId { get; set; }

    /// <summary>
    /// Points at <c>Screen.ScreenKey</c>. Immutable, unlike the code shown on screen.
    /// </summary>
    [Required]
    [StringLength(ScreenCodeConsts.MaxKeyLength)]
    public virtual string ScreenKey { get; set; }

    /// <summary>Pinned by the user. Favourites and quick links both read this.</summary>
    public virtual bool IsFavorite { get; set; }

    /// <summary>
    /// Last time the user opened it. Null for a screen favourited but never visited since -- which is
    /// why "recent" queries must exclude nulls rather than sorting them to one end.
    /// </summary>
    public virtual DateTime? LastVisitedAt { get; set; }

    /// <summary>
    /// How many times opened. Kept because "recent" alone is a poor ranking for quick links: a screen
    /// opened once yesterday would outrank one opened daily for a year.
    /// </summary>
    public virtual int VisitCount { get; set; }
}
