using System.Collections.Generic;
using System.Linq;
using Abp.Configuration;
using Abp.Extensions;
using Abp.Json;
using Abp.Localization;
using Abp.Net.Mail;
using Abp.Zero.Configuration;
using Abp.Zero.Ldap.Configuration;
using Microsoft.Extensions.Configuration;
using HCMv3.Authentication;
using HCMv3.Authorization.Session;
using HCMv3.DashboardCustomization;

namespace HCMv3.Configuration;

/// <summary>
/// Defines settings for the application.
/// See <see cref="AppSettings"/> for setting names.
/// </summary>
public class AppSettingProvider : SettingProvider
{
    private readonly IConfigurationRoot _appConfiguration;
    VisibleSettingClientVisibilityProvider _visibleSettingClientVisibilityProvider;

    public AppSettingProvider(IAppConfigurationAccessor configurationAccessor)
    {
        _appConfiguration = configurationAccessor.Configuration;
        _visibleSettingClientVisibilityProvider = new VisibleSettingClientVisibilityProvider();
    }

    public override IEnumerable<SettingDefinition> GetSettingDefinitions(SettingDefinitionProviderContext context)
    {
        // Disable TwoFactorLogin by default (can be enabled by UI)
        context.Manager.GetSettingDefinition(AbpZeroSettingNames.UserManagement.TwoFactorLogin.IsEnabled)
            .DefaultValue = false.ToString().ToLowerInvariant();

        // Change scope of Email settings
        ChangeEmailSettingScopes(context);

        return GetHostSettings().Union(GetTenantSettings()).Union(GetSharedSettings())
            // theme settings
            .Union(GetDefaultThemeSettings())
            .Union(GetTheme2Settings())
            .Union(GetTheme3Settings())
            .Union(GetTheme4Settings())
            .Union(GetTheme5Settings())
            .Union(GetTheme6Settings())
            .Union(GetTheme7Settings())
            .Union(GetTheme8Settings())
            .Union(GetTheme9Settings())
            .Union(GetTheme10Settings())
            .Union(GetTheme11Settings())
            .Union(GetTheme12Settings())
            .Union(GetTheme13Settings())
            .Union(GetTheme14Settings())
            .Union(GetDashboardSettings())
            .Union(GetExternalLoginProviderSettings())
            .Union(GetAdditionalLdapSettings());
    }

    private IEnumerable<SettingDefinition> GetAdditionalLdapSettings()
    {
        return new[]
        {
                new SettingDefinition(LdapSettingNames.UseSsl, "false", L("UseSsl"), scopes: SettingScopes.Application | SettingScopes.Tenant, isInherited: false, isEncrypted:false)
            };
    }

    private void ChangeEmailSettingScopes(SettingDefinitionProviderContext context)
    {
        if (!HCMv3Consts.AllowTenantsToChangeEmailSettings)
        {
            context.Manager.GetSettingDefinition(EmailSettingNames.Smtp.Host).Scopes = SettingScopes.Application;
            context.Manager.GetSettingDefinition(EmailSettingNames.Smtp.Port).Scopes = SettingScopes.Application;
            context.Manager.GetSettingDefinition(EmailSettingNames.Smtp.UserName).Scopes =
                SettingScopes.Application;
            context.Manager.GetSettingDefinition(EmailSettingNames.Smtp.Password).Scopes =
                SettingScopes.Application;
            context.Manager.GetSettingDefinition(EmailSettingNames.Smtp.Domain).Scopes = SettingScopes.Application;
            context.Manager.GetSettingDefinition(EmailSettingNames.Smtp.EnableSsl).Scopes =
                SettingScopes.Application;
            context.Manager.GetSettingDefinition(EmailSettingNames.Smtp.UseDefaultCredentials).Scopes =
                SettingScopes.Application;
            context.Manager.GetSettingDefinition(EmailSettingNames.DefaultFromAddress).Scopes =
                SettingScopes.Application;
            context.Manager.GetSettingDefinition(EmailSettingNames.DefaultFromDisplayName).Scopes =
                SettingScopes.Application;
        }
    }

