using HCMv3.Configuration.Host.Dto;

namespace HCMv3.Configuration.Tenants.Dto;

public class TenantUserManagementSettingsEditDto
{
    public bool AllowSelfRegistration { get; set; }

    public bool IsNewRegisteredUserActiveByDefault { get; set; }

    public bool IsEmailConfirmationRequiredForLogin { get; set; }

    public bool IsCookieConsentEnabled { get; set; }

    public bool IsQuickThemeSelectEnabled { get; set; }

    public bool AllowUsingGravatarProfilePicture { get; set; }

    public CaptchaSettingsEditDto CaptchaSettings { get; set; }

    public SessionTimeOutSettingsEditDto SessionTimeOutSettings { get; set; }

    public PasswordlessLoginSettingsEditDto PasswordlessLogin { get; set; }

    public string RestrictedEmailDomain { get; set; }

    public bool IsRestrictedEmailDomainEnabled { get; set; }

    public bool IsRestrictedEmailDomainEnabledForApplication { get; set; }

    public bool IsQrLoginEnabled { get; set; }

    public bool IsQrLoginEnabledForApplication { get; set; }
}