using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Collections.Extensions;
using Abp.Configuration;
using Abp.Configuration.Startup;
using Abp.Extensions;
using Abp.Json;
using Abp.Net.Mail;
using Abp.Runtime.Security;
using Abp.Runtime.Session;
using Abp.Timing;
using Abp.UI;
using Abp.Zero.Configuration;
using Abp.Zero.Ldap.Configuration;
using HCMv3.Authentication;
using HCMv3.Authorization;
using HCMv3.Authorization.Session;
using HCMv3.Configuration.Dto;
using HCMv3.Configuration.Host.Dto;
using HCMv3.Configuration.Tenants.Dto;
using HCMv3.Security;
using HCMv3.Storage;
using HCMv3.Timing;

namespace HCMv3.Configuration.Tenants;

[AbpAuthorize(AppPermissions.Pages_Administration_Tenant_Settings)]
public class TenantSettingsAppService : SettingsAppServiceBase, ITenantSettingsAppService
{
    public IExternalLoginOptionsCacheManager ExternalLoginOptionsCacheManager { get; set; }

    private readonly IMultiTenancyConfig _multiTenancyConfig;
    private readonly ITimeZoneService _timeZoneService;
    private readonly IBinaryObjectManager _binaryObjectManager;
    private readonly IAbpZeroLdapModuleConfig _ldapModuleConfig;

    public TenantSettingsAppService(
        IAbpZeroLdapModuleConfig ldapModuleConfig,
        IMultiTenancyConfig multiTenancyConfig,
        ITimeZoneService timeZoneService,
        IEmailSender emailSender,
        IBinaryObjectManager binaryObjectManager,
        IAppConfigurationAccessor configurationAccessor
    ) : base(emailSender, configurationAccessor)
    {
        ExternalLoginOptionsCacheManager = NullExternalLoginOptionsCacheManager.Instance;

        _multiTenancyConfig = multiTenancyConfig;
        _ldapModuleConfig = ldapModuleConfig;
        _timeZoneService = timeZoneService;
        _binaryObjectManager = binaryObjectManager;
    }

    #region Get Settings

    public async Task<TenantSettingsEditDto> GetAllSettings()
    {
        var settings = new TenantSettingsEditDto
        {
            UserManagement = await GetUserManagementSettingsAsync(),
            Security = await GetSecuritySettingsAsync(),
            Billing = await GetBillingSettingsAsync(),
            OtherSettings = await GetOtherSettingsAsync(),
            Email = await GetEmailSettingsAsync(),
            ExternalLoginProviderSettings = await GetExternalLoginProviderSettings()
        };

        if (!_multiTenancyConfig.IsEnabled || Clock.SupportsMultipleTimezone)
        {
            settings.General = await GetGeneralSettingsAsync();
        }

        if (_ldapModuleConfig.IsEnabled)
        {
            settings.Ldap = await GetLdapSettingsAsync();
        }
        else
        {
            settings.Ldap = new LdapSettingsEditDto { IsModuleEnabled = false };
        }

        return settings;
    }

    private async Task<LdapSettingsEditDto> GetLdapSettingsAsync()
    {
        var tenantId = AbpSession.GetTenantId();
        return new LdapSettingsEditDto
        {
            IsModuleEnabled = true,
            IsEnabled = await SettingManager.GetSettingValueForTenantAsync<bool>(LdapSettingNames.IsEnabled,
                tenantId),
            Domain = await SettingManager.GetSettingValueForTenantAsync(LdapSettingNames.Domain,
                tenantId),
            UserName = await SettingManager.GetSettingValueForTenantAsync(LdapSettingNames.UserName,
                tenantId),
            Password = await SettingManager.GetSettingValueForTenantAsync(LdapSettingNames.Password,
                tenantId),
            UseSsl = await SettingManager.GetSettingValueForTenantAsync<bool>(LdapSettingNames.UseSsl,
                tenantId),
        };
    }

    private async Task<TenantEmailSettingsEditDto> GetEmailSettingsAsync()
    {
        var tenantId = AbpSession.GetTenantId();
        var useHostDefaultEmailSettings = await SettingManager.GetSettingValueForTenantAsync<bool>(
            AppSettings.Email.UseHostDefaultEmailSettings,
            tenantId
        );

        if (useHostDefaultEmailSettings)
        {
            return new TenantEmailSettingsEditDto
            {
                UseHostDefaultEmailSettings = true
            };
        }

        var smtpPassword = await SettingManager.GetSettingValueForTenantAsync(
            EmailSettingNames.Smtp.Password,
            tenantId
        );

        // If user uses default credentials, use authentication should be false.
        var useAuthentication = !await SettingManager.GetSettingValueForTenantAsync<bool>(
            EmailSettingNames.Smtp.UseDefaultCredentials, tenantId);

        return new TenantEmailSettingsEditDto
        {
            UseHostDefaultEmailSettings = false,
            DefaultFromAddress = await SettingManager.GetSettingValueForTenantAsync(
                EmailSettingNames.DefaultFromAddress,
                tenantId),
            DefaultFromDisplayName = await SettingManager.GetSettingValueForTenantAsync(
                EmailSettingNames.DefaultFromDisplayName,
                tenantId),
            SmtpHost = await SettingManager.GetSettingValueForTenantAsync(
                EmailSettingNames.Smtp.Host,
                tenantId),
            SmtpPort = await SettingManager.GetSettingValueForTenantAsync<int>(
                EmailSettingNames.Smtp.Port,
                tenantId),
            SmtpUserName = await SettingManager.GetSettingValueForTenantAsync(
                EmailSettingNames.Smtp.UserName,
                tenantId),
            SmtpPassword = SimpleStringCipher.Instance.Decrypt(smtpPassword),
            SmtpDomain = await SettingManager.GetSettingValueForTenantAsync(
                EmailSettingNames.Smtp.Domain,
                tenantId),
            SmtpEnableSsl = await SettingManager.GetSettingValueForTenantAsync<bool>(
                EmailSettingNames.Smtp.EnableSsl,
                tenantId),
            SmtpUseAuthentication = useAuthentication
        };
    }

    private async Task<GeneralSettingsEditDto> GetGeneralSettingsAsync()
    {
        var tenantId = AbpSession.GetTenantId();
        var settings = new GeneralSettingsEditDto();

        if (Clock.SupportsMultipleTimezone)
        {
            var timezone = await SettingManager.GetSettingValueForTenantAsync(
                TimingSettingNames.TimeZone,
                tenantId
            );

            settings.Timezone = timezone;
            settings.TimezoneForComparison = timezone;
        }

        var defaultTimeZoneId = await _timeZoneService.GetDefaultTimezoneAsync(
            SettingScopes.Tenant,
            AbpSession.TenantId
        );

        if (settings.Timezone == defaultTimeZoneId)
        {
            settings.Timezone = string.Empty;
        }

        return settings;
    }

