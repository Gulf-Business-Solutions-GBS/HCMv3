namespace HCMv3.Configuration;

/// <summary>
/// Defines string constants for setting names in the application.
/// See <see cref="AppSettingProvider"/> for setting definitions.
/// </summary>
public static class AppSettings
{
    public static class HostManagement
    {
        public const string BillingLegalName = "App.HostManagement.BillingLegalName";
        public const string BillingAddress = "App.HostManagement.BillingAddress";
    }

    public static class Careers
    {
        /// <summary>
        /// Per-tenant master switch for anonymous candidate self-registration on the careers portal.
        /// Defaults to <c>false</c>: a tenant that has not deliberately opted in cannot have users created
        /// on it by unauthenticated internet traffic.
        ///
        /// This is an ADDITIONAL gate, ANDed with ABP's own
        /// <c>App.UserManagement.AllowSelfRegistration</c> (enforced inside
        /// <c>UserRegistrationManager.CheckSelfRegistrationIsEnabled</c>). It exists so that turning the
        /// careers portal on does not require reasoning about the main app's registration page, and so that
        /// turning careers registration off is one setting rather than a code change.
        /// </summary>
        public const string AllowCandidateSelfRegistration = "App.Careers.AllowCandidateSelfRegistration";
    }

    public static class DashboardCustomization
    {
        public const string Configuration = "App.DashboardCustomization.Configuration";
    }

    public static class UiManagement
    {
        public const string LayoutType = "App.UiManagement.LayoutType";
        public const string DarkMode = "App.UiManagement.DarkMode";
        public const string FixedBody = "App.UiManagement.Layout.FixedBody";
        public const string MobileFixedBody = "App.UiManagement.Layout.MobileFixedBody";

        public const string Theme = "App.UiManagement.Theme";

        public const string SearchActive = "App.UiManagement.MenuSearch";

        public static class Header
        {
            public const string DesktopFixedHeader = "App.UiManagement.Header.DesktopFixedHeader";
            public const string MobileFixedHeader = "App.UiManagement.Header.MobileFixedHeader";
            public const string Skin = "App.UiManagement.Header.Skin";
            public const string MinimizeType = "App.UiManagement.Header.MinimizeType";
            public const string MenuArrows = "App.UiManagement.Header.MenuArrows";
        }

        public static class SubHeader
        {
            public const string Fixed = "App.UiManagement.SubHeader.Fixed";
            public const string Style = "App.UiManagement.SubHeader.Style";
        }

        public static class LeftAside
        {
            public const string Position = "App.UiManagement.Left.Position";
            public const string AsideSkin = "App.UiManagement.Left.AsideSkin";
            public const string FixedAside = "App.UiManagement.Left.FixedAside";
            public const string AllowAsideMinimizing = "App.UiManagement.Left.AllowAsideMinimizing";
            public const string DefaultMinimizedAside = "App.UiManagement.Left.DefaultMinimizedAside";
            public const string HoverableAside = "App.UiManagement.Left.HoverableAside";
            public const string SubmenuToggle = "App.UiManagement.Left.SubmenuToggle";
        }

        public static class Footer
        {
            public const string DesktopFixedFooter = "App.UiManagement.Footer.DesktopFixedFooter";
            public const string MobileFixedFooter = "App.UiManagement.Footer.MobileFixedFooter";
            public const string FooterWidthType = "App.UiManagement.Footer.FooterWidthType";
        }

        public static class Toolbar
        {
            public const string DesktopFixedToolbar = "App.UiManagement.Toolbar.DesktopFixedToolbar";
            public const string MobileFixedToolbar = "App.UiManagement.Toolbar.MobileFixedToolbar";
        }
    }

    public static class TenantManagement
    {
        public const string AllowSelfRegistration = "App.TenantManagement.AllowSelfRegistration";
        public const string IsNewRegisteredTenantActiveByDefault = "App.TenantManagement.IsNewRegisteredTenantActiveByDefault";
        public const string UseCaptchaOnRegistration = "App.TenantManagement.UseCaptchaOnRegistration";
        public const string UseCaptchaOnEmailActivation = "App.TenantManagement.UseCaptchaOnEmailActivation";
        public const string UseCaptchaOnResetPassword = "App.TenantManagement.UseCaptchaOnResetPassword";
        public const string DefaultEdition = "App.TenantManagement.DefaultEdition";
        public const string SubscriptionExpireNotifyDayCount = "App.TenantManagement.SubscriptionExpireNotifyDayCount";
        public const string BillingLegalName = "App.TenantManagement.BillingLegalName";
        public const string BillingAddress = "App.TenantManagement.BillingAddress";
        public const string BillingTaxVatNo = "App.TenantManagement.BillingTaxVatNo";
        public const string IsRestrictedEmailDomainEnabled = "App.TenantManagement.IsRestrictedEmailDomainEnabled";
    }

    public static class UserManagement
    {
        public static class TwoFactorLogin
        {
            public const string IsGoogleAuthenticatorEnabled = "App.UserManagement.TwoFactorLogin.IsGoogleAuthenticatorEnabled";
        }

        public static class PasswordlessLogin
        {
            public const string IsEmailPasswordlessLoginEnabled = "App.UserManagement.PasswordlessLogin.IsEmailPasswordlessLoginEnabled";
            public const string IsSmsPasswordlessLoginEnabled = "App.UserManagement.PasswordlessLogin.IsSmsPasswordlessLoginEnabled";
        }

