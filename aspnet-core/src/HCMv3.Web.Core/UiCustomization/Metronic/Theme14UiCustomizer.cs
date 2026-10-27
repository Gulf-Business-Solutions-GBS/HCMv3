using Abp.Configuration;
using Abp.Localization;
using HCMv3.UiCustomization;

namespace HCMv3.Web.UiCustomization.Metronic;

/// <summary>
/// The legacy Pinnacle HRMS self-service skin: navy chrome, its blue accents, and Zain.
///
/// NOTHING BUT COLOUR AND TYPE. It inherits the default theme's behaviour entirely -- same layout, same
/// settings, same Metronic bundle -- and differs only in the stylesheet the client loads for it. That is
/// deliberate: a theme that also moved things would make "switch back" a bigger promise than it sounds,
/// because a user who had adjusted to the new arrangement would lose it.
///
/// Adopting it is a click in Select a Theme, and abandoning it is the same click.
/// </summary>
public class Theme14UiCustomizer : ThemeDefaultUiCustomizer, IUiCustomizer
{
    public Theme14UiCustomizer(
        ISettingManager settingManager,
        ILocalizationManager localizationManager)
        : base(settingManager, localizationManager, AppConsts.Theme14)
    {
    }
}
