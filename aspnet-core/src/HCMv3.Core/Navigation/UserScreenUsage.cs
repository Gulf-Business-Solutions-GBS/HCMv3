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
/// KEYED BY SCREEN CODE, NOT ROUTE. Routes change when a feature moves; a code is a promise not to. The
/// code is also what the S-code lookup resolves, so a favourite and a typed code name the same thing.
/// An unknown code is therefore possible -- a screen removed between visits -- and reading code MUST
/// tolerate it rather than assume every stored row still resolves.
/// </summary>
public class UserScreenUsage : CreationAuditedEntity, IMustHaveTenant
{
    public virtual int TenantId { get; set; }

    /// <summary>Owner. Every query is filtered by this: one person's rail is nobody else's business.</summary>
    public virtual long UserId { get; set; }

    /// <summary>
    /// Stable screen code (the S-code), e.g. <c>S-PA-EMP</c>. Not a route: routes move.
    /// </summary>
    [Required]
    [StringLength(ScreenCodeConsts.MaxCodeLength)]
    public virtual string ScreenCode { get; set; }

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