    private async Task<TenantUserManagementSettingsEditDto> GetUserManagementSettingsAsync()
    {
        return new TenantUserManagementSettingsEditDto
        {
            AllowSelfRegistration = await SettingManager.GetSettingValueAsync<bool>(
                AppSettings.UserManagement.AllowSelfRegistration
            ),
            IsNewRegisteredUserActiveByDefault = await SettingManager.GetSettingValueAsync<bool>(
                AppSettings.UserManagement.IsNewRegisteredUserActiveByDefault
            ),
            IsEmailConfirmationRequiredForLogin = await SettingManager.GetSettingValueAsync<bool>(
                AbpZeroSettingNames.UserManagement.IsEmailConfirmationRequiredForLogin
            ),
            IsCookieConsentEnabled = await SettingManager.GetSettingValueAsync<bool>(
                AppSettings.UserManagement.IsCookieConsentEnabled
            ),
            IsQuickThemeSelectEnabled = await SettingManager.GetSettingValueAsync<bool>(
                AppSettings.UserManagement.IsQuickThemeSelectEnabled
            ),
            AllowUsingGravatarProfilePicture = await SettingManager.GetSettingValueAsync<bool>(
                AppSettings.UserManagement.AllowUsingGravatarProfilePicture
            ),

            CaptchaSettings = new CaptchaSettingsEditDto
            {
                UseCaptchaOnRegistration = await SettingManager.GetSettingValueAsync<bool>(
                    AppSettings.UserManagement.UseCaptchaOnRegistration
                ),
                UseCaptchaOnEmailActivation = await SettingManager.GetSettingValueAsync<bool>(
                    AppSettings.UserManagement.UseCaptchaOnEmailActivation
                ),
                UseCaptchaOnResetPassword = await SettingManager.GetSettingValueAsync<bool>(
                    AppSettings.UserManagement.UseCaptchaOnResetPassword
                ),
                UseCaptchaOnLogin = await SettingManager.GetSettingValueAsync<bool>(
                    AppSettings.UserManagement.UseCaptchaOnLogin
                ),
            },

            SessionTimeOutSettings = new SessionTimeOutSettingsEditDto()
            {
                IsEnabled = await SettingManager.GetSettingValueAsync<bool>(
                    AppSettings.UserManagement.SessionTimeOut.IsEnabled
                ),
                TimeOutSecond = await SettingManager.GetSettingValueAsync<int>(
                    AppSettings.UserManagement.SessionTimeOut.TimeOutSecond
                ),
                ShowTimeOutNotificationSecond = await SettingManager.GetSettingValueAsync<int>(
                    AppSettings.UserManagement.SessionTimeOut.ShowTimeOutNotificationSecond
                ),
                ShowLockScreenWhenTimedOut = await SettingManager.GetSettingValueAsync<bool>(
                    AppSettings.UserManagement.SessionTimeOut.ShowLockScreenWhenTimedOut
                )
            },

            PasswordlessLogin = await GetPasswordlessLoginSettingsAsync(),

            RestrictedEmailDomain = await SettingManager.GetSettingValueAsync(
                AppSettings.UserManagement.RestrictedEmailDomain
            ),

            IsRestrictedEmailDomainEnabled = await SettingManager.GetSettingValueAsync<bool>(
                AppSettings.UserManagement.IsRestrictedEmailDomainEnabled
            ),

            IsRestrictedEmailDomainEnabledForApplication = await IsRestrictedEmailDomainEnabledForApplicationAsync(),

            IsQrLoginEnabled = await SettingManager.GetSettingValueAsync<bool>(
                AppSettings.UserManagement.IsQrLoginEnabled
            ),

            IsQrLoginEnabledForApplication = await IsQrLoginEnabledForApplicationAsync()
        };
    }

    private async Task<SecuritySettingsEditDto> GetSecuritySettingsAsync()
    {
        var passwordComplexitySetting = new PasswordComplexitySetting
        {
            RequireDigit = await SettingManager.GetSettingValueAsync<bool>(
                AbpZeroSettingNames.UserManagement.PasswordComplexity.RequireDigit
            ),
            RequireLowercase = await SettingManager.GetSettingValueAsync<bool>(
                AbpZeroSettingNames.UserManagement.PasswordComplexity.RequireLowercase
            ),
            RequireNonAlphanumeric = await SettingManager.GetSettingValueAsync<bool>(
                AbpZeroSettingNames.UserManagement.PasswordComplexity.RequireNonAlphanumeric
            ),
            RequireUppercase = await SettingManager.GetSettingValueAsync<bool>(
                AbpZeroSettingNames.UserManagement.PasswordComplexity.RequireUppercase
            ),
            RequiredLength = await SettingManager.GetSettingValueAsync<int>(
                AbpZeroSettingNames.UserManagement.PasswordComplexity.RequiredLength
            )
        };

        var defaultPasswordComplexitySetting = new PasswordComplexitySetting
        {
            RequireDigit = await SettingManager.GetSettingValueForApplicationAsync<bool>(
                AbpZeroSettingNames.UserManagement.PasswordComplexity.RequireDigit
            ),
            RequireLowercase = await SettingManager.GetSettingValueForApplicationAsync<bool>(
                AbpZeroSettingNames.UserManagement.PasswordComplexity.RequireLowercase
            ),
            RequireNonAlphanumeric = await SettingManager.GetSettingValueForApplicationAsync<bool>(
                AbpZeroSettingNames.UserManagement.PasswordComplexity.RequireNonAlphanumeric
            ),
            RequireUppercase = await SettingManager.GetSettingValueForApplicationAsync<bool>(
                AbpZeroSettingNames.UserManagement.PasswordComplexity.RequireUppercase
            ),
            RequiredLength = await SettingManager.GetSettingValueForApplicationAsync<int>(
                AbpZeroSettingNames.UserManagement.PasswordComplexity.RequiredLength
            )
        };

        return new SecuritySettingsEditDto
        {
            UseDefaultPasswordComplexitySettings = passwordComplexitySetting.Equals(
                defaultPasswordComplexitySetting
            ),
            PasswordComplexity = passwordComplexitySetting,
            DefaultPasswordComplexity = defaultPasswordComplexitySetting,
            UserLockOut = await GetUserLockOutSettingsAsync(),
            TwoFactorLogin = await GetTwoFactorLoginSettingsAsync(),
            AllowOneConcurrentLoginPerUser = await GetOneConcurrentLoginPerUserSetting(),
            UserPasswordSettings = await GetUserPasswordSettings(),
            SessionManagement = await GetSessionManagementSettingsAsync()
        };
    }

