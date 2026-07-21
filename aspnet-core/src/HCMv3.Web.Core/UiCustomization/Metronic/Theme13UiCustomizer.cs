using System.Threading.Tasks;
using Abp;
using Abp.Configuration;
using HCMv3.Configuration;
using HCMv3.Configuration.Dto;
using HCMv3.UiCustomization;
using HCMv3.UiCustomization.Dto;

namespace HCMv3.Web.UiCustomization.Metronic;

public class Theme13UiCustomizer : UiThemeCustomizerBase, IUiCustomizer
{
    public Theme13UiCustomizer(ISettingManager settingManager)
        : base(settingManager, AppConsts.Theme13)
    {
    }

    public async Task<UiCustomizationSettingsDto> GetUiSettings()
    {
        var settings = new UiCustomizationSettingsDto
        {
            BaseSettings = new ThemeSettingsDto
            {
                Layout = new ThemeLayoutSettingsDto
                {
                    LayoutType = await GetSettingValueAsync(AppSettings.UiManagement.LayoutType),
                    DarkMode = await GetSettingValueAsync<bool>(AppSettings.UiManagement.DarkMode)
                },
                Menu = new ThemeMenuSettingsDto
                {
                    FixedAside = await GetSettingValueAsync<bool>(AppSettings.UiManagement.LeftAside.FixedAside),
                    SearchActive = await GetSettingValueAsync<bool>(AppSettings.UiManagement.SearchActive)
                },
                Footer = new ThemeFooterSettingsDto()
                {
                    FooterWidthType = await GetSettingValueAsync(AppSettings.UiManagement.Footer.FooterWidthType)
                }
            }
        };

        settings.BaseSettings.Theme = ThemeName;
        settings.BaseSettings.SubHeader.SubheaderStyle = "transparent";
        settings.BaseSettings.Menu.Position = "left";
        settings.BaseSettings.Menu.AsideSkin = "light";
        settings.BaseSettings.Menu.SubmenuToggle = "false";
        settings.BaseSettings.Menu.FixedAside = true;

        settings.BaseSettings.SubHeader.SubheaderSize = 5;
        settings.BaseSettings.SubHeader.TitleStyle = "text-gray-900 fw-bold my-2 me-5";
        settings.BaseSettings.SubHeader.ContainerStyle = "toolbar";
        settings.BaseSettings.SubHeader.SubContainerStyle = "px-10";
        settings.BaseSettings.SubHeader.FixedSubHeader = true;

        settings.IsLeftMenuUsed = true;
        settings.IsTopMenuUsed = false;
        settings.IsTabMenuUsed = false;

        return settings;
    }

    public async Task UpdateUserUiManagementSettingsAsync(UserIdentifier user, ThemeSettingsDto settings)
    {
        await SettingManager.ChangeSettingForUserAsync(user, AppSettings.UiManagement.Theme, ThemeName);

        await ChangeSettingForUserAsync(user, AppSettings.UiManagement.LayoutType, settings.Layout.LayoutType);
        await ChangeSettingForUserAsync(user, AppSettings.UiManagement.DarkMode, settings.Layout.DarkMode.ToString());
        await ChangeSettingForUserAsync(user, AppSettings.UiManagement.Header.MobileFixedHeader, settings.Header.MobileFixedHeader.ToString());
        await ChangeSettingForUserAsync(user, AppSettings.UiManagement.LeftAside.FixedAside, settings.Menu.FixedAside.ToString());
        await ChangeSettingForUserAsync(user, AppSettings.UiManagement.SearchActive, settings.Menu.SearchActive.ToString());
        await ChangeSettingForUserAsync(user, AppSettings.UiManagement.Footer.FooterWidthType, settings.Footer.FooterWidthType);
    }

