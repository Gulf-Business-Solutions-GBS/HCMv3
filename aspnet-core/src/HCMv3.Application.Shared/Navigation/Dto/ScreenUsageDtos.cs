using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.Navigation.Dto;

/// <summary>One screen in the user's rail, favourites list or quick links.</summary>
public class ScreenUsageDto
{
    public string ScreenKey { get; set; }

    public bool IsFavorite { get; set; }

    public DateTime? LastVisitedAt { get; set; }

    public int VisitCount { get; set; }
}

/// <summary>
/// Everything the shell needs to draw the rail, in one call.
///
/// Favourites and recents are returned TOGETHER rather than as two endpoints because the shell draws
/// them together on every page load; two calls would double the requests on the hottest path in the app
/// to render one small list.
/// </summary>
public class MyScreenShortcutsDto
{
    public ScreenUsageDto[] Favorites { get; set; }

    public ScreenUsageDto[] Recent { get; set; }
}

public class SetScreenFavoriteInput
{
    [Required]
    [StringLength(ScreenCodeConsts.MaxKeyLength)]
    public string ScreenKey { get; set; }

    public bool IsFavorite { get; set; }
}

public class RecordScreenVisitInput
{
    [Required]
    [StringLength(ScreenCodeConsts.MaxKeyLength)]
    public string ScreenKey { get; set; }
}