    private async Task<TenantBillingSettingsEditDto> GetBillingSettingsAsync()
    {
        return new TenantBillingSettingsEditDto()
        {
            LegalName = await SettingManager.GetSettingValueAsync(AppSettings.TenantManagement.BillingLegalName),
            Address = await SettingManager.GetSettingValueAsync(AppSettings.TenantManagement.BillingAddress),
            TaxVatNo = await SettingManager.GetSettingValueAsync(AppSettings.TenantManagement.BillingTaxVatNo)
        };
    }

    private async Task<TenantOtherSettingsEditDto> GetOtherSettingsAsync()
    {
        return new TenantOtherSettingsEditDto()
        {
            IsQuickThemeSelectEnabled = await SettingManager.GetSettingValueAsync<bool>(
                AppSettings.UserManagement.IsQuickThemeSelectEnabled
            )
        };
    }

    private async Task<UserLockOutSettingsEditDto> GetUserLockOutSettingsAsync()
    {
        return new UserLockOutSettingsEditDto
        {
            IsEnabled = await SettingManager.GetSettingValueAsync<bool>(
                AbpZeroSettingNames.UserManagement.UserLockOut.IsEnabled
            ),
            MaxFailedAccessAttemptsBeforeLockout = await SettingManager.GetSettingValueAsync<int>(
                AbpZeroSettingNames.UserManagement.UserLockOut.MaxFailedAccessAttemptsBeforeLockout
            ),
            DefaultAccountLockoutSeconds = await SettingManager.GetSettingValueAsync<int>(
                AbpZeroSettingNames.UserManagement.UserLockOut.DefaultAccountLockoutSeconds
            )
        };
    }

    private async Task<bool> IsTwoFactorLoginEnabledForApplicationAsync()
    {
        return await SettingManager.GetSettingValueForApplicationAsync<bool>(
            AbpZeroSettingNames.UserManagement.TwoFactorLogin.IsEnabled
        );
    }

    private async Task<TwoFactorLoginSettingsEditDto> GetTwoFactorLoginSettingsAsync()
    {
        var settings = new TwoFactorLoginSettingsEditDto
        {
            IsEnabledForApplication = await IsTwoFactorLoginEnabledForApplicationAsync()
        };

        if (_multiTenancyConfig.IsEnabled && !settings.IsEnabledForApplication)
        {
            return settings;
        }

        settings.IsEnabled = await SettingManager.GetSettingValueAsync<bool>(
            AbpZeroSettingNames.UserManagement.TwoFactorLogin.IsEnabled
        );
        settings.IsRememberBrowserEnabled = await SettingManager.GetSettingValueAsync<bool>(
            AbpZeroSettingNames.UserManagement.TwoFactorLogin.IsRememberBrowserEnabled
        );

        settings.IsEmailProviderEnabled = await SettingManager.GetSettingValueAsync<bool>(
            AbpZeroSettingNames.UserManagement.TwoFactorLogin.IsEmailProviderEnabled
        );
        settings.IsSmsProviderEnabled = await SettingManager.GetSettingValueAsync<bool>(
            AbpZeroSettingNames.UserManagement.TwoFactorLogin.IsSmsProviderEnabled
        );
        settings.IsGoogleAuthenticatorEnabled = await SettingManager.GetSettingValueAsync<bool>(
            AppSettings.UserManagement.TwoFactorLogin.IsGoogleAuthenticatorEnabled
        );

        return settings;
    }

    private async Task<bool> IsPasswordlessLoginEnabledForApplicationAsync()
    {
        var isEmailEnabledTask = SettingManager.GetSettingValueForApplicationAsync<bool>(
            AppSettings.UserManagement.PasswordlessLogin.IsEmailPasswordlessLoginEnabled
        );

        var isSmsEnabledTask = SettingManager.GetSettingValueForApplicationAsync<bool>(
            AppSettings.UserManagement.PasswordlessLogin.IsSmsPasswordlessLoginEnabled
        );

        await Task.WhenAll(isEmailEnabledTask, isSmsEnabledTask);

        var isEmailEnabled = await isEmailEnabledTask;
        var isSmsEnabled = await isSmsEnabledTask;

        return isEmailEnabled || isSmsEnabled;
    }

    private async Task<PasswordlessLoginSettingsEditDto> GetPasswordlessLoginSettingsAsync()
    {
        var settings = new PasswordlessLoginSettingsEditDto
        {
            IsEnabledForApplication = await IsPasswordlessLoginEnabledForApplicationAsync(),

            IsEmailProviderEnabledForApplication = await SettingManager.GetSettingValueForApplicationAsync<bool>(
                AppSettings.UserManagement.PasswordlessLogin.IsEmailPasswordlessLoginEnabled
            ),

            IsSmsProviderEnabledForApplication = await SettingManager.GetSettingValueForApplicationAsync<bool>(
                AppSettings.UserManagement.PasswordlessLogin.IsSmsPasswordlessLoginEnabled
            )
        };

        if (_multiTenancyConfig.IsEnabled && !settings.IsEnabledForApplication)
        {
            return settings;
        }

        settings.IsEmailProviderEnabled = await SettingManager.GetSettingValueAsync<bool>(
            AppSettings.UserManagement.PasswordlessLogin.IsEmailPasswordlessLoginEnabled
        );

        settings.IsSmsProviderEnabled = await SettingManager.GetSettingValueAsync<bool>(
            AppSettings.UserManagement.PasswordlessLogin.IsSmsPasswordlessLoginEnabled
        );

        return settings;
    }

    private async Task<bool> GetOneConcurrentLoginPerUserSetting()
    {
        return await SettingManager.GetSettingValueAsync<bool>(
            AppSettings.UserManagement.AllowOneConcurrentLoginPerUser
        );
    }