    public async Task UpdateTenantUiManagementSettingsAsync(int tenantId, ThemeSettingsDto settings, UserIdentifier changerUser)
    {
        await SettingManager.ChangeSettingForTenantAsync(tenantId, AppSettings.UiManagement.Theme, ThemeName);

        await ChangeSettingForTenantAsync(tenantId, AppSettings.UiManagement.LayoutType, settings.Layout.LayoutType);
        await ChangeSettingForTenantAsync(tenantId, AppSettings.UiManagement.DarkMode, settings.Layout.DarkMode.ToString());
        await ChangeSettingForTenantAsync(tenantId, AppSettings.UiManagement.Header.MobileFixedHeader, settings.Header.MobileFixedHeader.ToString());
        await ChangeSettingForTenantAsync(tenantId, AppSettings.UiManagement.LeftAside.FixedAside, settings.Menu.FixedAside.ToString());
        await ChangeSettingForTenantAsync(tenantId, AppSettings.UiManagement.SearchActive, settings.Menu.SearchActive.ToString());
        await ChangeSettingForTenantAsync(tenantId, AppSettings.UiManagement.Footer.FooterWidthType, settings.Footer.FooterWidthType);

        await ResetDarkModeSettingsAsync(changerUser);
    }

    public async Task UpdateApplicationUiManagementSettingsAsync(ThemeSettingsDto settings, UserIdentifier changerUser)
    {
        await SettingManager.ChangeSettingForApplicationAsync(AppSettings.UiManagement.Theme, ThemeName);

        await ChangeSettingForApplicationAsync(AppSettings.UiManagement.LayoutType, settings.Layout.LayoutType);
        await ChangeSettingForApplicationAsync(AppSettings.UiManagement.DarkMode, settings.Layout.DarkMode.ToString());
        await ChangeSettingForApplicationAsync(AppSettings.UiManagement.Header.MobileFixedHeader, settings.Header.MobileFixedHeader.ToString());
        await ChangeSettingForApplicationAsync(AppSettings.UiManagement.LeftAside.FixedAside, settings.Menu.FixedAside.ToString());
        await ChangeSettingForApplicationAsync(AppSettings.UiManagement.SearchActive, settings.Menu.SearchActive.ToString());
        await ChangeSettingForApplicationAsync(AppSettings.UiManagement.Footer.FooterWidthType, settings.Footer.FooterWidthType);

        await ResetDarkModeSettingsAsync(changerUser);
    }

    public async Task<ThemeSettingsDto> GetHostUiManagementSettings()
    {
        var theme = await SettingManager.GetSettingValueForApplicationAsync(AppSettings.UiManagement.Theme);

        return new ThemeSettingsDto
        {
            Theme = theme,
            Layout = new ThemeLayoutSettingsDto
            {
                LayoutType = await GetSettingValueForApplicationAsync(AppSettings.UiManagement.LayoutType),
                DarkMode = await GetSettingValueForApplicationAsync<bool>(AppSettings.UiManagement.DarkMode)
            },
            Menu = new ThemeMenuSettingsDto
            {
                FixedAside = await GetSettingValueForApplicationAsync<bool>(AppSettings.UiManagement.LeftAside.FixedAside),
                SearchActive = await GetSettingValueForApplicationAsync<bool>(AppSettings.UiManagement.SearchActive)
            },
            SubHeader = new ThemeSubHeaderSettingsDto
            {
                FixedSubHeader = true
            },
            Footer = new ThemeFooterSettingsDto()
            {
                FooterWidthType = await GetSettingValueForApplicationAsync(AppSettings.UiManagement.Footer.FooterWidthType)
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
            Menu = new ThemeMenuSettingsDto
            {
                FixedAside = await GetSettingValueForTenantAsync<bool>(AppSettings.UiManagement.LeftAside.FixedAside, tenantId),
                SearchActive = await GetSettingValueForTenantAsync<bool>(AppSettings.UiManagement.SearchActive, tenantId)
            },
            SubHeader = new ThemeSubHeaderSettingsDto
            {
                FixedSubHeader = true
            },
            Footer = new ThemeFooterSettingsDto()
            {
                FooterWidthType = await GetSettingValueForTenantAsync(AppSettings.UiManagement.Footer.FooterWidthType, tenantId)
            }
        };
    }

    public override Task<string> GetBodyClass()
    {
        return Task.FromResult(
            "header-fixed header-tablet-and-mobile-fixed toolbar-enabled toolbar-fixed toolbar-tablet-and-mobile-fixed aside-enabled aside-fixed"
        );
    }

    public override Task<string> GetBodyStyle()
    {
        return Task.FromResult("--kt-toolbar-height:55px;--kt-toolbar-height-tablet-and-mobile:55px");
    }
}