    private IEnumerable<SettingDefinition> GetHostSettings()
    {
        return new[]
        {
                new SettingDefinition(AppSettings.TenantManagement.AllowSelfRegistration,
                    GetFromAppSettings(AppSettings.TenantManagement.AllowSelfRegistration, "true"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider),
                new SettingDefinition(AppSettings.TenantManagement.IsNewRegisteredTenantActiveByDefault,
                    GetFromAppSettings(AppSettings.TenantManagement.IsNewRegisteredTenantActiveByDefault, "false")),
                new SettingDefinition(AppSettings.TenantManagement.UseCaptchaOnRegistration,
                    GetFromAppSettings(AppSettings.TenantManagement.UseCaptchaOnRegistration, "true"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider),
                new SettingDefinition(AppSettings.TenantManagement.UseCaptchaOnEmailActivation,
                    GetFromAppSettings(AppSettings.TenantManagement.UseCaptchaOnEmailActivation, "true"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider),
                new SettingDefinition(AppSettings.TenantManagement.UseCaptchaOnResetPassword,
                    GetFromAppSettings(AppSettings.TenantManagement.UseCaptchaOnResetPassword, "true"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider),
                new SettingDefinition(AppSettings.TenantManagement.DefaultEdition,
                    GetFromAppSettings(AppSettings.TenantManagement.DefaultEdition, "")),
                new SettingDefinition(AppSettings.UserManagement.SmsVerificationEnabled,
                    GetFromAppSettings(AppSettings.UserManagement.SmsVerificationEnabled, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider),
                new SettingDefinition(AppSettings.TenantManagement.SubscriptionExpireNotifyDayCount,
                    GetFromAppSettings(AppSettings.TenantManagement.SubscriptionExpireNotifyDayCount, "7"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider),
                new SettingDefinition(AppSettings.HostManagement.BillingLegalName,
                    GetFromAppSettings(AppSettings.HostManagement.BillingLegalName, "")),
                new SettingDefinition(AppSettings.HostManagement.BillingAddress,
                    GetFromAppSettings(AppSettings.HostManagement.BillingAddress, "")),
                new SettingDefinition(AppSettings.Recaptcha.SiteKey, GetFromSettings("Recaptcha:SiteKey"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider),
                new SettingDefinition(AppSettings.UiManagement.Theme,
                    GetFromAppSettings(AppSettings.UiManagement.Theme, "default"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider,
                    scopes: SettingScopes.All),

                new SettingDefinition(AppSettings.UserManagement.Password.EnableCheckingLastXPasswordWhenPasswordChange,
                    GetFromAppSettings(AppSettings.UserManagement.Password.EnableCheckingLastXPasswordWhenPasswordChange, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider),

                new SettingDefinition(AppSettings.UserManagement.Password.CheckingLastXPasswordCount,
                    GetFromAppSettings(AppSettings.UserManagement.Password.CheckingLastXPasswordCount, "3"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider),

                new SettingDefinition(AppSettings.UserManagement.Password.EnablePasswordExpiration,
                    GetFromAppSettings(AppSettings.UserManagement.Password.EnablePasswordExpiration, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider),

                new SettingDefinition(AppSettings.UserManagement.Password.PasswordExpirationDayCount,
                    GetFromAppSettings(AppSettings.UserManagement.Password.PasswordExpirationDayCount, "30"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider),

                new SettingDefinition(AppSettings.UserManagement.Password.PasswordResetCodeExpirationHours,
                    GetFromAppSettings(AppSettings.UserManagement.Password.PasswordResetCodeExpirationHours, "24"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider),

                new SettingDefinition(AppSettings.TenantManagement.IsRestrictedEmailDomainEnabled,
                    GetFromAppSettings(AppSettings.TenantManagement.IsRestrictedEmailDomainEnabled, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider),
            };
    }

    private IEnumerable<SettingDefinition> GetTenantSettings()
    {
        return new[]
        {
                // Go-Live Setup's background payroll run. Not client-visible: it is read through its endpoint.
                new SettingDefinition(AppSettings.GoLive.PayrollRun, string.Empty, scopes: SettingScopes.Tenant),
                // Screen Codes after menu names. Per USER and default ON: codes only become the shared
                // vocabulary they are meant to be if people see them, but anyone who never uses them can
                // quieten their own menu without affecting colleagues.
                new SettingDefinition(AppSettings.Navigation.ShowScreenCodes, "true",
                    scopes: SettingScopes.Application | SettingScopes.Tenant | SettingScopes.User,
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider),
                new SettingDefinition(AppSettings.UserManagement.AllowSelfRegistration,
                    GetFromAppSettings(AppSettings.UserManagement.AllowSelfRegistration, "true"),
                    scopes: SettingScopes.Tenant, clientVisibilityProvider: _visibleSettingClientVisibilityProvider),
                // Careers-portal candidate self-registration. Default "false" -- opt-in, not opt-out.
                new SettingDefinition(AppSettings.Careers.AllowCandidateSelfRegistration,
                    GetFromAppSettings(AppSettings.Careers.AllowCandidateSelfRegistration, "false"),
                    scopes: SettingScopes.Tenant, clientVisibilityProvider: _visibleSettingClientVisibilityProvider),
                new SettingDefinition(AppSettings.UserManagement.IsNewRegisteredUserActiveByDefault,
                    GetFromAppSettings(AppSettings.UserManagement.IsNewRegisteredUserActiveByDefault, "false"),
                    scopes: SettingScopes.Tenant),
                new SettingDefinition(AppSettings.UserManagement.UseCaptchaOnRegistration,
                    GetFromAppSettings(AppSettings.UserManagement.UseCaptchaOnRegistration, "true"),
                    scopes: SettingScopes.Tenant, clientVisibilityProvider: _visibleSettingClientVisibilityProvider),
                new SettingDefinition(AppSettings.UserManagement.UseCaptchaOnEmailActivation,
                    GetFromAppSettings(AppSettings.UserManagement.UseCaptchaOnEmailActivation, "true"),
                    scopes: SettingScopes.Tenant, clientVisibilityProvider: _visibleSettingClientVisibilityProvider),
                new SettingDefinition(AppSettings.UserManagement.UseCaptchaOnResetPassword,
                    GetFromAppSettings(AppSettings.UserManagement.UseCaptchaOnResetPassword, "true"),
                    scopes: SettingScopes.Tenant, clientVisibilityProvider: _visibleSettingClientVisibilityProvider),
                new SettingDefinition(AppSettings.TenantManagement.BillingLegalName,
                    GetFromAppSettings(AppSettings.TenantManagement.BillingLegalName, ""),
                    scopes: SettingScopes.Tenant),
                new SettingDefinition(AppSettings.TenantManagement.BillingAddress,
                    GetFromAppSettings(AppSettings.TenantManagement.BillingAddress, ""), scopes: SettingScopes.Tenant),
                new SettingDefinition(AppSettings.TenantManagement.BillingTaxVatNo,
                    GetFromAppSettings(AppSettings.TenantManagement.BillingTaxVatNo, ""), scopes: SettingScopes.Tenant),
                new SettingDefinition(AppSettings.Email.UseHostDefaultEmailSettings,
                    GetFromAppSettings(AppSettings.Email.UseHostDefaultEmailSettings,
                        HCMv3Consts.MultiTenancyEnabled ? "true" : "false"), scopes: SettingScopes.Tenant),

                // Defaults to "true" so that every existing tenant keeps sending mail: this switch is new,
                // and an upgrade must not silence anyone's password-reset messages. Scoped to both host and
                // tenant so the sender can resolve it wherever it runs, including host-level mail and
                // background jobs outside any tenant.
                new SettingDefinition(AppSettings.Email.IsOutboundEmailEnabled,
                    GetFromAppSettings(AppSettings.Email.IsOutboundEmailEnabled, "true"),
                    scopes: SettingScopes.Application | SettingScopes.Tenant),
                new SettingDefinition(AppSettings.UserManagement.RestrictedEmailDomain,
                    GetFromAppSettings(AppSettings.UserManagement.RestrictedEmailDomain, ""),
                    scopes: SettingScopes.Tenant, clientVisibilityProvider: _visibleSettingClientVisibilityProvider),
                new SettingDefinition(AppSettings.UserManagement.IsRestrictedEmailDomainEnabled,
                    GetFromAppSettings(AppSettings.UserManagement.IsRestrictedEmailDomainEnabled, "false"),
                    scopes: SettingScopes.Tenant, clientVisibilityProvider: _visibleSettingClientVisibilityProvider),
            };
    }

    private IEnumerable<SettingDefinition> GetSharedSettings()
    {
        return new[]
        {
                new SettingDefinition(AppSettings.UserManagement.TwoFactorLogin.IsGoogleAuthenticatorEnabled,
                    GetFromAppSettings(AppSettings.UserManagement.TwoFactorLogin.IsGoogleAuthenticatorEnabled, "false"),
                    scopes: SettingScopes.Application | SettingScopes.Tenant, clientVisibilityProvider: _visibleSettingClientVisibilityProvider),
                new SettingDefinition(AppSettings.UserManagement.IsCookieConsentEnabled,
                    GetFromAppSettings(AppSettings.UserManagement.IsCookieConsentEnabled, "false"),
                    scopes: SettingScopes.Application | SettingScopes.Tenant, clientVisibilityProvider: _visibleSettingClientVisibilityProvider),
                new SettingDefinition(AppSettings.UserManagement.PasswordlessLogin.IsEmailPasswordlessLoginEnabled,
                    GetFromAppSettings(AppSettings.UserManagement.PasswordlessLogin.IsEmailPasswordlessLoginEnabled, "false"),
                    scopes: SettingScopes.Application | SettingScopes.Tenant, clientVisibilityProvider: _visibleSettingClientVisibilityProvider),
                new SettingDefinition(AppSettings.UserManagement.PasswordlessLogin.IsSmsPasswordlessLoginEnabled,
                    GetFromAppSettings(AppSettings.UserManagement.PasswordlessLogin.IsSmsPasswordlessLoginEnabled, "false"),
                    scopes: SettingScopes.Application | SettingScopes.Tenant, clientVisibilityProvider: _visibleSettingClientVisibilityProvider),
                new SettingDefinition(AppSettings.UserManagement.IsQrLoginEnabled,
                    GetFromAppSettings(AppSettings.UserManagement.IsQrLoginEnabled, "false"),
                    scopes: SettingScopes.Application | SettingScopes.Tenant, clientVisibilityProvider: _visibleSettingClientVisibilityProvider),
                new SettingDefinition(AppSettings.UserManagement.IsQuickThemeSelectEnabled,
                    GetFromAppSettings(AppSettings.UserManagement.IsQuickThemeSelectEnabled, "false"),
                    scopes: SettingScopes.Application | SettingScopes.Tenant, clientVisibilityProvider: _visibleSettingClientVisibilityProvider),
                new SettingDefinition(AppSettings.UserManagement.UseCaptchaOnLogin,
                    GetFromAppSettings(AppSettings.UserManagement.UseCaptchaOnLogin, "false"),
                    scopes: SettingScopes.Application | SettingScopes.Tenant, clientVisibilityProvider: _visibleSettingClientVisibilityProvider),
                new SettingDefinition(AppSettings.UserManagement.SessionTimeOut.IsEnabled,
                    GetFromAppSettings(AppSettings.UserManagement.SessionTimeOut.IsEnabled, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.Application | SettingScopes.Tenant),
                new SettingDefinition(AppSettings.UserManagement.SessionTimeOut.TimeOutSecond,
                    GetFromAppSettings(AppSettings.UserManagement.SessionTimeOut.TimeOutSecond, "30"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.Application | SettingScopes.Tenant),
                new SettingDefinition(AppSettings.UserManagement.SessionTimeOut.ShowTimeOutNotificationSecond,
                    GetFromAppSettings(AppSettings.UserManagement.SessionTimeOut.ShowTimeOutNotificationSecond, "30"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.Application | SettingScopes.Tenant),
                new SettingDefinition(AppSettings.UserManagement.SessionTimeOut.ShowLockScreenWhenTimedOut,
                    GetFromAppSettings(AppSettings.UserManagement.SessionTimeOut.ShowLockScreenWhenTimedOut, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.Application | SettingScopes.Tenant),
                new SettingDefinition(AppSettings.UserManagement.AllowOneConcurrentLoginPerUser,
                    GetFromAppSettings(AppSettings.UserManagement.AllowOneConcurrentLoginPerUser, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.Application | SettingScopes.Tenant),
                //Session Management
                new SettingDefinition(AppSettings.UserManagement.SessionManagement.IsEnabled,
                    GetFromAppSettings(AppSettings.UserManagement.SessionManagement.IsEnabled, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.Application | SettingScopes.Tenant),
                new SettingDefinition(AppSettings.UserManagement.SessionManagement.IsSessionFingerprintValidationEnabled,
                    GetFromAppSettings(AppSettings.UserManagement.SessionManagement.IsSessionFingerprintValidationEnabled, "true"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.Application | SettingScopes.Tenant),
                new SettingDefinition(AppSettings.UserManagement.SessionManagement.SessionFingerprintValidationPolicy,
                    GetFromAppSettings(AppSettings.UserManagement.SessionManagement.SessionFingerprintValidationPolicy, nameof(SessionFingerprintValidationPolicy.IpAndUserAgent)),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.Application | SettingScopes.Tenant),
                new SettingDefinition(AppSettings.UserManagement.SessionManagement.SessionAbsoluteTimeoutMinutes,
                    GetFromAppSettings(AppSettings.UserManagement.SessionManagement.SessionAbsoluteTimeoutMinutes, "120"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.Application | SettingScopes.Tenant),
                new SettingDefinition(AppSettings.UserManagement.SessionManagement.IsSessionRevocationEnabled,
                    GetFromAppSettings(AppSettings.UserManagement.SessionManagement.IsSessionRevocationEnabled, "true"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.Application | SettingScopes.Tenant),
                new SettingDefinition(AppSettings.UserManagement.AllowUsingGravatarProfilePicture,
                    GetFromAppSettings(AppSettings.UserManagement.AllowUsingGravatarProfilePicture, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.Application | SettingScopes.Tenant),
                // Not visible to clients: the search bar reads it through GlobalSearchAppService, which leaves
                // out people who no longer exist before anything reaches the browser.
                new SettingDefinition(AppSettings.UserManagement.RecentGlobalSearches, "[]",
                    scopes: SettingScopes.User),
                new SettingDefinition(AppSettings.UserManagement.UseGravatarProfilePicture,
                    GetFromAppSettings(AppSettings.UserManagement.UseGravatarProfilePicture, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.User),
                new SettingDefinition(AppSettings.UserManagement.MaxProfilePictureSizeInMB,
                    GetFromAppSettings(AppSettings.UserManagement.MaxProfilePictureSizeInMB, "5.0"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.Application | SettingScopes.Tenant),
                 new SettingDefinition(AppSettings.UserManagement.MaxProfilePictureWidth,
                    GetFromAppSettings(AppSettings.UserManagement.MaxProfilePictureWidth, "250"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.Application | SettingScopes.Tenant),
                  new SettingDefinition(AppSettings.UserManagement.MaxProfilePictureHeight,
                    GetFromAppSettings(AppSettings.UserManagement.MaxProfilePictureHeight, "250"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.Application | SettingScopes.Tenant),
            };
    }

    private string GetFromAppSettings(string name, string defaultValue = null)
    {
        return GetFromSettings("App:" + name, defaultValue);
    }

    private string GetFromSettings(string name, string defaultValue = null)
    {
        return _appConfiguration[name] ?? defaultValue;
    }

    private IEnumerable<SettingDefinition> GetDefaultThemeSettings()
    {
        return GetLayoutSettingsFor("default");
    }

    /// <summary>
    /// Theme14 -- the legacy self-service skin -- is the default layout wearing different colours, so it
    /// takes the same settings rather than a set of its own. Sharing the definitions is what makes
    /// switching between the two change nothing but the palette.
    /// </summary>
    private IEnumerable<SettingDefinition> GetTheme14Settings()
    {
        // Literal, like every other theme here: Core does not reference Application.Shared, so AppConsts
        // is out of reach.
        return GetLayoutSettingsFor("theme14");
    }

    private IEnumerable<SettingDefinition> GetLayoutSettingsFor(string themeName)
    {
        return new[]
        {
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.DarkMode,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.DarkMode, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.LayoutType,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.LayoutType, "fixed"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Header.DesktopFixedHeader,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Header.DesktopFixedHeader, "true"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Header.MobileFixedHeader,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Header.MobileFixedHeader, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Header.Skin,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Header.Skin, "light"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),

                new SettingDefinition(themeName + "." + AppSettings.UiManagement.SubHeader.Fixed,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.SubHeader.Fixed, "true"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.SubHeader.Style,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.SubHeader.Style, "solid"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),

                new SettingDefinition(themeName + "." + AppSettings.UiManagement.LeftAside.AsideSkin,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.LeftAside.AsideSkin, "light"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.LeftAside.FixedAside,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.LeftAside.FixedAside, "true"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.LeftAside.AllowAsideMinimizing,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.LeftAside.AllowAsideMinimizing,
                        "true"), clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.LeftAside.DefaultMinimizedAside,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.LeftAside.DefaultMinimizedAside,
                        "false"), clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.LeftAside.SubmenuToggle,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.LeftAside.SubmenuToggle, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.LeftAside.HoverableAside,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.LeftAside.HoverableAside,
                        "true"), clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),


                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Footer.DesktopFixedFooter,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Footer.DesktopFixedFooter, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Footer.MobileFixedFooter,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Footer.MobileFixedFooter, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.SearchActive,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.SearchActive, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),

                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Toolbar.DesktopFixedToolbar,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Toolbar.DesktopFixedToolbar, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Toolbar.MobileFixedToolbar,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Toolbar.MobileFixedToolbar, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All)
            };
    }

    private IEnumerable<SettingDefinition> GetTheme2Settings()
    {
        var themeName = "theme2";

        return new[]
        {
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.DarkMode,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.DarkMode, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.LayoutType,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.LayoutType, "fixed"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Header.DesktopFixedHeader,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Header.DesktopFixedHeader, "true"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Header.MobileFixedHeader,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Header.MobileFixedHeader, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Header.MinimizeType,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Header.MinimizeType, "topbar"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.SearchActive,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.SearchActive, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Footer.FooterWidthType,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Footer.FooterWidthType, "fixed"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
            };
    }

    private IEnumerable<SettingDefinition> GetTheme3Settings()
    {
        var themeName = "theme3";

        return new[]
        {
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.DarkMode,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.DarkMode, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.LayoutType,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.LayoutType, "fixed"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Header.DesktopFixedHeader,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Header.DesktopFixedHeader, "true"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Header.MobileFixedHeader,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Header.MobileFixedHeader, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),

                new SettingDefinition(themeName + "." + AppSettings.UiManagement.SubHeader.Fixed,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.SubHeader.Fixed, "true"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.SubHeader.Style,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.SubHeader.Style, "solid"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),

                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Footer.FooterWidthType,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Footer.FooterWidthType, "fixed"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.SearchActive,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.SearchActive, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All)
            };
    }

    private IEnumerable<SettingDefinition> GetTheme4Settings()
    {
        var themeName = "theme4";

        return new[]
        {
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Footer.FooterWidthType,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Footer.FooterWidthType, "fluid"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),

                new SettingDefinition(themeName + "." + AppSettings.UiManagement.DarkMode,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.DarkMode, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.LayoutType,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.LayoutType, "fixed"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Header.DesktopFixedHeader,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Header.DesktopFixedHeader, "true"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Header.MobileFixedHeader,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Header.MobileFixedHeader, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Header.MinimizeType,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Header.MinimizeType, "menu"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),


                new SettingDefinition(themeName + "." + AppSettings.UiManagement.SearchActive,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.SearchActive, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All)
            };
    }

    private IEnumerable<SettingDefinition> GetTheme5Settings()
    {
        var themeName = "theme5";

        return new[]
        {
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.DarkMode,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.DarkMode, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.LayoutType,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.LayoutType, "fixed"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Header.DesktopFixedHeader,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Header.DesktopFixedHeader, "true"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Header.MobileFixedHeader,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Header.MobileFixedHeader, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Header.MinimizeType,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Header.MinimizeType, "menu"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Footer.DesktopFixedFooter,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Footer.DesktopFixedFooter, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.SearchActive,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.SearchActive, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All)
            };
    }

    private IEnumerable<SettingDefinition> GetTheme6Settings()
    {
        var themeName = "theme6";

        return new[]
        {
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.DarkMode,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.DarkMode, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.LayoutType,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.LayoutType, "fixed"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Header.DesktopFixedHeader,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Header.DesktopFixedHeader, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Header.MobileFixedHeader,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Header.MobileFixedHeader, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),

                new SettingDefinition(themeName + "." + AppSettings.UiManagement.SubHeader.Fixed,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.SubHeader.Fixed, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.SubHeader.Style,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.SubHeader.Style, "solid"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),

                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Footer.FooterWidthType,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Footer.FooterWidthType, "fixed"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.SearchActive,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.SearchActive, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All)
            };
    }

    private IEnumerable<SettingDefinition> GetTheme7Settings()
    {
        var themeName = "theme7";

        return new[]
        {
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.DarkMode,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.DarkMode, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.LayoutType,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.LayoutType, "fixed"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Header.DesktopFixedHeader,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Header.DesktopFixedHeader, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Header.MobileFixedHeader,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Header.MobileFixedHeader, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),

                new SettingDefinition(themeName + "." + AppSettings.UiManagement.SubHeader.Fixed,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.SubHeader.Fixed, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.SubHeader.Style,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.SubHeader.Style, "solid"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),

                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Footer.DesktopFixedFooter,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Footer.DesktopFixedFooter, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Footer.MobileFixedFooter,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Footer.MobileFixedFooter, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.SearchActive,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.SearchActive, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All)
            };
    }

    private IEnumerable<SettingDefinition> GetTheme8Settings()
    {
        var themeName = "theme8";

        return new[]
        {
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.DarkMode,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.DarkMode, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.LayoutType,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.LayoutType, "fluid"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Header.DesktopFixedHeader,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Header.DesktopFixedHeader, "true"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Header.MobileFixedHeader,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Header.MobileFixedHeader, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),

                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Footer.FooterWidthType,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Footer.FooterWidthType, "fixed"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.SearchActive,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.SearchActive, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All)
            };
    }

    private IEnumerable<SettingDefinition> GetTheme9Settings()
    {
        var themeName = "theme9";

        return new[]
        {
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.DarkMode,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.DarkMode, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.LayoutType,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.LayoutType, "fixed"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Header.DesktopFixedHeader,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Header.DesktopFixedHeader, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Header.MobileFixedHeader,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Header.MobileFixedHeader, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),

                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Footer.FooterWidthType,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Footer.FooterWidthType, "fixed"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.SearchActive,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.SearchActive, "true"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All)
            };
    }

    private IEnumerable<SettingDefinition> GetTheme10Settings()
    {
        var themeName = "theme10";

        return new[]
        {
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.DarkMode,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.DarkMode, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.LayoutType,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.LayoutType, "fixed"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.LayoutType,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.LayoutType, "fluid"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Header.DesktopFixedHeader,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Header.DesktopFixedHeader, "true"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Header.MobileFixedHeader,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Header.MobileFixedHeader, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),

                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Footer.FooterWidthType,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Footer.FooterWidthType, "fixed"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.SearchActive,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.SearchActive, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All)
            };
    }

    private IEnumerable<SettingDefinition> GetTheme11Settings()
    {
        var themeName = "theme11";

        return new[]
        {
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.DarkMode,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.DarkMode, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.LayoutType,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.LayoutType, "fixed"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.LayoutType,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.LayoutType, "fluid-xxl"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Header.MobileFixedHeader,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Header.MobileFixedHeader, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.LeftAside.FixedAside,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.LeftAside.FixedAside, "true"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),

                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Footer.FooterWidthType,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Footer.FooterWidthType, "fixed"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.SearchActive,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.SearchActive, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All)
            };
    }

    private IEnumerable<SettingDefinition> GetTheme12Settings()
    {
        var themeName = "theme12";

        return new[]
        {
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.DarkMode,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.DarkMode, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.LayoutType,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.LayoutType, "fixed"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.LayoutType,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.LayoutType, "fluid-xxl"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Header.MobileFixedHeader,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Header.MobileFixedHeader, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.LeftAside.FixedAside,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.LeftAside.FixedAside, "true"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),

                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Footer.FooterWidthType,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Footer.FooterWidthType, "fixed"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.SearchActive,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.SearchActive, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All)
            };
    }

    private IEnumerable<SettingDefinition> GetTheme13Settings()
    {
        var themeName = "theme13";

        return new[]
        {
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.DarkMode,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.DarkMode, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.LayoutType,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.LayoutType, "fixed"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.LayoutType,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.LayoutType, "fluid"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Header.MobileFixedHeader,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Header.MobileFixedHeader, "true"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.LeftAside.FixedAside,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.LeftAside.FixedAside, "true"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),

                new SettingDefinition(themeName + "." + AppSettings.UiManagement.Footer.FooterWidthType,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.Footer.FooterWidthType, "fixed"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),
                new SettingDefinition(themeName + "." + AppSettings.UiManagement.SearchActive,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.SearchActive, "false"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All),

                new SettingDefinition(themeName + "." + AppSettings.UiManagement.SubHeader.Fixed,
                    GetFromAppSettings(themeName + "." + AppSettings.UiManagement.SubHeader.Fixed, "true"),
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider, scopes: SettingScopes.All)

            };
    }

    private IEnumerable<SettingDefinition> GetDashboardSettings()
    {
        var mvcDefaultHostView = GetDefaultMvcHostDashboardView();
        var mvcDefaultTenantView = GetDefaultMvcTenantDashboardView();

        var angularDefaultHostView = GetDefaultAngularHostDashboardView();
        var angularDefaultTenantView = GetDefaultAngularTenantDashboardView();


        var reactDefaultHostView = GetDefaultReactHostDashboardView();
        var reactDefaultTenantView = GetDefaultReactTenantDashboardView();

        string GetSettingName(string application, string dashboardName)
        {
            return AppSettings.DashboardCustomization.Configuration + "." + application + "." + dashboardName;
        }

        return new[]
        {
                new SettingDefinition(
                    GetSettingName(
                        HCMv3DashboardCustomizationConsts.Applications.Mvc,
                        mvcDefaultHostView.DashboardName
                    ),
                    mvcDefaultHostView.ToJsonString(),
                    scopes: SettingScopes.All,
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider
                ),
                new SettingDefinition(
                    GetSettingName(
                        HCMv3DashboardCustomizationConsts.Applications.Mvc,
                        mvcDefaultTenantView.DashboardName
                    ),
                    mvcDefaultTenantView.ToJsonString(),
                    scopes: SettingScopes.All,
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider
                ),
                new SettingDefinition(
                    GetSettingName(
                        HCMv3DashboardCustomizationConsts.Applications.Angular,
                        angularDefaultHostView.DashboardName
                    ),
                    angularDefaultHostView.ToJsonString(),
                    scopes: SettingScopes.All,
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider
                ),
                new SettingDefinition(
                    GetSettingName(
                        HCMv3DashboardCustomizationConsts.Applications.Angular,
                        angularDefaultTenantView.DashboardName
                    ),
                    angularDefaultTenantView.ToJsonString(),
                    scopes: SettingScopes.All,
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider
                ),
                new SettingDefinition(
                    GetSettingName(
                        HCMv3DashboardCustomizationConsts.Applications.React,
                        reactDefaultHostView.DashboardName
                    ),
                    reactDefaultHostView.ToJsonString(),
                    scopes: SettingScopes.All,
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider
                ),
                new SettingDefinition(
                    GetSettingName(
                        HCMv3DashboardCustomizationConsts.Applications.React,
                        reactDefaultTenantView.DashboardName
                    ),
                    reactDefaultTenantView.ToJsonString(),
                    scopes: SettingScopes.All,
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider
                )
            };
    }

    public Dashboard GetDefaultMvcHostDashboardView()
    {
        //It is the default dashboard view which your user will see if they don't do any customization.
        return new Dashboard
        {
            DashboardName = HCMv3DashboardCustomizationConsts.DashboardNames.DefaultHostDashboard,
            Pages = new List<Page>
                {
                    new Page($"Page_{HCMv3DashboardCustomizationConsts.DashboardNames.DefaultHostDashboard}_{HCMv3DashboardCustomizationConsts.DefaultPageUniqueName}_{1}")
                    {
                        Name = HCMv3DashboardCustomizationConsts.DefaultPageName,
                        Widgets = new List<Widget>
                        {
                            new Widget
                            {
                                WidgetId = HCMv3DashboardCustomizationConsts.Widgets.Host
                                    .TopStats, // Top Stats
                                Height = 6,
                                Width = 12,
                                PositionX = 0,
                                PositionY = 0
                            },
                            new Widget
                            {
                                WidgetId =
                                    HCMv3DashboardCustomizationConsts.Widgets.Host
                                        .IncomeStatistics, // Income Statistics
                                Height = 11,
                                Width = 7,
                                PositionX = 0,
                                PositionY = 6
                            },
                            new Widget
                            {
                                WidgetId = HCMv3DashboardCustomizationConsts.Widgets.Host
                                    .RecentTenants, // Recent tenants
                                Height = 10,
                                Width = 5,
                                PositionX = 7,
                                PositionY = 17
                            },
                            new Widget
                            {
                                WidgetId = HCMv3DashboardCustomizationConsts.Widgets.Host
                                    .SubscriptionExpiringTenants, // Subscription expiring tenants
                                Height = 10,
                                Width = 7,
                                PositionX = 0,
                                PositionY = 17
                            },
                            new Widget
                            {
                                WidgetId = HCMv3DashboardCustomizationConsts.Widgets.Host
                                    .EditionStatistics, // Edition statistics
                                Height = 11,
                                Width = 5,
                                PositionX = 7,
                                PositionY = 6
                            }
                        }
                    }
                }
        };
    }

    public Dashboard GetDefaultMvcTenantDashboardView()
    {
        // It is the default dashboard view which your user will see if they don't do any customization.
        return new Dashboard
        {
            DashboardName = HCMv3DashboardCustomizationConsts.DashboardNames.DefaultTenantDashboard,
            Pages = new List<Page>
                {
                    new Page($"Page_{HCMv3DashboardCustomizationConsts.DashboardNames.DefaultTenantDashboard}_{HCMv3DashboardCustomizationConsts.DefaultPageUniqueName}_{1}")
                    {
                        Name = HCMv3DashboardCustomizationConsts.DefaultPageName,
                        Widgets = new List<Widget>
                        {
                            new Widget
                            {
                                WidgetId = HCMv3DashboardCustomizationConsts.Widgets.Tenant
                                    .GeneralStats, // General Stats
                                Height = 9,
                                Width = 6,
                                PositionX = 0,
                                PositionY = 19
                            },
                            new Widget
                            {
                                WidgetId = HCMv3DashboardCustomizationConsts.Widgets.Tenant
                                    .ProfitShare, // Profit Share
                                Height = 13,
                                Width = 6,
                                PositionX = 0,
                                PositionY = 28
                            },
                            new Widget
                            {
                                WidgetId =
                                    HCMv3DashboardCustomizationConsts.Widgets.Tenant
                                        .MemberActivity, // Memeber Activity
                                Height = 13,
                                Width = 6,
                                PositionX = 6,
                                PositionY = 28
                            },
                            new Widget
                            {
                                WidgetId = HCMv3DashboardCustomizationConsts.Widgets.Tenant
                                    .RegionalStats, // Regional Stats
                                Height = 14,
                                Width = 6,
                                PositionX = 6,
                                PositionY = 5
                            },
                            new Widget
                            {
                                WidgetId = HCMv3DashboardCustomizationConsts.Widgets.Tenant
                                    .DailySales, // Daily Sales
                                Height = 9,
                                Width = 6,
                                PositionX = 6,
                                PositionY = 19
                            },
                            new Widget
                            {
                                WidgetId = HCMv3DashboardCustomizationConsts.Widgets.Tenant
                                    .TopStats, // Top Stats
                                Height = 5,
                                Width = 12,
                                PositionX = 0,
                                PositionY = 0
                            },
                            new Widget
                            {
                                WidgetId = HCMv3DashboardCustomizationConsts.Widgets.Tenant
                                    .SalesSummary, // Sales Summary
                                Height = 14,
                                Width = 6,
                                PositionX = 0,
                                PositionY = 5
                            }
                        }
                    }
                }
        };
    }

    public Dashboard GetDefaultAngularHostDashboardView()
    {
        // It is the default dashboard view which your user will see if they don't do any customization.
        return new Dashboard
        {
            DashboardName = HCMv3DashboardCustomizationConsts.DashboardNames.DefaultHostDashboard,
            Pages = new List<Page>
                {
                    new Page($"Page_{HCMv3DashboardCustomizationConsts.DashboardNames.DefaultHostDashboard}_{HCMv3DashboardCustomizationConsts.DefaultPageUniqueName}_{1}")
                    {
                        Name = HCMv3DashboardCustomizationConsts.DefaultPageName,
                        Widgets = new List<Widget>
                        {
                            new Widget
                            {
                                WidgetId = HCMv3DashboardCustomizationConsts.Widgets.Host
                                    .TopStats, // Top Stats
                                Height = 5,
                                Width = 12,
                                PositionX = 0,
                                PositionY = 0
                            },
                            new Widget
                            {
                                WidgetId =
                                    HCMv3DashboardCustomizationConsts.Widgets.Host
                                        .IncomeStatistics, // Income Statistics
                                Height = 8,
                                Width = 7,
                                PositionX = 0,
                                PositionY = 5
                            },
                            new Widget
                            {
                                WidgetId =
                                    HCMv3DashboardCustomizationConsts.Widgets.Host
                                        .RecentTenants, // Recent tenants
                                Height = 9,
                                Width = 5,
                                PositionX = 7,
                                PositionY = 13
                            },
                            new Widget
                            {
                                WidgetId = HCMv3DashboardCustomizationConsts.Widgets.Host
                                    .SubscriptionExpiringTenants, // Subscription expiring tenants
                                Height = 9,
                                Width = 7,
                                PositionX = 0,
                                PositionY = 13
                            },
                            new Widget
                            {
                                WidgetId = HCMv3DashboardCustomizationConsts.Widgets.Host
                                    .EditionStatistics, // Edition statistics
                                Height = 8,
                                Width = 5,
                                PositionX = 7,
                                PositionY = 5
                            }
                        }
                    }
                }
        };
    }

    public Dashboard GetDefaultAngularTenantDashboardView()
    {
        //It is the default dashboard view which your user will see if they don't do any customization.
        return new Dashboard
        {
            DashboardName = HCMv3DashboardCustomizationConsts.DashboardNames.DefaultTenantDashboard,
            Pages = new List<Page>
                {
                    new Page($"Page_{HCMv3DashboardCustomizationConsts.DashboardNames.DefaultTenantDashboard}_{HCMv3DashboardCustomizationConsts.DefaultPageUniqueName}_{1}")
                    {
                        Name = HCMv3DashboardCustomizationConsts.DefaultPageName,
                        Widgets = new List<Widget>
                        {
                            new Widget
                            {
                                WidgetId = HCMv3DashboardCustomizationConsts.Widgets.Tenant
                                    .TopStats, // Top Stats
                                Height = 4,
                                Width = 12,
                                PositionX = 0,
                                PositionY = 0
                            },
                            new Widget
                            {
                                WidgetId = HCMv3DashboardCustomizationConsts.Widgets.Tenant
                                    .SalesSummary, // Sales Summary
                                Height = 12,
                                Width = 6,
                                PositionX = 0,
                                PositionY = 4
                            },
                            new Widget
                            {
                                WidgetId = HCMv3DashboardCustomizationConsts.Widgets.Tenant
                                    .RegionalStats, // Regional Stats
                                Height = 12,
                                Width = 6,
                                PositionX = 6,
                                PositionY = 4
                            },
                            new Widget
                            {
                                WidgetId = HCMv3DashboardCustomizationConsts.Widgets.Tenant
                                    .GeneralStats, // General Stats
                                Height = 8,
                                Width = 6,
                                PositionX = 0,
                                PositionY = 16
                            },
                            new Widget
                            {
                                WidgetId = HCMv3DashboardCustomizationConsts.Widgets.Tenant
                                    .DailySales, // Daily Sales
                                Height = 8,
                                Width = 6,
                                PositionX = 6,
                                PositionY = 16
                            },
                            new Widget
                            {
                                WidgetId = HCMv3DashboardCustomizationConsts.Widgets.Tenant
                                    .ProfitShare, // Profit Share
                                Height = 11,
                                Width = 6,
                                PositionX = 0,
                                PositionY = 24
                            },
                            new Widget
                            {
                                WidgetId =
                                    HCMv3DashboardCustomizationConsts.Widgets.Tenant
                                        .MemberActivity, // Member Activity
                                Height = 11,
                                Width = 6,
                                PositionX = 6,
                                PositionY = 24
                            }
                        }
                    }
                }
        };
    }

    public Dashboard GetDefaultReactHostDashboardView()
    {
        // It is the default dashboard view which your user will see if they don't do any customization.
        return new Dashboard
        {
            DashboardName = HCMv3DashboardCustomizationConsts.DashboardNames.DefaultHostDashboard,
            Pages = new List<Page>
                {
                    new Page($"Page_{HCMv3DashboardCustomizationConsts.DashboardNames.DefaultHostDashboard}_{HCMv3DashboardCustomizationConsts.DefaultPageUniqueName}_{1}")
                    {
                        Name = HCMv3DashboardCustomizationConsts.DefaultPageName,
                        Widgets = new List<Widget>
                        {
                            new Widget
                            {
                                WidgetId = HCMv3DashboardCustomizationConsts.Widgets.Host
                                    .TopStats, // Top Stats
                                Height = 5,
                                Width = 12,
                                PositionX = 0,
                                PositionY = 0
                            },
                            new Widget
                            {
                                WidgetId =
                                    HCMv3DashboardCustomizationConsts.Widgets.Host
                                        .IncomeStatistics, // Income Statistics
                                Height = 8,
                                Width = 7,
                                PositionX = 0,
                                PositionY = 5
                            },
                            new Widget
                            {
                                WidgetId =
                                    HCMv3DashboardCustomizationConsts.Widgets.Host
                                        .RecentTenants, // Recent tenants
                                Height = 9,
                                Width = 5,
                                PositionX = 7,
                                PositionY = 13
                            },
                            new Widget
                            {
                                WidgetId = HCMv3DashboardCustomizationConsts.Widgets.Host
                                    .SubscriptionExpiringTenants, // Subscription expiring tenants
                                Height = 9,
                                Width = 7,
                                PositionX = 0,
                                PositionY = 13
                            },
                            new Widget
                            {
                                WidgetId = HCMv3DashboardCustomizationConsts.Widgets.Host
                                    .EditionStatistics, // Edition statistics
                                Height = 8,
                                Width = 5,
                                PositionX = 7,
                                PositionY = 5
                            }
                        }
                    }
                }
        };
    }

    public Dashboard GetDefaultReactTenantDashboardView()
    {
        //It is the default dashboard view which your user will see if they don't do any customization.
        return new Dashboard
        {
            DashboardName = HCMv3DashboardCustomizationConsts.DashboardNames.DefaultTenantDashboard,
            Pages = new List<Page>
                {
                    new Page($"Page_{HCMv3DashboardCustomizationConsts.DashboardNames.DefaultTenantDashboard}_{HCMv3DashboardCustomizationConsts.DefaultPageUniqueName}_{1}")
                    {
                        Name = HCMv3DashboardCustomizationConsts.DefaultPageName,
                        Widgets = new List<Widget>
                        {
                            new Widget
                            {
                                WidgetId = HCMv3DashboardCustomizationConsts.Widgets.Tenant
                                    .TopStats, // Top Stats
                                Height = 4,
                                Width = 12,
                                PositionX = 0,
                                PositionY = 0
                            },
                            new Widget
                            {
                                WidgetId = HCMv3DashboardCustomizationConsts.Widgets.Tenant
                                    .SalesSummary, // Sales Summary
                                Height = 12,
                                Width = 6,
                                PositionX = 0,
                                PositionY = 4
                            },
                            new Widget
                            {
                                WidgetId = HCMv3DashboardCustomizationConsts.Widgets.Tenant
                                    .RegionalStats, // Regional Stats
                                Height = 12,
                                Width = 6,
                                PositionX = 6,
                                PositionY = 4
                            },
                            new Widget
                            {
                                WidgetId = HCMv3DashboardCustomizationConsts.Widgets.Tenant
                                    .GeneralStats, // General Stats
                                Height = 8,
                                Width = 6,
                                PositionX = 0,
                                PositionY = 16
                            },
                            new Widget
                            {
                                WidgetId = HCMv3DashboardCustomizationConsts.Widgets.Tenant
                                    .DailySales, // Daily Sales
                                Height = 8,
                                Width = 6,
                                PositionX = 6,
                                PositionY = 16
                            },
                            new Widget
                            {
                                WidgetId = HCMv3DashboardCustomizationConsts.Widgets.Tenant
                                    .ProfitShare, // Profit Share
                                Height = 11,
                                Width = 6,
                                PositionX = 0,
                                PositionY = 24
                            },
                            new Widget
                            {
                                WidgetId =
                                    HCMv3DashboardCustomizationConsts.Widgets.Tenant
                                        .MemberActivity, // Member Activity
                                Height = 11,
                                Width = 6,
                                PositionX = 6,
                                PositionY = 24
                            }
                        }
                    }
                }
        };
    }


    private IEnumerable<SettingDefinition> GetExternalLoginProviderSettings()
    {
        return GetFacebookExternalLoginProviderSettings()
            .Union(GetGoogleExternalLoginProviderSettings())
            .Union(GetTwitterExternalLoginProviderSettings())
            .Union(GetMicrosoftExternalLoginProviderSettings())
            .Union(GetOpenIdConnectExternalLoginProviderSettings())
            .Union(GetWsFederationExternalLoginProviderSettings());
    }

    private SettingDefinition[] GetFacebookExternalLoginProviderSettings()
    {
        string appId = GetFromSettings("Authentication:Facebook:AppId");
        string appSecret = GetFromSettings("Authentication:Facebook:AppSecret");

        var facebookExternalLoginProviderInfo = new FacebookExternalLoginProviderSettings()
        {
            AppId = appId,
            AppSecret = appSecret
        };

        return new[]
        {
                new SettingDefinition(
                    AppSettings.ExternalLoginProvider.Host.Facebook,
                    facebookExternalLoginProviderInfo.ToJsonString(),
                    isVisibleToClients: false,
                    scopes: SettingScopes.Application,
                    isEncrypted:true
                ),
                new SettingDefinition(
                    AppSettings.ExternalLoginProvider.Tenant.Facebook_IsDeactivated,
                    "false",
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider,
                    scopes: SettingScopes.Tenant
                ),
                new SettingDefinition( //default is empty for tenants
                    AppSettings.ExternalLoginProvider.Tenant.Facebook,
                    "",
                    isVisibleToClients: false,
                    scopes: SettingScopes.Tenant,
                    isEncrypted:true
                )
            };
    }

    private SettingDefinition[] GetGoogleExternalLoginProviderSettings()
    {
        string clientId = GetFromSettings("Authentication:Google:ClientId");
        string clientSecret = GetFromSettings("Authentication:Google:ClientSecret");
        string userInfoEndPoint = GetFromSettings("Authentication:Google:UserInfoEndpoint");

        var googleExternalLoginProviderInfo = new GoogleExternalLoginProviderSettings()
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            UserInfoEndpoint = userInfoEndPoint
        };

        return new[]
        {
                new SettingDefinition(
                    AppSettings.ExternalLoginProvider.Host.Google,
                    googleExternalLoginProviderInfo.ToJsonString(),
                    isVisibleToClients: false,
                    scopes: SettingScopes.Application,
                    isEncrypted:true
                ),
                new SettingDefinition(
                    AppSettings.ExternalLoginProvider.Tenant.Google_IsDeactivated,
                    "false",
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider,
                    scopes: SettingScopes.Tenant
                ),
                new SettingDefinition( //default is empty for tenants
                    AppSettings.ExternalLoginProvider.Tenant.Google,
                    "",
                    isVisibleToClients: false,
                    scopes: SettingScopes.Tenant,
                    isEncrypted:true
                ),
            };
    }

    private SettingDefinition[] GetTwitterExternalLoginProviderSettings()
    {
        string consumerKey = GetFromSettings("Authentication:Twitter:ConsumerKey");
        string consumerSecret = GetFromSettings("Authentication:Twitter:ConsumerSecret");

        var twitterExternalLoginProviderInfo = new TwitterExternalLoginProviderSettings
        {
            ConsumerKey = consumerKey,
            ConsumerSecret = consumerSecret
        };

        return new[]
        {
                new SettingDefinition(
                    AppSettings.ExternalLoginProvider.Host.Twitter,
                    twitterExternalLoginProviderInfo.ToJsonString(),
                    isVisibleToClients: false,
                    scopes: SettingScopes.Application,
                    isEncrypted:true
                ),
                new SettingDefinition(
                    AppSettings.ExternalLoginProvider.Tenant.Twitter_IsDeactivated,
                    "false",
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider,
                    scopes: SettingScopes.Tenant
                ),
                new SettingDefinition( //default is empty for tenants
                    AppSettings.ExternalLoginProvider.Tenant.Twitter,
                    "",
                    isVisibleToClients: false,
                    scopes: SettingScopes.Tenant,
                    isEncrypted:true
                ),
            };
    }

    private SettingDefinition[] GetMicrosoftExternalLoginProviderSettings()
    {
        string consumerKey = GetFromSettings("Authentication:Microsoft:ConsumerKey");
        string consumerSecret = GetFromSettings("Authentication:Microsoft:ConsumerSecret");

        var microsoftExternalLoginProviderInfo = new MicrosoftExternalLoginProviderSettings()
        {
            ClientId = consumerKey,
            ClientSecret = consumerSecret
        };


        return new[]
        {
                new SettingDefinition(
                    AppSettings.ExternalLoginProvider.Host.Microsoft,
                    microsoftExternalLoginProviderInfo.ToJsonString(),
                    isVisibleToClients: false,
                    scopes: SettingScopes.Application,
                    isEncrypted:true
                ),
                new SettingDefinition(
                    AppSettings.ExternalLoginProvider.Tenant.Microsoft_IsDeactivated,
                    "false",
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider,
                    scopes: SettingScopes.Tenant
                ),
                new SettingDefinition( //default is empty for tenants
                    AppSettings.ExternalLoginProvider.Tenant.Microsoft,
                    "",
                    isVisibleToClients: false,
                    scopes: SettingScopes.Tenant,
                    isEncrypted:true
                ),
            };
    }

    private SettingDefinition[] GetOpenIdConnectExternalLoginProviderSettings()
    {
        var clientId = GetFromSettings("Authentication:OpenId:ClientId");
        var clientSecret = GetFromSettings("Authentication:OpenId:ClientSecret");
        var authority = GetFromSettings("Authentication:OpenId:Authority");
        var loginUrl = GetFromSettings("Authentication:OpenId:LoginUrl");
        var validateIssuerStr = GetFromSettings("Authentication:OpenId:ValidateIssuer");
        var responseType = GetFromSettings("Authentication:OpenId:ResponseType");

        bool.TryParse(validateIssuerStr, out bool validateIssuer);

        var openIdConnectExternalLoginProviderInfo = new OpenIdConnectExternalLoginProviderSettings
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            Authority = authority,
            ValidateIssuer = validateIssuer,
            ResponseType = responseType
        };

        if (!loginUrl.IsNullOrEmpty())
        {
            openIdConnectExternalLoginProviderInfo.LoginUrl = loginUrl;
        }

        var jsonClaimMappings = new List<JsonClaimMapDto>();
        _appConfiguration.GetSection("Authentication:OpenId:ClaimsMapping").Bind(jsonClaimMappings);

        return new[]
        {
                new SettingDefinition(
                    AppSettings.ExternalLoginProvider.Host.OpenIdConnect,
                    openIdConnectExternalLoginProviderInfo.ToJsonString(),
                    isVisibleToClients: false,
                    scopes: SettingScopes.Application,
                    isEncrypted:true
                ),
                new SettingDefinition(
                    AppSettings.ExternalLoginProvider.Tenant.OpenIdConnect_IsDeactivated,
                    "false",
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider,
                    scopes: SettingScopes.Tenant
                ),
                new SettingDefinition( //default is empty for tenants
                    AppSettings.ExternalLoginProvider.Tenant.OpenIdConnect,
                    "",
                    isVisibleToClients: false,
                    scopes: SettingScopes.Tenant,
                    isEncrypted:true
                ),
                new SettingDefinition(
                    AppSettings.ExternalLoginProvider.OpenIdConnectMappedClaims,
                    jsonClaimMappings.ToJsonString(),
                    isVisibleToClients: false,
                    scopes: SettingScopes.Application | SettingScopes.Tenant
                )
            };
    }

    private SettingDefinition[] GetWsFederationExternalLoginProviderSettings()
    {
        var clientId = GetFromSettings("Authentication:WsFederation:ClientId");
        var wtrealm = GetFromSettings("Authentication:WsFederation:Wtrealm");
        var authority = GetFromSettings("Authentication:WsFederation:Authority");
        var tenant = GetFromSettings("Authentication:WsFederation:Tenant");
        var metaDataAddress = GetFromSettings("Authentication:WsFederation:MetaDataAddress");

        var wsFederationExternalLoginProviderInfo = new WsFederationExternalLoginProviderSettings()
        {
            ClientId = clientId,
            Tenant = tenant,
            Authority = authority,
            Wtrealm = wtrealm,
            MetaDataAddress = metaDataAddress
        };

        var jsonClaimMappings = new List<JsonClaimMapDto>();
        _appConfiguration.GetSection("Authentication:WsFederation:ClaimsMapping").Bind(jsonClaimMappings);

        return new[]
        {
                new SettingDefinition(
                    AppSettings.ExternalLoginProvider.Host.WsFederation,
                    wsFederationExternalLoginProviderInfo.ToJsonString(),
                    isVisibleToClients: false,
                    scopes: SettingScopes.Application,
                    isEncrypted:true
                ),
                new SettingDefinition( //default is empty for tenants
                    AppSettings.ExternalLoginProvider.Tenant.WsFederation,
                    "",
                    isVisibleToClients: false,
                    scopes: SettingScopes.Tenant,
                    isEncrypted:true
                ),
                new SettingDefinition(
                    AppSettings.ExternalLoginProvider.Tenant.WsFederation_IsDeactivated,
                    "false",
                    clientVisibilityProvider: _visibleSettingClientVisibilityProvider,
                    scopes: SettingScopes.Tenant
                ),
                new SettingDefinition(
                    AppSettings.ExternalLoginProvider.WsFederationMappedClaims,
                    jsonClaimMappings.ToJsonString(),
                    isVisibleToClients: false,
                    scopes: SettingScopes.Application | SettingScopes.Tenant
                )
            };
    }

    protected virtual ILocalizableString L(string name)
    {
        return new LocalizableString(name, HCMv3Consts.LocalizationSourceName);
    }
}

