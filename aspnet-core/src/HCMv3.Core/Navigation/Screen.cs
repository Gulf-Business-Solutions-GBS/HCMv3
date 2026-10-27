using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.Navigation;

/// <summary>
/// One menu-reachable screen and the Screen Code it answers to.
///
/// WHY A TABLE AND NOT A CONSTANT. Codes have to be re-mappable: a customer who has always called
/// payroll "PR" should be able to make <c>SPY001</c> read their way. That is the whole reason this is
/// data. <see cref="ScreenCatalog"/> only seeds rows that do not exist yet, so an admin's edit is never
/// overwritten by a later deployment.
///
/// KEY VERSUS CODE, AND WHY BOTH.
/// <list type="bullet">
/// <item><description><see cref="ScreenKey"/> is IMMUTABLE. Seeded once from the route, then
/// independent of it. Everything that must survive -- favourites, recents, quick links -- points
/// here.</description></item>
/// <item><description><see cref="Code"/> is MUTABLE and exists to be typed, searched and printed.
/// Nothing references it.</description></item>
/// </list>
/// Keying favourites by the code instead would mean an admin renaming a code silently orphaned every
/// user's pinned screens -- the failure would surface weeks later as "my favourites disappeared", with
/// nothing to connect it to the rename.
/// </summary>
public class Screen : FullAuditedEntity, IMayHaveTenant
{
    /// <summary>
    /// Nullable, NOT IMustHaveTenant. The host administrator has no tenant and still navigates, and ABP
    /// refuses TenantId 0 on IMustHaveTenant -- which made every page change return HTTP 500 for them.
    /// Null here means a host-level row, which is exactly what a host user needs.
    /// </summary>
    public virtual int? TenantId { get; set; }

    /// <summary>
    /// Immutable identity, e.g. <c>app.admin.employees</c>. Seeded from the route because that was the
    /// only unique identifier available -- four menu items share a name, no two share a route -- but it
    /// does NOT track the route afterwards. Move the screen and only <see cref="Route"/> changes.
    /// </summary>
    [Required]
    [StringLength(ScreenCodeConsts.MaxKeyLength)]
    public virtual string ScreenKey { get; set; }

    /// <summary>
    /// The Screen Code, e.g. <c>SPA001</c>. Unique within a tenant, editable by an administrator, and
    /// shown after the menu name when the user has codes switched on.
    /// </summary>
    [Required]
    [StringLength(ScreenCodeConsts.MaxCodeLength)]
    public virtual string Code { get; set; }

    /// <summary>Module the code's prefix came from, e.g. <c>PayrollManagement</c>. Grouping only.</summary>
    [StringLength(ScreenCodeConsts.MaxModuleLength)]
    public virtual string Module { get; set; }

    /// <summary>Localization key for the menu name. Not the display text: that is per language.</summary>
    [Required]
    [StringLength(ScreenCodeConsts.MaxNameKeyLength)]
    public virtual string NameKey { get; set; }

    /// <summary>Where it currently lives. Free to change; the key does not follow it.</summary>
    [Required]
    [StringLength(ScreenCodeConsts.MaxRouteLength)]
    public virtual string Route { get; set; }

    /// <summary>
    /// Permission guarding the screen, or null for one open to any signed-in user. Held here so a quick
    /// link can be filtered without the client having to resolve the route back to a menu entry.
    /// </summary>
    [StringLength(ScreenCodeConsts.MaxPermissionLength)]
    public virtual string PermissionName { get; set; }
}
