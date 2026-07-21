using System.Threading.Tasks;
using Abp;
using Abp.Configuration;
using HCMv3.Configuration;
using HCMv3.Configuration.Dto;
using HCMv3.UiCustomization;
using HCMv3.UiCustomization.Dto;

namespace HCMv3.Web.UiCustomization.Metronic;

public class Theme7UiCustomizer : UiThemeCustomizerBase, IUiCustomizer
{
    public Theme7UiCustomizer(ISettingManager settingManager)
        : base(settingManager, AppConsts.Theme7)
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
                    DarkMode = await GetSettingValueAsync<bool>(AppSettings.UiManagement.DarkMode)
                },
                Footer = new ThemeFooterSettingsDto
                {
                    DesktopFixedFooter = await GetSettingValueAsync<bool>(AppSettings.UiManagement.Footer.DesktopFixedFooter),
                    MobileFixedFooter = await GetSettingValueAsync<bool>(AppSettings.UiManagement.Footer.MobileFixedFooter),

                },
                Menu = new ThemeMenuSettingsDto()
                {
                    SearchActive = await GetSettingValueAsync<bool>(AppSettings.UiManagement.SearchActive),
                    EnableSecondary = true,
                    DefaultMinimizedAside = false,
                    FixedAside = true
                }
            }
        };

        settings.BaseSettings.Theme = ThemeName;
        settings.BaseSettings.Menu.Position = "left";
        settings.BaseSettings.Menu.AsideSkin = "light";
        settings.BaseSettings.Menu.SubmenuToggle = "false";

        settings.BaseSettings.SubHeader.SubheaderSize = 1;
        settings.BaseSettings.SubHeader.TitleStyle = "text-gray-900 fw-bold my-0 fs-2";
        settings.BaseSettings.SubHeader.ContainerStyle = "subheader py-2 py-lg-4  subheader-transparent";

        settings.IsLeftMenuUsed = true;
        settings.IsTopMenuUsed = false;
        settings.IsTabMenuUsed = false;
        settings.AllowMenuScroll = false;

        return settings;
    }

    public async Task UpdateUserUiManagementSettingsAsync(UserIdentifier user, ThemeSettingsDto settings)
    {
        await SettingManager.ChangeSettingForUserAsync(user, AppSettings.UiManagement.Theme, ThemeName);

        await ChangeSettingForUserAsync(user, AppSettings.UiManagement.LayoutType,
            settings.Layout.LayoutType);
        await ChangeSettingForUserAsync(user, AppSettings.UiManagement.DarkMode,
            settings.Layout.DarkMode.ToString());
        await ChangeSettingForUserAsync(user, AppSettings.UiManagement.Header.DesktopFixedHeader,
            settings.Header.DesktopFixedHeader.ToString());
        await ChangeSettingForUserAsync(user, AppSettings.UiManagement.Header.MobileFixedHeader,
            settings.Header.MobileFixedHeader.ToString());
        await ChangeSettingForUserAsync(user, AppSettings.UiManagement.SubHeader.Fixed,
            settings.SubHeader.FixedSubHeader.ToString());
        await ChangeSettingForUserAsync(user, AppSettings.UiManagement.SubHeader.Style,
            settings.SubHeader.SubheaderStyle);
        await ChangeSettingForUserAsync(user, AppSettings.UiManagement.Footer.DesktopFixedFooter,
            settings.Footer.DesktopFixedFooter.ToString());
        await ChangeSettingForUserAsync(user, AppSettings.UiManagement.Footer.MobileFixedFooter,
            settings.Footer.MobileFixedFooter.ToString());
        await ChangeSettingForUserAsync(user, AppSettings.UiManagement.SearchActive,
            settings.Menu.SearchActive.ToString());
    }

    public async Task UpdateTenantUiManagementSettingsAsync(int tenantId, ThemeSettingsDto settings, UserIdentifier changerUser)
    {
        await SettingManager.ChangeSettingForTenantAsync(tenantId, AppSettings.UiManagement.Theme, ThemeName);

        await ChangeSettingForTenantAsync(tenantId, AppSettings.UiManagement.LayoutType,
            settings.Layout.LayoutType);
        await ChangeSettingForTenantAsync(tenantId, AppSettings.UiManagement.DarkMode,
            settings.Layout.DarkMode.ToString());
        await ChangeSettingForTenantAsync(tenantId, AppSettings.UiManagement.Header.DesktopFixedHeader,
            settings.Header.DesktopFixedHeader.ToString());
        await ChangeSettingForTenantAsync(tenantId, AppSettings.UiManagement.Header.MobileFixedHeader,
            settings.Header.MobileFixedHeader.ToString());
        await ChangeSettingForTenantAsync(tenantId, AppSettings.UiManagement.SubHeader.Fixed,
            settings.SubHeader.FixedSubHeader.ToString());
        await ChangeSettingForTenantAsync(tenantId, AppSettings.UiManagement.SubHeader.Style,
            settings.SubHeader.SubheaderStyle);
        await ChangeSettingForTenantAsync(tenantId, AppSettings.UiManagement.Footer.DesktopFixedFooter,
            settings.Footer.DesktopFixedFooter.ToString());
        await ChangeSettingForTenantAsync(tenantId, AppSettings.UiManagement.Footer.MobileFixedFooter,
            settings.Footer.MobileFixedFooter.ToString());
        await ChangeSettingForTenantAsync(tenantId, AppSettings.UiManagement.SearchActive,
            settings.Menu.SearchActive.ToString());

        await ResetDarkModeSettingsAsync(changerUser);
    }

    public async Task UpdateApplicationUiManagementSettingsAsync(ThemeSettingsDto settings, UserIdentifier changerUser)
    {
        await SettingManager.ChangeSettingForApplicationAsync(AppSettings.UiManagement.Theme, ThemeName);

        await ChangeSettingForApplicationAsync(AppSettings.UiManagement.LayoutType,
            settings.Layout.LayoutType);
        await ChangeSettingForApplicationAsync(AppSettings.UiManagement.DarkMode,
            settings.Layout.DarkMode.ToString());
        await ChangeSettingForApplicationAsync(AppSettings.UiManagement.Header.DesktopFixedHeader,
            settings.Header.DesktopFixedHeader.ToString());
        await ChangeSettingForApplicationAsync(AppSettings.UiManagement.Header.MobileFixedHeader,
            settings.Header.MobileFixedHeader.ToString());
        await ChangeSettingForApplicationAsync(AppSettings.UiManagement.SubHeader.Fixed,
            settings.SubHeader.FixedSubHeader.ToString());
        await ChangeSettingForApplicationAsync(AppSettings.UiManagement.SubHeader.Style,
            settings.SubHeader.SubheaderStyle);
        await ChangeSettingForApplicationAsync(AppSettings.UiManagement.Footer.DesktopFixedFooter,
            settings.Footer.DesktopFixedFooter.ToString());
        await ChangeSettingForApplicationAsync(AppSettings.UiManagement.Footer.MobileFixedFooter,
            settings.Footer.MobileFixedFooter.ToString());
        await ChangeSettingForApplicationAsync(AppSettings.UiManagement.SearchActive,
            settings.Menu.SearchActive.ToString());

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
                DarkMode = await GetSettingValueForApplicationAsync<bool>(AppSettings.UiManagement.DarkMode),
            },
            Header = new ThemeHeaderSettingsDto(),
            SubHeader = new ThemeSubHeaderSettingsDto
            {
                FixedSubHeader =
                    await GetSettingValueForApplicationAsync<bool>(AppSettings.UiManagement.SubHeader.Fixed),
                SubheaderStyle = await GetSettingValueForApplicationAsync(AppSettings.UiManagement.SubHeader.Style)
            },
            Footer = new ThemeFooterSettingsDto
            {
                DesktopFixedFooter =
                    await GetSettingValueForApplicationAsync<bool>(AppSettings.UiManagement.Footer.DesktopFixedFooter),
                MobileFixedFooter =
                    await GetSettingValueForApplicationAsync<bool>(AppSettings.UiManagement.Footer.MobileFixedFooter)
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
            Layout = new ThemeLayoutSettingsDto()
            {
                LayoutType = await GetSettingValueForTenantAsync(AppSettings.UiManagement.LayoutType, tenantId),
                DarkMode = await GetSettingValueForTenantAsync<bool>(AppSettings.UiManagement.DarkMode, tenantId)
            },
            SubHeader = new ThemeSubHeaderSettingsDto
            {
                FixedSubHeader = await GetSettingValueForTenantAsync<bool>(
                    AppSettings.UiManagement.SubHeader.Fixed,
                    tenantId
                ),
                SubheaderStyle = await GetSettingValueForTenantAsync(
                    AppSettings.UiManagement.SubHeader.Style,
                    tenantId
                )
            },
            Footer = new ThemeFooterSettingsDto
            {
                DesktopFixedFooter = await GetSettingValueForTenantAsync<bool>(
                    AppSettings.UiManagement.Footer.DesktopFixedFooter,
                    tenantId
                ),
                MobileFixedFooter = await GetSettingValueForTenantAsync<bool>(
                    AppSettings.UiManagement.Footer.MobileFixedFooter,
                    tenantId
                )
            },
            Menu = new ThemeMenuSettingsDto()
            {
                SearchActive =
                    await GetSettingValueForTenantAsync<bool>(
                        AppSettings.UiManagement.SearchActive,
                        tenantId
                    )
            }
        };
    }

    public override async Task<string> GetBodyClass()
    {
        var defaultBodyClass = "header-fixed header-tablet-and-mobile-fixed aside-fixed aside-secondary-enabled";


        if (await GetSettingValueAsync<bool>(AppSettings.UiManagement.Footer.DesktopFixedFooter))
        {
            defaultBodyClass += " " + "footer-fixed";
        }
        if (await GetSettingValueAsync<bool>(AppSettings.UiManagement.Footer.MobileFixedFooter))
        {
            defaultBodyClass += " " + "footer-tablet-and-mobile-fixed";
        }

        return await Task.FromResult(defaultBodyClass);
    }
}

