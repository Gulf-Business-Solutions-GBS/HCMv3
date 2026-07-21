using HCMv3.Configuration.Dto;

namespace HCMv3.UiCustomization.Dto;

public class UiCustomizationSettingsDto
{
    public ThemeSettingsDto BaseSettings { get; set; }

    public bool IsLeftMenuUsed { get; set; }

    public bool IsTopMenuUsed { get; set; }

    public bool IsTabMenuUsed { get; set; }

    public bool AllowMenuScroll { get; set; } = true;
}