        public static class SessionTimeOut
        {
            public const string IsEnabled = "App.UserManagement.SessionTimeOut.IsEnabled";
            public const string TimeOutSecond = "App.UserManagement.SessionTimeOut.TimeOutSecond";
            public const string ShowTimeOutNotificationSecond = "App.UserManagement.SessionTimeOut.ShowTimeOutNotificationSecond";
            public const string ShowLockScreenWhenTimedOut = "App.UserManagement.SessionTimeOut.ShowLockScreenWhenTimedOut";
        }

        public static class SessionManagement
        {
            public const string IsEnabled = "App.UserManagement.SessionManagement.IsEnabled";
            public const string IsSessionFingerprintValidationEnabled = "App.UserManagement.SessionManagement.IsSessionFingerprintValidationEnabled";
            public const string SessionFingerprintValidationPolicy = "App.UserManagement.SessionManagement.SessionFingerprintValidationPolicy";
            public const string SessionAbsoluteTimeoutMinutes = "App.UserManagement.SessionManagement.SessionAbsoluteTimeoutMinutes";
            public const string IsSessionRevocationEnabled = "App.UserManagement.SessionManagement.IsSessionRevocationEnabled";
        }

        public const string IsQrLoginEnabled = "App.UserManagement.IsQrLoginEnabled";
        public const string AllowSelfRegistration = "App.UserManagement.AllowSelfRegistration";
        public const string IsNewRegisteredUserActiveByDefault = "App.UserManagement.IsNewRegisteredUserActiveByDefault";
        public const string UseCaptchaOnRegistration = "App.UserManagement.UseCaptchaOnRegistration";
        public const string UseCaptchaOnLogin = "App.UserManagement.UseCaptchaOnLogin";
        public const string UseCaptchaOnEmailActivation = "App.UserManagement.UseCaptchaOnEmailActivation";
        public const string UseCaptchaOnResetPassword = "App.UserManagement.UseCaptchaOnResetPassword";
        public const string SmsVerificationEnabled = "App.UserManagement.SmsVerificationEnabled";
        public const string IsCookieConsentEnabled = "App.UserManagement.IsCookieConsentEnabled";
        public const string IsQuickThemeSelectEnabled = "App.UserManagement.IsQuickThemeSelectEnabled";
        public const string AllowOneConcurrentLoginPerUser = "App.UserManagement.AllowOneConcurrentLoginPerUser";
        public const string AllowUsingGravatarProfilePicture = "App.UserManagement.AllowUsingGravatarProfilePicture";
        public const string UseGravatarProfilePicture = "App.UserManagement.UseGravatarProfilePicture";
        public const string RestrictedEmailDomain = "App.UserManagement.RestrictedEmailDomain";
        public const string IsRestrictedEmailDomainEnabled = "App.UserManagement.IsRestrictedEmailDomainEnabled";
        public const string MaxProfilePictureSizeInMB = "App.UserManagement.MaxProfilePictureSizeInMB";
        public const string MaxProfilePictureWidth = "App.UserManagement.MaxProfilePictureWidth";
        public const string MaxProfilePictureHeight = "App.UserManagement.MaxProfilePictureHeight";

        public static class Password
        {
            public const string EnableCheckingLastXPasswordWhenPasswordChange = "App.UserManagement.EnableCheckingLastXPasswordWhenPasswordChange";
            public const string CheckingLastXPasswordCount = "App.UserManagement.CheckingLastXPasswordCount";
            public const string EnablePasswordExpiration = "App.UserManagement.EnablePasswordExpiration";
            public const string PasswordExpirationDayCount = "App.UserManagement.PasswordExpirationDayCount";
            public const string PasswordResetCodeExpirationHours = "App.UserManagement.PasswordResetCodeExpirationHours";
        }
    }

    public static class Email
    {
        public const string UseHostDefaultEmailSettings = "App.Email.UseHostDefaultEmailSettings";
    }

    public static class Recaptcha
    {
        public const string SiteKey = "Recaptcha.SiteKey";
    }

    public static class ExternalLoginProvider
    {
        public const string OpenIdConnectMappedClaims = "ExternalLoginProvider.OpenIdConnect.MappedClaims";
        public const string WsFederationMappedClaims = "ExternalLoginProvider.WsFederation.MappedClaims";

        public static class Host
        {
            public const string Facebook = "ExternalLoginProvider.Facebook";
            public const string Google = "ExternalLoginProvider.Google";
            public const string Twitter = "ExternalLoginProvider.Twitter";
            public const string Microsoft = "ExternalLoginProvider.Microsoft";
            public const string OpenIdConnect = "ExternalLoginProvider.OpenIdConnect";
            public const string WsFederation = "ExternalLoginProvider.WsFederation";
        }

        public static class Tenant
        {
            public const string Facebook = "ExternalLoginProvider.Facebook.Tenant";
            public const string Facebook_IsDeactivated = "ExternalLoginProvider.Facebook.IsDeactivated";
            public const string Google = "ExternalLoginProvider.Google.Tenant";
            public const string Google_IsDeactivated = "ExternalLoginProvider.Google.IsDeactivated";
            public const string Twitter = "ExternalLoginProvider.Twitter.Tenant";
            public const string Twitter_IsDeactivated = "ExternalLoginProvider.Twitter.IsDeactivated";
            public const string Microsoft = "ExternalLoginProvider.Microsoft.Tenant";
            public const string Microsoft_IsDeactivated = "ExternalLoginProvider.Microsoft.IsDeactivated";
            public const string OpenIdConnect = "ExternalLoginProvider.OpenIdConnect.Tenant";
            public const string OpenIdConnect_IsDeactivated = "ExternalLoginProvider.OpenIdConnect.IsDeactivated";
            public const string WsFederation = "ExternalLoginProvider.WsFederation.Tenant";
            public const string WsFederation_IsDeactivated = "ExternalLoginProvider.WsFederation.IsDeactivated";
        }
    }
}