    private async Task<ExternalLoginProviderSettingsEditDto> GetExternalLoginProviderSettings()
    {
        var tenantId = AbpSession.GetTenantId();
        var facebookSettings = await SettingManager.GetSettingValueForTenantAsync(
            AppSettings.ExternalLoginProvider.Tenant.Facebook,
            tenantId
        );

        var googleSettings = await SettingManager.GetSettingValueForTenantAsync(
            AppSettings.ExternalLoginProvider.Tenant.Google,
            tenantId
        );

        var twitterSettings = await SettingManager.GetSettingValueForTenantAsync(
            AppSettings.ExternalLoginProvider.Tenant.Twitter,
            tenantId
        );

        var microsoftSettings = await SettingManager.GetSettingValueForTenantAsync(
            AppSettings.ExternalLoginProvider.Tenant.Microsoft,
            tenantId
        );

        var openIdConnectSettings = await SettingManager.GetSettingValueForTenantAsync(
            AppSettings.ExternalLoginProvider.Tenant.OpenIdConnect,
            tenantId
        );

        var openIdConnectMappedClaims = await SettingManager.GetSettingValueAsync(
            AppSettings.ExternalLoginProvider.OpenIdConnectMappedClaims
        );

        var wsFederationSettings = await SettingManager.GetSettingValueForTenantAsync(
            AppSettings.ExternalLoginProvider.Tenant.WsFederation,
            tenantId
        );

        var wsFederationMappedClaims = await SettingManager.GetSettingValueAsync(
            AppSettings.ExternalLoginProvider.WsFederationMappedClaims
        );

        return new ExternalLoginProviderSettingsEditDto
        {
            Facebook_IsDeactivated = await SettingManager.GetSettingValueForTenantAsync<bool>(
                AppSettings.ExternalLoginProvider.Tenant.Facebook_IsDeactivated,
                tenantId
            ),
            Facebook = facebookSettings.IsNullOrWhiteSpace()
                ? new FacebookExternalLoginProviderSettings()
                : facebookSettings.FromJsonString<FacebookExternalLoginProviderSettings>(),

            Google_IsDeactivated = await SettingManager.GetSettingValueForTenantAsync<bool>(
                AppSettings.ExternalLoginProvider.Tenant.Google_IsDeactivated,
                tenantId
            ),
            Google = googleSettings.IsNullOrWhiteSpace()
                ? new GoogleExternalLoginProviderSettings()
                : googleSettings.FromJsonString<GoogleExternalLoginProviderSettings>(),

            Twitter_IsDeactivated = await SettingManager.GetSettingValueForTenantAsync<bool>(
                AppSettings.ExternalLoginProvider.Tenant.Twitter_IsDeactivated,
                tenantId
            ),
            Twitter = twitterSettings.IsNullOrWhiteSpace()
                ? new TwitterExternalLoginProviderSettings()
                : twitterSettings.FromJsonString<TwitterExternalLoginProviderSettings>(),

            Microsoft_IsDeactivated = await SettingManager.GetSettingValueForTenantAsync<bool>(
                AppSettings.ExternalLoginProvider.Tenant.Microsoft_IsDeactivated,
                tenantId
            ),
            Microsoft = microsoftSettings.IsNullOrWhiteSpace()
                ? new MicrosoftExternalLoginProviderSettings()
                : microsoftSettings.FromJsonString<MicrosoftExternalLoginProviderSettings>(),

            OpenIdConnect_IsDeactivated = await SettingManager.GetSettingValueForTenantAsync<bool>(
                AppSettings.ExternalLoginProvider.Tenant.OpenIdConnect_IsDeactivated,
                tenantId
            ),
            OpenIdConnect = openIdConnectSettings.IsNullOrWhiteSpace()
                ? new OpenIdConnectExternalLoginProviderSettings()
                : openIdConnectSettings.FromJsonString<OpenIdConnectExternalLoginProviderSettings>(),
            OpenIdConnectClaimsMapping = openIdConnectMappedClaims.IsNullOrWhiteSpace()
                ? new List<JsonClaimMapDto>()
                : openIdConnectMappedClaims.FromJsonString<List<JsonClaimMapDto>>(),

            WsFederation_IsDeactivated = await SettingManager.GetSettingValueForTenantAsync<bool>(
                AppSettings.ExternalLoginProvider.Tenant.WsFederation_IsDeactivated,
                tenantId
            ),
            WsFederation = wsFederationSettings.IsNullOrWhiteSpace()
                ? new WsFederationExternalLoginProviderSettings()
                : wsFederationSettings.FromJsonString<WsFederationExternalLoginProviderSettings>(),
            WsFederationClaimsMapping = wsFederationMappedClaims.IsNullOrWhiteSpace()
                ? new List<JsonClaimMapDto>()
                : wsFederationMappedClaims.FromJsonString<List<JsonClaimMapDto>>()
        };
    }

    private async Task<bool> IsRestrictedEmailDomainEnabledForApplicationAsync()
    {
        return await SettingManager.GetSettingValueForApplicationAsync<bool>(
            AppSettings.TenantManagement.IsRestrictedEmailDomainEnabled
        );
    }

    private async Task<bool> IsQrLoginEnabledForApplicationAsync()
    {
        return await SettingManager.GetSettingValueForApplicationAsync<bool>(
            AppSettings.UserManagement.IsQrLoginEnabled
        );
    }

    private async Task<UserPasswordSettingsEditDto> GetUserPasswordSettings()
    {
        return new UserPasswordSettingsEditDto
        {
            CheckingLastXPasswordCount = await SettingManager.GetSettingValueAsync<int>(
                AppSettings.UserManagement.Password.CheckingLastXPasswordCount
            ),
            EnableCheckingLastXPasswordWhenPasswordChange = await SettingManager.GetSettingValueAsync<bool>(
                AppSettings.UserManagement.Password.EnableCheckingLastXPasswordWhenPasswordChange
            ),
            EnablePasswordExpiration = await SettingManager.GetSettingValueAsync<bool>(
                AppSettings.UserManagement.Password.EnablePasswordExpiration
            ),
            PasswordResetCodeExpirationHours = await SettingManager.GetSettingValueAsync<int>(
                AppSettings.UserManagement.Password.PasswordResetCodeExpirationHours
            ),
            PasswordExpirationDayCount = await SettingManager.GetSettingValueAsync<int>(
                AppSettings.UserManagement.Password.PasswordExpirationDayCount
            ),
        };
    }

    #endregion

    #region Update Settings

