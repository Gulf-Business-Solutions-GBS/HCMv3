using System.Threading.Tasks;
using Abp;
using Abp.Configuration;
using HCMv3.Configuration;
using HCMv3.Configuration.Dto;
using HCMv3.UiCustomization;
using HCMv3.UiCustomization.Dto;

namespace HCMv3.Web.UiCustomization.Metronic;

public class Theme6UiCustomizer : UiThemeCustomizerBase, IUiCustomizer
{
    public Theme6UiCustomizer(ISettingManager settingManager)
        : base(settingManager, AppConsts.Theme6)
    {
    }

    public async Task<UiCustomizationSettingsDto> GetUiSettings()
    {
        var settings = new UiCustomizationSettingsDto
        {
            BaseSettings = new ThemeSettingsDto
            {
                Layout = new ThemeLayoutSettingsDto()
                {
                    LayoutType = await GetSettingValueAsync(AppSettings.UiManagement.LayoutType),
                    DarkMode = await GetSettingValueAsync<bool>(AppSettings.UiManagement.DarkMode),
                },
                Footer = new ThemeFooterSettingsDto
                {
                    FooterWidthType = await GetSettingValueAsync(AppSettings.UiManagement.Footer.FooterWidthType)
                },
                Menu = new ThemeMenuSettingsDto()
                {
                    SearchActive = await GetSettingValueAsync<bool>(AppSettings.UiManagement.SearchActive)
                }
            }
        };

        settings.BaseSettings.Theme = ThemeName;
        settings.BaseSettings.Menu.Position = "left";
        settings.BaseSettings.Menu.AsideSkin = "light";
        settings.BaseSettings.Menu.FixedAside = true;
        settings.BaseSettings.Menu.SubmenuToggle = "false";
        settings.BaseSettings.Menu.AllowAsideMinimizing = true;
        settings.BaseSettings.Menu.DefaultMinimizedAside = true;

        settings.BaseSettings.SubHeader.SubheaderSize = 5;
        settings.BaseSettings.SubHeader.TitleStyle = "text-gray-900 fw-bold my-2 me-5";
        settings.BaseSettings.SubHeader.ContainerStyle = "toolbar py-2";

        settings.IsLeftMenuUsed = true;
        settings.IsTopMenuUsed = false;
        settings.IsTabMenuUsed = false;
        settings.AllowMenuScroll = true;

        return settings;
    }

    public async Task UpdateUserUiManagementSettingsAsync(UserIdentifier user, ThemeSettingsDto settings)
    {
        await SettingManager.ChangeSettingForUserAsync(user, AppSettings.UiManagement.Theme, ThemeName);

        await ChangeSettingForUserAsync(user, AppSettings.UiManagement.LayoutType, settings.Layout.LayoutType);
        await ChangeSettingForUserAsync(user, AppSettings.UiManagement.DarkMode, settings.Layout.DarkMode.ToString());
        await ChangeSettingForUserAsync(user, AppSettings.UiManagement.Header.DesktopFixedHeader, settings.Header.DesktopFixedHeader.ToString());
        await ChangeSettingForUserAsync(user, AppSettings.UiManagement.Header.MobileFixedHeader, settings.Header.MobileFixedHeader.ToString());
        await ChangeSettingForUserAsync(user, AppSettings.UiManagement.SubHeader.Fixed, settings.SubHeader.FixedSubHeader.ToString());
        await ChangeSettingForUserAsync(user, AppSettings.UiManagement.SubHeader.Style, settings.SubHeader.SubheaderStyle);
        await ChangeSettingForUserAsync(user, AppSettings.UiManagement.Footer.FooterWidthType, settings.Footer.FooterWidthType);
        await ChangeSettingForUserAsync(user, AppSettings.UiManagement.SearchActive, settings.Menu.SearchActive.ToString());
    }

    public async Task UpdateTenantUiManagementSettingsAsync(int tenantId, ThemeSettingsDto settings, UserIdentifier changerUser)
    {
        await SettingManager.ChangeSettingForTenantAsync(tenantId, AppSettings.UiManagement.Theme, ThemeName);

        await ChangeSettingForTenantAsync(tenantId, AppSettings.UiManagement.LayoutType, settings.Layout.LayoutType);
        await ChangeSettingForTenantAsync(tenantId, AppSettings.UiManagement.DarkMode, settings.Layout.DarkMode.ToString());
        await ChangeSettingForTenantAsync(tenantId, AppSettings.UiManagement.Header.DesktopFixedHeader, settings.Header.DesktopFixedHeader.ToString());
        await ChangeSettingForTenantAsync(tenantId, AppSettings.UiManagement.Header.MobileFixedHeader, settings.Header.MobileFixedHeader.ToString());
        await ChangeSettingForTenantAsync(tenantId, AppSettings.UiManagement.SubHeader.Fixed, settings.SubHeader.FixedSubHeader.ToString());
        await ChangeSettingForTenantAsync(tenantId, AppSettings.UiManagement.SubHeader.Style, settings.SubHeader.SubheaderStyle);
        await ChangeSettingForTenantAsync(tenantId, AppSettings.UiManagement.Footer.FooterWidthType, settings.Footer.FooterWidthType);
        await ChangeSettingForTenantAsync(tenantId, AppSettings.UiManagement.SearchActive, settings.Menu.SearchActive.ToString());

        await ResetDarkModeSettingsAsync(changerUser);
    }

