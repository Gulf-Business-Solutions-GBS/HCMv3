using System.ComponentModel.DataAnnotations;
using Abp.Runtime.Validation;

namespace HCMv3.Navigation.Dto;

/// <summary>One screen as the client needs it: identity, code, and where it goes.</summary>
public class ScreenDto
{
    public int Id { get; set; }

    public string ScreenKey { get; set; }

    public string Code { get; set; }

    public string Module { get; set; }

    public string NameKey { get; set; }

    public string Route { get; set; }

    public string PermissionName { get; set; }
}

/// <summary>
/// Everything the shell needs to draw codes, shortcuts and the dashboard tile, in one call.
///
/// One call rather than four because the shell needs all of it before it can render the menu, and four
/// round trips on the hottest path in the application to draw one sidebar is not a trade worth making.
/// </summary>
public class NavigationBootstrapDto
{
    /// <summary>Every screen the CALLER may open. Screens they lack permission for are absent.</summary>
    public ScreenDto[] Screens { get; set; }

    /// <summary>The caller's own pinned screens, capped.</summary>
    public ScreenUsageDto[] Favorites { get; set; }

    /// <summary>The caller's last few screens, most recent first.</summary>
    public ScreenUsageDto[] Recent { get; set; }

    /// <summary>Tenant-wide links published by an administrator, in the order they set.</summary>
    public ScreenDto[] SharedQuickLinks { get; set; }

    /// <summary>
    /// Whether this user wants codes shown after menu names. A setting, not a preference we infer: it
    /// defaults on, and the user turns it off in their profile.
    /// </summary>
    public bool ShowScreenCodes { get; set; }
}

public class CreateOrEditSharedQuickLinkInput
{
    public int? Id { get; set; }

    [Required]
    [StringLength(ScreenCodeConsts.MaxKeyLength)]
    public string ScreenKey { get; set; }

    public int DisplayOrder { get; set; }
}

public class UpdateScreenCodeInput
{
    [Required]
    public int Id { get; set; }

    [Required]
    [StringLength(ScreenCodeConsts.MaxCodeLength)]
    public string Code { get; set; }
}

/// <summary>
/// Admin listing filter. Deliberately searches code, name key and module together: an administrator
/// looking for a screen may remember any one of the three, and making them choose which field they are
/// searching is a question the screen should answer for them.
/// </summary>
public class GetAllScreensInput : Abp.Application.Services.Dto.PagedAndSortedResultRequestDto, IShouldNormalize
{
    public string Filter { get; set; }

    public string Module { get; set; }

    public void Normalize()
    {
        if (string.IsNullOrWhiteSpace(Sorting))
        {
            Sorting = "Code";
        }

        if (MaxResultCount == 0)
        {
            MaxResultCount = 20;
        }
    }
}

/// <summary>A published quick link with the screen it points at, resolved for display.</summary>
public class SharedQuickLinkDto
{
    public int Id { get; set; }

    public string ScreenKey { get; set; }

    public int DisplayOrder { get; set; }

    /// <summary>Null when the link points at a screen no longer in the catalogue.</summary>
    public ScreenDto Screen { get; set; }
}