    public async Task UpdateAllSettings(TenantSettingsEditDto input)
    {
        await UpdateUserManagementSettingsAsync(input.UserManagement);
        await UpdateSecuritySettingsAsync(input.Security);
        await UpdateBillingSettingsAsync(input.Billing);
        await UpdateEmailSettingsAsync(input.Email);
        await UpdateExternalLoginSettingsAsync(input.ExternalLoginProviderSettings);

        //Time Zone
        if (Clock.SupportsMultipleTimezone)
        {
            var tenantId = AbpSession.GetTenantId();
            if (input.General.Timezone.IsNullOrEmpty())
            {
                var defaultValue = await _timeZoneService.GetDefaultTimezoneAsync(
                    SettingScopes.Tenant, AbpSession.TenantId
                );

                await SettingManager.ChangeSettingForTenantAsync(
                    tenantId,
                    TimingSettingNames.TimeZone, defaultValue
                );
            }
            else
            {
                _timeZoneService.ValidateTimezone(input.General.Timezone);
                await SettingManager.ChangeSettingForTenantAsync(
                    tenantId,
                    TimingSettingNames.TimeZone,
                    input.General.Timezone
                );
            }
        }

        if (!_multiTenancyConfig.IsEnabled)
        {
            await UpdateOtherSettingsAsync(input.OtherSettings);

            input.ValidateHostSettings();
        }

        if (_ldapModuleConfig.IsEnabled)
        {
            await UpdateLdapSettingsAsync(input.Ldap);
        }
    }

    private async Task UpdateOtherSettingsAsync(TenantOtherSettingsEditDto input)
    {
        await SettingManager.ChangeSettingForTenantAsync(
            AbpSession.GetTenantId(),
            AppSettings.UserManagement.IsQuickThemeSelectEnabled,
            input.IsQuickThemeSelectEnabled.ToString().ToLowerInvariant()
        );
    }