    public async Task UpdateApplicationUiManagementSettingsAsync(ThemeSettingsDto settings, UserIdentifier changerUser)
    {
        await SettingManager.ChangeSettingForApplicationAsync(AppSettings.UiManagement.Theme, ThemeName);

        await ChangeSettingForApplicationAsync(AppSettings.UiManagement.LayoutType, settings.Layout.LayoutType);
        await ChangeSettingForApplicationAsync(AppSettings.UiManagement.DarkMode, settings.Layout.DarkMode.ToString());
        await ChangeSettingForApplicationAsync(AppSettings.UiManagement.Header.DesktopFixedHeader, settings.Header.DesktopFixedHeader.ToString());
        await ChangeSettingForApplicationAsync(AppSettings.UiManagement.Header.MobileFixedHeader, settings.Header.MobileFixedHeader.ToString());
        await ChangeSettingForApplicationAsync(AppSettings.UiManagement.SubHeader.Fixed, settings.SubHeader.FixedSubHeader.ToString());
        await ChangeSettingForApplicationAsync(AppSettings.UiManagement.SubHeader.Style, settings.SubHeader.SubheaderStyle);
        await ChangeSettingForApplicationAsync(AppSettings.UiManagement.Footer.FooterWidthType, settings.Footer.FooterWidthType);
        await ChangeSettingForApplicationAsync(AppSettings.UiManagement.SearchActive, settings.Menu.SearchActive.ToString());

        await ResetDarkModeSettingsAsync(changerUser);
    }

    public async Task<ThemeSettingsDto> GetHostUiManagementSettings()
    {
        var theme = await SettingManager.GetSettingValueForApplicationAsync(AppSettings.UiManagement.Theme);

        return new ThemeSettingsDto
        {
            Theme = theme,
            Layout = new ThemeLayoutSettingsDto()
            {
                LayoutType = await GetSettingValueForApplicationAsync(AppSettings.UiManagement.LayoutType),
                DarkMode = await GetSettingValueForApplicationAsync<bool>(AppSettings.UiManagement.DarkMode)
            },
            SubHeader = new ThemeSubHeaderSettingsDto
            {
                FixedSubHeader = await GetSettingValueForApplicationAsync<bool>(AppSettings.UiManagement.SubHeader.Fixed),
                SubheaderStyle = await GetSettingValueForApplicationAsync(AppSettings.UiManagement.SubHeader.Style)
            },
            Footer = new ThemeFooterSettingsDto
            {
                FooterWidthType = await GetSettingValueForApplicationAsync(AppSettings.UiManagement.Footer.FooterWidthType)
            },
            Menu = new ThemeMenuSettingsDto()
            {
                SearchActive = await GetSettingValueForApplicationAsync<bool>(AppSettings.UiManagement.SearchActive)
            }
        };
    }

    public async Task<ThemeSettingsDto> GetTenantUiCustomizationSettings(int tenantId)
    {
        var theme = await SettingManager.GetSettingValueForTenantAsync(AppSettings.UiManagement.Theme, tenantId);

        return new ThemeSettingsDto
        {
            Theme = theme,
            Layout = new ThemeLayoutSettingsDto
            {
                LayoutType = await GetSettingValueForTenantAsync(AppSettings.UiManagement.LayoutType, tenantId),
                DarkMode = await GetSettingValueForTenantAsync<bool>(AppSettings.UiManagement.DarkMode, tenantId),
            },
            SubHeader = new ThemeSubHeaderSettingsDto
            {
                FixedSubHeader = await GetSettingValueForTenantAsync<bool>(AppSettings.UiManagement.SubHeader.Fixed, tenantId),
                SubheaderStyle = await GetSettingValueForTenantAsync(AppSettings.UiManagement.SubHeader.Style, tenantId)
            },
            Footer = new ThemeFooterSettingsDto
            {
                FooterWidthType = await GetSettingValueForTenantAsync(AppSettings.UiManagement.Footer.FooterWidthType, tenantId)
            },
            Menu = new ThemeMenuSettingsDto
            {
                SearchActive = await GetSettingValueForTenantAsync<bool>(AppSettings.UiManagement.SearchActive, tenantId)
            }
        };
    }

    public override async Task<string> GetBodyClass()
    {
        return await Task.FromResult(
            "header-fixed header-tablet-and-mobile-fixed toolbar-enabled toolbar-fixed aside-enabled aside-fixed"
        );
    }
}