    private async Task UpdateBillingSettingsAsync(TenantBillingSettingsEditDto input)
    {
        var tenantId = AbpSession.GetTenantId();
        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            AppSettings.TenantManagement.BillingLegalName,
            input.LegalName
        );

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            AppSettings.TenantManagement.BillingAddress,
            input.Address
        );

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            AppSettings.TenantManagement.BillingTaxVatNo,
            input.TaxVatNo
        );
    }

    private async Task UpdateLdapSettingsAsync(LdapSettingsEditDto input)
    {
        var tenantId = AbpSession.GetTenantId();
        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            LdapSettingNames.IsEnabled,
            input.IsEnabled.ToString().ToLowerInvariant()
        );

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            LdapSettingNames.Domain,
            input.Domain.IsNullOrWhiteSpace() ? null : input.Domain
        );

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            LdapSettingNames.UserName,
            input.UserName.IsNullOrWhiteSpace() ? null : input.UserName
        );

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            LdapSettingNames.Password,
            input.Password.IsNullOrWhiteSpace() ? null : input.Password
        );

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            LdapSettingNames.UseSsl,
            input.UseSsl.ToString().ToLowerInvariant()
        );
    }

    private async Task UpdateEmailSettingsAsync(TenantEmailSettingsEditDto input)
    {
        if (_multiTenancyConfig.IsEnabled && !HCMv3Consts.AllowTenantsToChangeEmailSettings)
        {
            return;
        }

        var useHostDefaultEmailSettings = _multiTenancyConfig.IsEnabled && input.UseHostDefaultEmailSettings;

        if (useHostDefaultEmailSettings)
        {
            var smtpPassword = await SettingManager.GetSettingValueForApplicationAsync(
                EmailSettingNames.Smtp.Password
            );

            input = new TenantEmailSettingsEditDto
            {
                UseHostDefaultEmailSettings = true,
                DefaultFromAddress = await SettingManager.GetSettingValueForApplicationAsync(
                    EmailSettingNames.DefaultFromAddress
                ),
                DefaultFromDisplayName = await SettingManager.GetSettingValueForApplicationAsync(
                    EmailSettingNames.DefaultFromDisplayName
                ),
                SmtpHost = await SettingManager.GetSettingValueForApplicationAsync(
                    EmailSettingNames.Smtp.Host
                ),
                SmtpPort = await SettingManager.GetSettingValueForApplicationAsync<int>(
                    EmailSettingNames.Smtp.Port
                ),
                SmtpUserName = await SettingManager.GetSettingValueForApplicationAsync(
                    EmailSettingNames.Smtp.UserName
                ),
                SmtpPassword = SimpleStringCipher.Instance.Decrypt(smtpPassword),
                SmtpDomain = await SettingManager.GetSettingValueForApplicationAsync(
                    EmailSettingNames.Smtp.Domain
                ),
                SmtpEnableSsl = await SettingManager.GetSettingValueForApplicationAsync<bool>(
                    EmailSettingNames.Smtp.EnableSsl
                ),
                SmtpUseAuthentication = await SettingManager.GetSettingValueForApplicationAsync<bool>(
                    EmailSettingNames.Smtp.UseDefaultCredentials
                )
            };
        }

        var tenantId = AbpSession.GetTenantId();
        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            AppSettings.Email.UseHostDefaultEmailSettings,
            useHostDefaultEmailSettings.ToString().ToLowerInvariant()
        );

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            EmailSettingNames.DefaultFromAddress,
            input.DefaultFromAddress
        );

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            EmailSettingNames.DefaultFromDisplayName,
            input.DefaultFromDisplayName
        );

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            EmailSettingNames.Smtp.Host,
            input.SmtpHost
        );

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            EmailSettingNames.Smtp.Port,
            input.SmtpPort.ToString(CultureInfo.InvariantCulture)
        );

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            EmailSettingNames.Smtp.UserName,
            input.SmtpUserName
        );

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            EmailSettingNames.Smtp.Password,
            SimpleStringCipher.Instance.Encrypt(input.SmtpPassword)
        );

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            EmailSettingNames.Smtp.Domain,
            input.SmtpDomain
        );

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            EmailSettingNames.Smtp.EnableSsl,
            input.SmtpEnableSsl.ToString().ToLowerInvariant()
        );

        // If user uses authentication, use default credentials should be false.
        var useDefaultCredentials = (!input.SmtpUseAuthentication).ToString().ToLowerInvariant();

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            EmailSettingNames.Smtp.UseDefaultCredentials,
            useDefaultCredentials
        );
    }

    private async Task UpdateUserManagementSettingsAsync(TenantUserManagementSettingsEditDto settings)
    {
        await CheckUserConfirmationStatus(settings);

        CheckRestrictedEmailDomain(settings);

        var tenantId = AbpSession.GetTenantId();

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            AppSettings.UserManagement.AllowSelfRegistration,
            settings.AllowSelfRegistration.ToString().ToLowerInvariant()
        );

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            AppSettings.UserManagement.IsNewRegisteredUserActiveByDefault,
            settings.IsNewRegisteredUserActiveByDefault.ToString().ToLowerInvariant()
        );

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            AbpZeroSettingNames.UserManagement.IsEmailConfirmationRequiredForLogin,
            settings.IsEmailConfirmationRequiredForLogin.ToString().ToLowerInvariant()
        );

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            AppSettings.UserManagement.UseCaptchaOnRegistration,
            settings.CaptchaSettings.UseCaptchaOnRegistration.ToString().ToLowerInvariant()
        );

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            AppSettings.UserManagement.UseCaptchaOnEmailActivation,
            settings.CaptchaSettings.UseCaptchaOnEmailActivation.ToString().ToLowerInvariant()
        );

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            AppSettings.UserManagement.UseCaptchaOnResetPassword,
            settings.CaptchaSettings.UseCaptchaOnResetPassword.ToString().ToLowerInvariant()
        );

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            AppSettings.UserManagement.UseCaptchaOnLogin,
            settings.CaptchaSettings.UseCaptchaOnLogin.ToString().ToLowerInvariant()
        );

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            AppSettings.UserManagement.IsCookieConsentEnabled,
            settings.IsCookieConsentEnabled.ToString().ToLowerInvariant()
        );

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            AppSettings.UserManagement.AllowUsingGravatarProfilePicture,
            settings.AllowUsingGravatarProfilePicture.ToString().ToLowerInvariant()
        );

        await UpdateUserManagementSessionTimeOutSettingsAsync(settings.SessionTimeOutSettings);

        await UpdatePasswordlessLoginSettingsAsync(settings.PasswordlessLogin);

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            AppSettings.UserManagement.RestrictedEmailDomain,
            settings.RestrictedEmailDomain
        );

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            AppSettings.UserManagement.IsRestrictedEmailDomainEnabled,
            settings.IsRestrictedEmailDomainEnabled.ToString().ToLowerInvariant()
        );

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            AppSettings.UserManagement.IsQrLoginEnabled,
            settings.IsQrLoginEnabled.ToString().ToLowerInvariant()
        );
    }

    private async Task CheckUserConfirmationStatus(TenantUserManagementSettingsEditDto settings)
    {
        var user = await GetCurrentUserAsync();

        if (!user.IsEmailConfirmed && settings.IsEmailConfirmationRequiredForLogin)
        {
            throw new UserFriendlyException(L("CurrentUsersEmailIsNotConfirmed"));
        }
    }

    private async Task UpdateUserManagementSessionTimeOutSettingsAsync(SessionTimeOutSettingsEditDto settings)
    {
        var tenantId = AbpSession.GetTenantId();
        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            AppSettings.UserManagement.SessionTimeOut.IsEnabled,
            settings.IsEnabled.ToString().ToLowerInvariant()
        );

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            AppSettings.UserManagement.SessionTimeOut.TimeOutSecond,
            settings.TimeOutSecond.ToString()
        );

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            AppSettings.UserManagement.SessionTimeOut.ShowTimeOutNotificationSecond,
            settings.ShowTimeOutNotificationSecond.ToString()
        );

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            AppSettings.UserManagement.SessionTimeOut.ShowLockScreenWhenTimedOut,
            settings.ShowLockScreenWhenTimedOut.ToString()
        );
    }

    private async Task UpdateSecuritySettingsAsync(SecuritySettingsEditDto settings)
    {
        if (settings.UseDefaultPasswordComplexitySettings)
        {
            await UpdatePasswordComplexitySettingsAsync(settings.DefaultPasswordComplexity);
        }
        else
        {
            if (settings.PasswordComplexity.RequiredLength < settings.PasswordComplexity.AllowedMinimumLength)
            {
                throw new UserFriendlyException(L("AllowedMinimumLength",
                    settings.PasswordComplexity.AllowedMinimumLength));
            }

            await UpdatePasswordComplexitySettingsAsync(settings.PasswordComplexity);
        }

        await UpdateUserLockOutSettingsAsync(settings.UserLockOut);
        await UpdateTwoFactorLoginSettingsAsync(settings.TwoFactorLogin);
        await UpdateOneConcurrentLoginPerUserSettingAsync(settings.AllowOneConcurrentLoginPerUser);
        await UpdateUserPasswordSettings(settings.UserPasswordSettings);
        await UpdateSessionManagementSettingsAsync(settings.SessionManagement);
    }

    private async Task UpdatePasswordComplexitySettingsAsync(PasswordComplexitySetting settings)
    {
        var tenantId = AbpSession.GetTenantId();
        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            AbpZeroSettingNames.UserManagement.PasswordComplexity.RequireDigit,
            settings.RequireDigit.ToString()
        );

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            AbpZeroSettingNames.UserManagement.PasswordComplexity.RequireLowercase,
            settings.RequireLowercase.ToString()
        );

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            AbpZeroSettingNames.UserManagement.PasswordComplexity.RequireNonAlphanumeric,
            settings.RequireNonAlphanumeric.ToString()
        );

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            AbpZeroSettingNames.UserManagement.PasswordComplexity.RequireUppercase,
            settings.RequireUppercase.ToString()
        );

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            AbpZeroSettingNames.UserManagement.PasswordComplexity.RequiredLength,
            settings.RequiredLength.ToString()
        );
    }

    private async Task UpdateUserLockOutSettingsAsync(UserLockOutSettingsEditDto settings)
    {
        var tenantId = AbpSession.GetTenantId();

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            AbpZeroSettingNames.UserManagement.UserLockOut.IsEnabled,
            settings.IsEnabled.ToString().ToLowerInvariant()
        );

        if (settings.DefaultAccountLockoutSeconds.HasValue)
        {
            await SettingManager.ChangeSettingForTenantAsync(
                tenantId,
                AbpZeroSettingNames.UserManagement.UserLockOut.DefaultAccountLockoutSeconds,
                settings.DefaultAccountLockoutSeconds.ToString()
            );
        }

        if (settings.MaxFailedAccessAttemptsBeforeLockout.HasValue)
        {
            await SettingManager.ChangeSettingForTenantAsync(
                tenantId,
                AbpZeroSettingNames.UserManagement.UserLockOut.MaxFailedAccessAttemptsBeforeLockout,
                settings.MaxFailedAccessAttemptsBeforeLockout.ToString()
            );
        }
    }

    private async Task UpdateTwoFactorLoginSettingsAsync(TwoFactorLoginSettingsEditDto settings)
    {
        if (_multiTenancyConfig.IsEnabled &&
            !await IsTwoFactorLoginEnabledForApplicationAsync()) //Two factor login can not be used by tenants if disabled by the host
        {
            return;
        }

        var tenantId = AbpSession.GetTenantId();

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            AbpZeroSettingNames.UserManagement.TwoFactorLogin.IsEnabled,
            settings.IsEnabled.ToString().ToLowerInvariant()
        );

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            AbpZeroSettingNames.UserManagement.TwoFactorLogin.IsRememberBrowserEnabled,
            settings.IsRememberBrowserEnabled.ToString().ToLowerInvariant()
        );

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            AbpZeroSettingNames.UserManagement.TwoFactorLogin.IsEmailProviderEnabled,
            settings.IsEmailProviderEnabled.ToString().ToLowerInvariant()
        );

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            AbpZeroSettingNames.UserManagement.TwoFactorLogin.IsSmsProviderEnabled,
            settings.IsSmsProviderEnabled.ToString().ToLowerInvariant()
        );

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            AppSettings.UserManagement.TwoFactorLogin.IsGoogleAuthenticatorEnabled,
            settings.IsGoogleAuthenticatorEnabled.ToString().ToLowerInvariant()
        );
    }

    private async Task UpdatePasswordlessLoginSettingsAsync(PasswordlessLoginSettingsEditDto settings)
    {
        if (_multiTenancyConfig.IsEnabled &&
            !await IsPasswordlessLoginEnabledForApplicationAsync()) //Passwordless login can not be used by tenants if disabled by the host
        {
            return;
        }

        var tenantId = AbpSession.GetTenantId();

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            AppSettings.UserManagement.PasswordlessLogin.IsEmailPasswordlessLoginEnabled,
            settings.IsEmailProviderEnabled.ToString().ToLowerInvariant()
        );

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            AppSettings.UserManagement.PasswordlessLogin.IsSmsPasswordlessLoginEnabled,
            settings.IsSmsProviderEnabled.ToString().ToLowerInvariant()
        );
    }

    private async Task UpdateOneConcurrentLoginPerUserSettingAsync(bool allowOneConcurrentLoginPerUser)
    {
        if (_multiTenancyConfig.IsEnabled)
        {
            return;
        }

        await SettingManager.ChangeSettingForApplicationAsync(
            AppSettings.UserManagement.AllowOneConcurrentLoginPerUser,
            allowOneConcurrentLoginPerUser.ToString()
        );
    }

    private async Task<SessionManagementSettingsEditDto> GetSessionManagementSettingsAsync()
    {
        return new SessionManagementSettingsEditDto
        {
            IsEnabled = await SettingManager.GetSettingValueAsync<bool>(
                AppSettings.UserManagement.SessionManagement.IsEnabled),
            IsSessionFingerprintValidationEnabled = await SettingManager.GetSettingValueAsync<bool>(
                AppSettings.UserManagement.SessionManagement.IsSessionFingerprintValidationEnabled),
            SessionFingerprintValidationPolicy = await SettingManager.GetSettingValueAsync(
                AppSettings.UserManagement.SessionManagement.SessionFingerprintValidationPolicy),
            SessionAbsoluteTimeoutMinutes = await SettingManager.GetSettingValueAsync<int>(
                AppSettings.UserManagement.SessionManagement.SessionAbsoluteTimeoutMinutes),
            IsSessionRevocationEnabled = await SettingManager.GetSettingValueAsync<bool>(
                AppSettings.UserManagement.SessionManagement.IsSessionRevocationEnabled)
        };
    }

    private async Task UpdateSessionManagementSettingsAsync(SessionManagementSettingsEditDto settings)
    {
        if (settings == null)
        {
            return;
        }

        var tenantId = AbpSession.GetTenantId();

        await SettingManager.ChangeSettingForTenantAsync(tenantId,
            AppSettings.UserManagement.SessionManagement.IsEnabled, settings.IsEnabled.ToString());
        await SettingManager.ChangeSettingForTenantAsync(tenantId,
            AppSettings.UserManagement.SessionManagement.IsSessionFingerprintValidationEnabled,
            settings.IsSessionFingerprintValidationEnabled.ToString());
        await SettingManager.ChangeSettingForTenantAsync(tenantId,
            AppSettings.UserManagement.SessionManagement.SessionFingerprintValidationPolicy,
            settings.SessionFingerprintValidationPolicy ?? nameof(SessionFingerprintValidationPolicy.IpAndUserAgent));
        await SettingManager.ChangeSettingForTenantAsync(tenantId,
            AppSettings.UserManagement.SessionManagement.SessionAbsoluteTimeoutMinutes,
            settings.SessionAbsoluteTimeoutMinutes.ToString());
        await SettingManager.ChangeSettingForTenantAsync(tenantId,
            AppSettings.UserManagement.SessionManagement.IsSessionRevocationEnabled,
            settings.IsSessionRevocationEnabled.ToString());
    }

    private async Task UpdateExternalLoginSettingsAsync(ExternalLoginProviderSettingsEditDto input)
    {
        var tenantId = AbpSession.GetTenantId();

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            AppSettings.ExternalLoginProvider.Tenant.Facebook,
            input.Facebook == null || !input.Facebook.IsValid() ? "" : input.Facebook.ToJsonString()
        );

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            AppSettings.ExternalLoginProvider.Tenant.Facebook_IsDeactivated,
            input.Facebook_IsDeactivated.ToString()
        );

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            AppSettings.ExternalLoginProvider.Tenant.Google,
            input.Google == null || !input.Google.IsValid() ? "" : input.Google.ToJsonString()
        );

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            AppSettings.ExternalLoginProvider.Tenant.Google_IsDeactivated,
            input.Google_IsDeactivated.ToString()
        );

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            AppSettings.ExternalLoginProvider.Tenant.Twitter,
            input.Twitter == null || !input.Twitter.IsValid() ? "" : input.Twitter.ToJsonString()
        );

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            AppSettings.ExternalLoginProvider.Tenant.Twitter_IsDeactivated,
            input.Twitter_IsDeactivated.ToString()
        );

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            AppSettings.ExternalLoginProvider.Tenant.Microsoft,
            input.Microsoft == null || !input.Microsoft.IsValid() ? "" : input.Microsoft.ToJsonString()
        );

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            AppSettings.ExternalLoginProvider.Tenant.Microsoft_IsDeactivated,
            input.Microsoft_IsDeactivated.ToString()
        );

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            AppSettings.ExternalLoginProvider.Tenant.OpenIdConnect,
            input.OpenIdConnect == null || !input.OpenIdConnect.IsValid() ? "" : input.OpenIdConnect.ToJsonString()
        );

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            AppSettings.ExternalLoginProvider.Tenant.OpenIdConnect_IsDeactivated,
            input.OpenIdConnect_IsDeactivated.ToString()
        );

        var openIdConnectMappedClaimsValue = "";
        if (input.OpenIdConnect == null || !input.OpenIdConnect.IsValid() ||
            input.OpenIdConnectClaimsMapping.IsNullOrEmpty())
        {
            openIdConnectMappedClaimsValue = await SettingManager.GetSettingValueForApplicationAsync(
                AppSettings.ExternalLoginProvider.OpenIdConnectMappedClaims
            ); //set default value
        }
        else
        {
            openIdConnectMappedClaimsValue = input.OpenIdConnectClaimsMapping.ToJsonString();
        }

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            AppSettings.ExternalLoginProvider.OpenIdConnectMappedClaims,
            openIdConnectMappedClaimsValue
        );

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            AppSettings.ExternalLoginProvider.Tenant.WsFederation,
            input.WsFederation == null || !input.WsFederation.IsValid() ? "" : input.WsFederation.ToJsonString()
        );

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            AppSettings.ExternalLoginProvider.Tenant.WsFederation_IsDeactivated,
            input.WsFederation_IsDeactivated.ToString()
        );

        var wsFederationMappedClaimsValue = "";
        if (input.WsFederation == null || !input.WsFederation.IsValid() ||
            input.WsFederationClaimsMapping.IsNullOrEmpty())
        {
            wsFederationMappedClaimsValue = await SettingManager.GetSettingValueForApplicationAsync(
                AppSettings.ExternalLoginProvider.WsFederationMappedClaims
            ); //set default value
        }
        else
        {
            wsFederationMappedClaimsValue = input.WsFederationClaimsMapping.ToJsonString();
        }

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            AppSettings.ExternalLoginProvider.WsFederationMappedClaims,
            wsFederationMappedClaimsValue
        );

        ExternalLoginOptionsCacheManager.ClearCache();
    }

    private void CheckRestrictedEmailDomain(TenantUserManagementSettingsEditDto settings)
    {
        string emailDomainPattern = @"^[a-zA-Z0-9._%+-]+(?<!@)[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";

        if (settings.RestrictedEmailDomain.IsNullOrEmpty() && settings.IsRestrictedEmailDomainEnabled)
        {
            throw new UserFriendlyException(L("RestrictedEmailDomain_NullError"));
        }

        if (!settings.RestrictedEmailDomain.IsNullOrEmpty() &&
            !Regex.IsMatch(settings.RestrictedEmailDomain, emailDomainPattern))
        {
            throw new UserFriendlyException(L("RestrictedEmailDomain_InvalidError"));
        }
    }

    private async Task UpdateUserPasswordSettings(UserPasswordSettingsEditDto settings)
    {
        if (_multiTenancyConfig.IsEnabled)
        {
            return;
        }

        var tenantId = AbpSession.GetTenantId();

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            AppSettings.UserManagement.Password.EnableCheckingLastXPasswordWhenPasswordChange,
            settings.EnableCheckingLastXPasswordWhenPasswordChange.ToString().ToLowerInvariant()
        );

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            AppSettings.UserManagement.Password.CheckingLastXPasswordCount,
            settings.CheckingLastXPasswordCount.ToString()
        );

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            AppSettings.UserManagement.Password.EnablePasswordExpiration,
            settings.EnablePasswordExpiration.ToString().ToLowerInvariant()
        );

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            AppSettings.UserManagement.Password.PasswordExpirationDayCount,
            settings.PasswordExpirationDayCount.ToString()
        );

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            AppSettings.UserManagement.Password.PasswordResetCodeExpirationHours,
            settings.PasswordResetCodeExpirationHours.ToString()
        );
    }
    #endregion

    #region Logo & Css

    public async Task ClearDarkLogo()
    {
        var tenant = await GetCurrentTenantAsync();

        if (!tenant.HasDarkLogo())
        {
            return;
        }

        var logoObject = await _binaryObjectManager.GetOrNullAsync(tenant.DarkLogoId.Value);
        if (logoObject != null)
        {
            await _binaryObjectManager.DeleteAsync(tenant.DarkLogoId.Value);
        }

        tenant.ClearDarkLogo();
    }

    public async Task ClearDarkLogoMinimal()
    {
        var tenant = await GetCurrentTenantAsync();

        if (!tenant.HasDarkLogoMinimal())
        {
            return;
        }

        var logoObject = await _binaryObjectManager.GetOrNullAsync(tenant.DarkLogoMinimalId.Value);
        if (logoObject != null)
        {
            await _binaryObjectManager.DeleteAsync(tenant.DarkLogoMinimalId.Value);
        }

        tenant.ClearDarkLogoMinimal();
    }

    public async Task ClearLightLogo()
    {
        var tenant = await GetCurrentTenantAsync();

        if (!tenant.HasLightLogo())
        {
            return;
        }

        var logoObject = await _binaryObjectManager.GetOrNullAsync(tenant.LightLogoId.Value);
        if (logoObject != null)
        {
            await _binaryObjectManager.DeleteAsync(tenant.LightLogoId.Value);
        }

        tenant.ClearLightLogo();
    }

    public async Task ClearLightLogoMinimal()
    {
        var tenant = await GetCurrentTenantAsync();

        if (!tenant.HasLightLogoMinimal())
        {
            return;
        }

        var logoObject = await _binaryObjectManager.GetOrNullAsync(tenant.LightLogoMinimalId.Value);
        if (logoObject != null)
        {
            await _binaryObjectManager.DeleteAsync(tenant.LightLogoMinimalId.Value);
        }

        tenant.ClearLightLogoMinimal();
    }

    public async Task ClearCustomCss()
    {
        var tenant = await GetCurrentTenantAsync();

        if (!tenant.CustomCssId.HasValue)
        {
            return;
        }

        var cssObject = await _binaryObjectManager.GetOrNullAsync(tenant.CustomCssId.Value);
        if (cssObject != null)
        {
            await _binaryObjectManager.DeleteAsync(tenant.CustomCssId.Value);
        }

        tenant.CustomCssId = null;
    }

    #endregion
}
