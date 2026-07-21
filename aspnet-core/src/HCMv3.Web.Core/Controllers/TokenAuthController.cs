using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Abp;
using Abp.AspNetCore.Mvc.Authorization;
using Abp.AspNetZeroCore.Web.Authentication.External;
using Abp.Authorization;
using Abp.Authorization.Users;
using Abp.MultiTenancy;
using Abp.Configuration;
using Abp.Extensions;
using Abp.Localization;
using Abp.Net.Mail;
using Abp.Notifications;
using Abp.Runtime.Caching;
using Abp.Runtime.Security;
using Abp.Runtime.Session;
using Abp.Timing;
using Abp.UI;
using Abp.Zero.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using HCMv3.Authentication.TwoFactor;
using HCMv3.Authentication.TwoFactor.Google;
using HCMv3.Authorization;
using HCMv3.Authorization.Accounts.Dto;
using HCMv3.Authorization.Users;
using HCMv3.MultiTenancy;
using HCMv3.Web.Authentication.JwtBearer;
using HCMv3.Web.Authentication.TwoFactor;
using HCMv3.Web.Models.TokenAuth;
using HCMv3.Authorization.Impersonation;
using HCMv3.Authorization.Roles;
using HCMv3.Configuration;
using HCMv3.Identity;
using HCMv3.Net.Sms;
using HCMv3.Notifications;
using HCMv3.Security.Recaptcha;
using HCMv3.Web.Authentication.External;
using HCMv3.Web.Common;
using HCMv3.Authorization.Delegation;
using HCMv3.Authorization.Session;
using Abp.Runtime.Validation;
using Microsoft.AspNetCore.SignalR;
using HCMv3.Authorization.PasswordlessLogin;
using HCMv3.Authorization.QrLogin;

namespace HCMv3.Web.Controllers;

[Route("api/[controller]/[action]")]
public class TokenAuthController : HCMv3ControllerBase
{
    private readonly LogInManager _logInManager;
    private readonly ITenantCache _tenantCache;
    private readonly AbpLoginResultTypeHelper _abpLoginResultTypeHelper;
    private readonly TokenAuthConfiguration _configuration;
    private readonly UserManager _userManager;
    private readonly ICacheManager _cacheManager;
    private readonly IOptions<AsyncJwtBearerOptions> _jwtOptions;
    private readonly IExternalAuthConfiguration _externalAuthConfiguration;
    private readonly IExternalAuthManager _externalAuthManager;
    private readonly UserRegistrationManager _userRegistrationManager;
    private readonly IImpersonationManager _impersonationManager;
    private readonly IUserLinkManager _userLinkManager;
    private readonly IAppNotifier _appNotifier;
    private readonly ISmsSender _smsSender;
    private readonly IEmailSender _emailSender;
    private readonly IdentityOptions _identityOptions;
    private readonly GoogleAuthenticatorProvider _googleAuthenticatorProvider;
    private readonly ExternalLoginInfoManagerFactory _externalLoginInfoManagerFactory;
    private readonly ISettingManager _settingManager;
    private readonly IJwtSecurityStampHandler _securityStampHandler;
    private readonly AbpUserClaimsPrincipalFactory<User, Role> _claimsPrincipalFactory;
    private readonly IUserDelegationManager _userDelegationManager;
    private readonly IPasswordlessLoginManager _passwordlessLoginManager;
    private readonly IQrLoginManager _qrLoginManager;
    private readonly IUserSessionManager _userSessionManager;

    public IRecaptchaValidator RecaptchaValidator { get; set; }

    public TokenAuthController(
        LogInManager logInManager,
        ITenantCache tenantCache,
        AbpLoginResultTypeHelper abpLoginResultTypeHelper,
        TokenAuthConfiguration configuration,
        UserManager userManager,
        ICacheManager cacheManager,
        IOptions<AsyncJwtBearerOptions> jwtOptions,
        IExternalAuthConfiguration externalAuthConfiguration,
        IExternalAuthManager externalAuthManager,
        UserRegistrationManager userRegistrationManager,
        IImpersonationManager impersonationManager,
        IUserLinkManager userLinkManager,
        IAppNotifier appNotifier,
        ISmsSender smsSender,
        IEmailSender emailSender,
        IOptions<IdentityOptions> identityOptions,
        GoogleAuthenticatorProvider googleAuthenticatorProvider,
        ExternalLoginInfoManagerFactory externalLoginInfoManagerFactory,
        ISettingManager settingManager,
        IJwtSecurityStampHandler securityStampHandler,
        AbpUserClaimsPrincipalFactory<User, Role> claimsPrincipalFactory,
        IUserDelegationManager userDelegationManager,
        IPasswordlessLoginManager passwordlessLoginManager,
        IQrLoginManager qrLoginManager,
        SignInManager signInManager,
        IUserSessionManager userSessionManager)
    {
        _logInManager = logInManager;
        _tenantCache = tenantCache;
        _abpLoginResultTypeHelper = abpLoginResultTypeHelper;
        _configuration = configuration;
        _userManager = userManager;
        _cacheManager = cacheManager;
        _jwtOptions = jwtOptions;
        _externalAuthConfiguration = externalAuthConfiguration;
        _externalAuthManager = externalAuthManager;
        _userRegistrationManager = userRegistrationManager;
        _impersonationManager = impersonationManager;
        _userLinkManager = userLinkManager;
        _appNotifier = appNotifier;
        _smsSender = smsSender;
        _emailSender = emailSender;
        _googleAuthenticatorProvider = googleAuthenticatorProvider;
        _externalLoginInfoManagerFactory = externalLoginInfoManagerFactory;
        _settingManager = settingManager;
        _securityStampHandler = securityStampHandler;
        _identityOptions = identityOptions.Value;
        _claimsPrincipalFactory = claimsPrincipalFactory;
        RecaptchaValidator = NullRecaptchaValidator.Instance;
        _userDelegationManager = userDelegationManager;
        _passwordlessLoginManager = passwordlessLoginManager;
        _qrLoginManager = qrLoginManager;
        _userSessionManager = userSessionManager;
    }

    private async Task<string> EncryptQueryParameters(long userId, Tenant tenant, string passwordResetCode)
    {
        var expirationHours = await _settingManager.GetSettingValueAsync<int>(
            AppSettings.UserManagement.Password.PasswordResetCodeExpirationHours
        );

        var expireDate = Uri.EscapeDataString(Clock.Now.AddHours(expirationHours)
            .ToString(HCMv3Consts.DateTimeOffsetFormat));

        var query = $"userId={userId}&resetCode={passwordResetCode}&expireDate={expireDate}";

        if (tenant != null)
        {
            query += $"&tenantId={tenant.Id}";
        }

        return SimpleStringCipher.Instance.Encrypt(query);
    }


    [HttpPost]
    public async Task<AuthenticateResultModel> Authenticate([FromBody] AuthenticateModel model)
    {
        if (UseCaptchaOnLogin())
        {
            await ValidateReCaptcha(model.CaptchaResponse);
        }

        var loginResult = await GetLoginResultAsync(
            model.UserNameOrEmailAddress,
            model.Password,
            GetTenancyNameOrNull()
        );

        var returnUrl = model.ReturnUrl;

        if (model.SingleSignIn.HasValue && model.SingleSignIn.Value &&
            loginResult.Result == AbpLoginResultType.Success)
        {
            loginResult.User.SetSignInToken();
            returnUrl = AddSingleSignInParametersToReturnUrl(model.ReturnUrl, loginResult.User.SignInToken,
                loginResult.User.Id, loginResult.User.TenantId);
        }

        //Password reset
        if (loginResult.User.ShouldChangePasswordOnNextLogin)
        {
            loginResult.User.SetNewPasswordResetCode();
            return new AuthenticateResultModel
            {
                ShouldResetPassword = true,
                ReturnUrl = returnUrl,
                c = await EncryptQueryParameters(loginResult.User.Id, loginResult.Tenant, loginResult.User.PasswordResetCode)
            };
        }

        //Two factor auth
        await _userManager.InitializeOptionsAsync(loginResult.Tenant?.Id);

        string twoFactorRememberClientToken = null;
        if (await IsTwoFactorAuthRequiredAsync(loginResult, model))
        {
            if (model.TwoFactorVerificationCode.IsNullOrEmpty())
            {
                //Add a cache item which will be checked in SendTwoFactorAuthCode to prevent sending unwanted two factor code to users.
                await _cacheManager
                    .GetTwoFactorCodeCache()
                    .SetAsync(
                        loginResult.User.ToUserIdentifier().ToString(),
                        new TwoFactorCodeCacheItem()
                    );

                return new AuthenticateResultModel
                {
                    RequiresTwoFactorVerification = true,
                    UserId = loginResult.User.Id,
                    TwoFactorAuthProviders = await _userManager.GetValidTwoFactorProvidersAsync(loginResult.User),
                    ReturnUrl = returnUrl
                };
            }

            twoFactorRememberClientToken = await TwoFactorAuthenticateAsync(loginResult, model);
        }

        if (AllowOneConcurrentLoginPerUser())
        {
            await ResetSecurityStampForLoginResult(loginResult);
        }

        var sessionToken = await CreateUserSessionIfEnabledAsync(loginResult.User.Id, loginResult.User.TenantId);

        var refreshToken = CreateRefreshToken(
            await CreateJwtClaims(
                loginResult.Identity,
                loginResult.User,
                tokenType: TokenType.RefreshToken,
                sessionToken: sessionToken
            )
        );

        var accessToken = CreateAccessToken(
            await CreateJwtClaims(
                loginResult.Identity,
                loginResult.User,
                refreshTokenKey: refreshToken.key,
                sessionToken: sessionToken
            )
        );

        return new AuthenticateResultModel
        {
            AccessToken = accessToken,
            ExpireInSeconds = (int)_configuration.AccessTokenExpiration.TotalSeconds,
            RefreshToken = refreshToken.token,
            RefreshTokenExpireInSeconds = (int)_configuration.RefreshTokenExpiration.TotalSeconds,
            EncryptedAccessToken = GetEncryptedAccessToken(accessToken),
            TwoFactorRememberClientToken = twoFactorRememberClientToken,
            UserId = loginResult.User.Id,
            ReturnUrl = returnUrl
        };
    }

    [AbpAuthorize]
    [HttpPost]
    public async Task AuthenticateWithQrCode([FromBody] QrLoginAuthenticateModel model)
    {
        await _qrLoginManager.VerifySessionId(model.ConnectionId, model.SessionId);

        var user = await _userManager.GetUserByIdAsync(AbpSession.GetUserId());
        var loginResult = await _logInManager.CreateLoginResultAsync(user);

        var sessionToken = await CreateUserSessionIfEnabledAsync(loginResult.User.Id, loginResult.User.TenantId);

        var refreshToken = CreateRefreshToken(
            await CreateJwtClaims(
                loginResult.Identity,
                loginResult.User,
                tokenType: TokenType.RefreshToken,
                sessionToken: sessionToken
            )
        );

        var accessToken = CreateAccessToken(
            await CreateJwtClaims(
                loginResult.Identity,
                loginResult.User,
                refreshTokenKey: refreshToken.key,
                sessionToken: sessionToken
            )
        );

        await _qrLoginManager.RemoveQrLoginCache(model.ConnectionId);

        await _qrLoginManager.SendAuthData(model.ConnectionId, new QrLoginAuthenticateResultModel
        {
            AccessToken = accessToken,
            EncryptedAccessToken = GetEncryptedAccessToken(accessToken),
            ExpireInSeconds = (int)_configuration.AccessTokenExpiration.TotalSeconds,
            RefreshToken = refreshToken.token,
            RefreshTokenExpireInSeconds = (int)_configuration.RefreshTokenExpiration.TotalSeconds
        });
    }

    [HttpPost]
    public async Task<PasswordlessAuthenticateResultModel> PasswordlessAuthenticate([FromBody] PasswordlessAuthenticateModel model)
    {
        if (UseCaptchaOnLogin())
        {
            await ValidateReCaptcha(model.CaptchaResponse);
        }

        if (model.VerificationCode.IsNullOrEmpty())
        {
            return new PasswordlessAuthenticateResultModel();
        }

        await _passwordlessLoginManager.VerifyPasswordlessLoginCode(
            AbpSession.TenantId,
            model.ProviderValue,
            model.VerificationCode
        );

        var user = await _passwordlessLoginManager.GetUserByPasswordlessProviderAndKeyAsync(
            model.Provider,
            model.ProviderValue
        );

        var loginResult = await GetPasswordlessLoginResultAsync(user);

        var returnUrl = model.ReturnUrl;

        if (model.SingleSignIn.HasValue && model.SingleSignIn.Value &&
            loginResult.Result == AbpLoginResultType.Success)
        {
            loginResult.User.SetSignInToken();
            returnUrl = AddSingleSignInParametersToReturnUrl(model.ReturnUrl, loginResult.User.SignInToken,
                loginResult.User.Id, loginResult.User.TenantId);
        }

        var sessionToken = await CreateUserSessionIfEnabledAsync(loginResult.User.Id, loginResult.User.TenantId);

        var refreshToken = CreateRefreshToken(
            await CreateJwtClaims(
                loginResult.Identity,
                loginResult.User,
                tokenType: TokenType.RefreshToken,
                sessionToken: sessionToken
            )
        );

        var accessToken = CreateAccessToken(
            await CreateJwtClaims(
                loginResult.Identity,
                loginResult.User,
                refreshTokenKey: refreshToken.key,
                sessionToken: sessionToken
            )
        );

        await _passwordlessLoginManager.RemovePasswordlessLoginCode(
            AbpSession.TenantId,
            model.ProviderValue
        );

        return new PasswordlessAuthenticateResultModel
        {
            AccessToken = accessToken,
            ExpireInSeconds = (int)_configuration.AccessTokenExpiration.TotalSeconds,
            RefreshToken = refreshToken.token,
            RefreshTokenExpireInSeconds = (int)_configuration.RefreshTokenExpiration.TotalSeconds,
            EncryptedAccessToken = GetEncryptedAccessToken(accessToken),
            UserId = loginResult.User.Id,
            ReturnUrl = returnUrl
        };
    }

    [HttpPost]
    public async Task<RefreshTokenResult> RefreshToken(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new ArgumentNullException(nameof(refreshToken));
        }

        var (isRefreshTokenValid, principal) = await IsRefreshTokenValid(refreshToken);

        if (!isRefreshTokenValid)
        {
            throw new AbpValidationException("Refresh token is not valid!");
        }

        try
        {
            var user = await _userManager.GetUserAsync(
                UserIdentifier.Parse(principal.Claims.First(x => x.Type == AppConsts.UserIdentifier).Value)
            );

            if (AllowOneConcurrentLoginPerUser())
            {
                await _userManager.UpdateSecurityStampAsync(user);
                await _securityStampHandler.SetSecurityStampCacheItem(user.TenantId, user.Id, user.SecurityStamp);
            }

            // Propagate session token from refresh token claims
            var existingSessionToken = principal.Claims
                .FirstOrDefault(x => x.Type == AppConsts.SessionTokenKey)?.Value;

            principal = await _claimsPrincipalFactory.CreateAsync(user);

            var accessToken = CreateAccessToken(
                await CreateJwtClaims(principal.Identity as ClaimsIdentity, user, sessionToken: existingSessionToken)
            );

            return await Task.FromResult(new RefreshTokenResult(
                accessToken,
                GetEncryptedAccessToken(accessToken),
                (int)_configuration.AccessTokenExpiration.TotalSeconds)
            );
        }
        catch (UserFriendlyException)
        {
            throw;
        }
        catch (Exception e)
        {
            throw new AbpValidationException("Refresh token is not valid!" + e);
        }
    }

    private bool UseCaptchaOnLogin()
    {
        return SettingManager.GetSettingValue<bool>(AppSettings.UserManagement.UseCaptchaOnLogin);
    }

    [HttpGet]
    [AbpAuthorize]
    public async Task LogOut()
    {
        if (AbpSession.UserId != null)
        {
            var tokenValidityKeyInClaims = User.Claims.First(c => c.Type == AppConsts.TokenValidityKey);
            await RemoveTokenAsync(tokenValidityKeyInClaims.Value);

            var refreshTokenValidityKeyInClaims =
                User.Claims.FirstOrDefault(c => c.Type == AppConsts.RefreshTokenValidityKey);
            if (refreshTokenValidityKeyInClaims != null)
            {
                await RemoveTokenAsync(refreshTokenValidityKeyInClaims.Value);
            }

            // Invalidate user session record
            if (await _userSessionManager.IsSessionManagementEnabledAsync())
            {
                var sessionTokenClaim = User.Claims.FirstOrDefault(c => c.Type == AppConsts.SessionTokenKey);
                if (sessionTokenClaim != null)
                {
                    await _userSessionManager.InvalidateSessionAsync(sessionTokenClaim.Value);
                }
            }

            if (AllowOneConcurrentLoginPerUser())
            {
                await _securityStampHandler.RemoveSecurityStampCacheItem(
                    AbpSession.TenantId,
                    AbpSession.GetUserId()
                );
            }
        }
    }

    private async Task RemoveTokenAsync(string tokenKey)
    {
        await _userManager.RemoveTokenValidityKeyAsync(
            await _userManager.GetUserAsync(AbpSession.ToUserIdentifier()), tokenKey
        );

        await _cacheManager.GetCache(AppConsts.TokenValidityKey).RemoveAsync(tokenKey);
    }

    [HttpPost]
    public async Task SendTwoFactorAuthCode([FromBody] SendTwoFactorAuthCodeModel model)
    {
        var cacheKey = new UserIdentifier(AbpSession.TenantId, model.UserId).ToString();

        var cacheItem = await _cacheManager
            .GetTwoFactorCodeCache()
            .GetOrDefaultAsync(cacheKey);

        if (cacheItem == null)
        {
            //There should be a cache item added in Authenticate method! This check is needed to prevent sending unwanted two factor code to users.
            throw new UserFriendlyException(L("SendSecurityCodeErrorMessage"));
        }

        var user = await _userManager.FindByIdAsync(model.UserId.ToString());

        if (model.Provider != GoogleAuthenticatorProvider.Name)
        {
            cacheItem.Code = await _userManager.GenerateTwoFactorTokenAsync(user, model.Provider);
            var message = L("EmailSecurityCodeBody", cacheItem.Code);

            if (model.Provider == "Email")
            {
                await _emailSender.SendAsync(await _userManager.GetEmailAsync(user), L("EmailSecurityCodeSubject"),
                    message);
            }
            else if (model.Provider == "Phone")
            {
                await _smsSender.SendAsync(await _userManager.GetPhoneNumberAsync(user), message);
            }
        }

        await _cacheManager.GetTwoFactorCodeCache().SetAsync(
            cacheKey,
            cacheItem
        );

        await _cacheManager.GetCache("ProviderCache").SetAsync(
            "Provider",
            model.Provider
        );
    }

    [HttpPost]
    public async Task<ImpersonatedAuthenticateResultModel> ImpersonatedAuthenticate(string impersonationToken)
    {
        var result = await _impersonationManager.GetImpersonatedUserAndIdentity(impersonationToken);
        var sessionToken = await CreateUserSessionIfEnabledAsync(result.User.Id, result.User.TenantId);
        var accessToken = CreateAccessToken(await CreateJwtClaims(result.Identity, result.User, sessionToken: sessionToken));

        return new ImpersonatedAuthenticateResultModel
        {
            AccessToken = accessToken,
            EncryptedAccessToken = GetEncryptedAccessToken(accessToken),
            ExpireInSeconds = (int)_configuration.AccessTokenExpiration.TotalSeconds
        };
    }

    [HttpPost]
    public async Task<ImpersonatedAuthenticateResultModel> DelegatedImpersonatedAuthenticate(long userDelegationId,
        string impersonationToken)
    {
        var result = await _impersonationManager.GetImpersonatedUserAndIdentity(impersonationToken);
        var userDelegation = await _userDelegationManager.GetAsync(userDelegationId);

        if (!userDelegation.IsCreatedByUser(result.User.Id))
        {
            throw new UserFriendlyException("User delegation error...");
        }

        var sessionToken = await CreateUserSessionIfEnabledAsync(result.User.Id, result.User.TenantId);

        var expiration = userDelegation.EndTime.Subtract(Clock.Now);
        var accessToken = CreateAccessToken(await CreateJwtClaims(result.Identity, result.User, expiration, sessionToken: sessionToken),
            expiration);

        return new ImpersonatedAuthenticateResultModel
        {
            AccessToken = accessToken,
            EncryptedAccessToken = GetEncryptedAccessToken(accessToken),
            ExpireInSeconds = (int)expiration.TotalSeconds
        };
    }

    [HttpPost]
    public async Task<SwitchedAccountAuthenticateResultModel> LinkedAccountAuthenticate(string switchAccountToken)
    {
        var result = await _userLinkManager.GetSwitchedUserAndIdentity(switchAccountToken);
        var sessionToken = await CreateUserSessionIfEnabledAsync(result.User.Id, result.User.TenantId);
        var accessToken = CreateAccessToken(await CreateJwtClaims(result.Identity, result.User, sessionToken: sessionToken));

        return new SwitchedAccountAuthenticateResultModel
        {
            AccessToken = accessToken,
            EncryptedAccessToken = GetEncryptedAccessToken(accessToken),
            ExpireInSeconds = (int)_configuration.AccessTokenExpiration.TotalSeconds
        };
    }

    [HttpGet]
    public List<ExternalLoginProviderInfoModel> GetExternalAuthenticationProviders()
    {
        var allProviders = _externalAuthConfiguration.ExternalLoginInfoProviders
            .Select(infoProvider => infoProvider.GetExternalLoginInfo())
            .Where(IsSchemeEnabledOnTenant)
            .ToList();
        return ObjectMapper.Map<List<ExternalLoginProviderInfoModel>>(allProviders);
    }

    private bool IsSchemeEnabledOnTenant(ExternalLoginProviderInfo scheme)
    {
        if (!AbpSession.TenantId.HasValue)
        {
            return true;
        }

        switch (scheme.Name)
        {
            case "OpenIdConnect":
                return !_settingManager.GetSettingValueForTenant<bool>(
                    AppSettings.ExternalLoginProvider.Tenant.OpenIdConnect_IsDeactivated, AbpSession.GetTenantId());
            case "Microsoft":
                return !_settingManager.GetSettingValueForTenant<bool>(
                    AppSettings.ExternalLoginProvider.Tenant.Microsoft_IsDeactivated, AbpSession.GetTenantId());
            case "Google":
                return !_settingManager.GetSettingValueForTenant<bool>(
                    AppSettings.ExternalLoginProvider.Tenant.Google_IsDeactivated, AbpSession.GetTenantId());
            case "Twitter":
                return !_settingManager.GetSettingValueForTenant<bool>(
                    AppSettings.ExternalLoginProvider.Tenant.Twitter_IsDeactivated, AbpSession.GetTenantId());
            case "Facebook":
                return !_settingManager.GetSettingValueForTenant<bool>(
                    AppSettings.ExternalLoginProvider.Tenant.Facebook_IsDeactivated, AbpSession.GetTenantId());
            case "WsFederation":
                return !_settingManager.GetSettingValueForTenant<bool>(
                    AppSettings.ExternalLoginProvider.Tenant.WsFederation_IsDeactivated, AbpSession.GetTenantId());
            default: return true;
        }
    }

    [HttpPost]
    public async Task<ExternalAuthenticateResultModel> ExternalAuthenticate(
        [FromBody] ExternalAuthenticateModel model)
    {
        var externalUser = await GetExternalUserInfo(model);
        var shouldLockout = await IsUserLockoutEnabled();
        var loginResult = await _logInManager.LoginAsync(
            new UserLoginInfo(model.AuthProvider, externalUser.ProviderKey, model.AuthProvider),
            GetTenancyNameOrNull()
        );

        switch (loginResult.Result)
        {
            case AbpLoginResultType.Success:
                {
                    if (AllowOneConcurrentLoginPerUser())
                    {
                        await ResetSecurityStampForLoginResult(loginResult);
                    }

                    var sessionToken = await CreateUserSessionIfEnabledAsync(loginResult.User.Id, loginResult.User.TenantId);

                    var refreshToken = CreateRefreshToken(
                        await CreateJwtClaims(
                            loginResult.Identity,
                            loginResult.User,
                            tokenType: TokenType.RefreshToken,
                            sessionToken: sessionToken
                        )
                    );

                    var accessToken = CreateAccessToken(
                        await CreateJwtClaims(
                            loginResult.Identity,
                            loginResult.User,
                            refreshTokenKey: refreshToken.key,
                            sessionToken: sessionToken
                        )
                    );

                    var returnUrl = model.ReturnUrl;

                    if (model.SingleSignIn.HasValue && model.SingleSignIn.Value &&
                        loginResult.Result == AbpLoginResultType.Success)
                    {
                        loginResult.User.SetSignInToken();
                        returnUrl = AddSingleSignInParametersToReturnUrl(
                            model.ReturnUrl,
                            loginResult.User.SignInToken,
                            loginResult.User.Id,
                            loginResult.User.TenantId
                        );
                    }

                    return new ExternalAuthenticateResultModel
                    {
                        AccessToken = accessToken,
                        EncryptedAccessToken = GetEncryptedAccessToken(accessToken),
                        ExpireInSeconds = (int)_configuration.AccessTokenExpiration.TotalSeconds,
                        ReturnUrl = returnUrl,
                        RefreshToken = refreshToken.token,
                        RefreshTokenExpireInSeconds = (int)_configuration.RefreshTokenExpiration.TotalSeconds
                    };
                }
            case AbpLoginResultType.UnknownExternalLogin:
                {
                    var newUser = await RegisterExternalUserAsync(externalUser);
                    if (!newUser.IsActive)
                    {
                        return new ExternalAuthenticateResultModel
                        {
                            WaitingForActivation = true
                        };
                    }

                    //Try to login again with newly registered user!
                    loginResult = await _logInManager.LoginAsync(
                        new UserLoginInfo(model.AuthProvider, model.ProviderKey, model.AuthProvider),
                        GetTenancyNameOrNull()
                    );

                    if (loginResult.Result != AbpLoginResultType.Success)
                    {
                        throw _abpLoginResultTypeHelper.CreateExceptionForFailedLoginAttempt(
                            loginResult.Result,
                            externalUser.EmailAddress,
                            GetTenancyNameOrNull()
                        );
                    }

                    var newUserSessionToken = await CreateUserSessionIfEnabledAsync(loginResult.User.Id, loginResult.User.TenantId);

                    var refreshToken = CreateRefreshToken(await CreateJwtClaims(loginResult.Identity,
                        loginResult.User, tokenType: TokenType.RefreshToken, sessionToken: newUserSessionToken)
                    );

                    var accessToken = CreateAccessToken(await CreateJwtClaims(loginResult.Identity,
                        loginResult.User, refreshTokenKey: refreshToken.key, sessionToken: newUserSessionToken));

                    return new ExternalAuthenticateResultModel
                    {
                        AccessToken = accessToken,
                        EncryptedAccessToken = GetEncryptedAccessToken(accessToken),
                        ExpireInSeconds = (int)_configuration.AccessTokenExpiration.TotalSeconds,
                        RefreshToken = refreshToken.token,
                        RefreshTokenExpireInSeconds = (int)_configuration.RefreshTokenExpiration.TotalSeconds
                    };
                }
            default:
                {
                    throw _abpLoginResultTypeHelper.CreateExceptionForFailedLoginAttempt(
                        loginResult.Result,
                        externalUser.EmailAddress,
                        GetTenancyNameOrNull()
                    );
                }
        }
    }

    private async Task ResetSecurityStampForLoginResult(AbpLoginResult<Tenant, User> loginResult)
    {
        await _userManager.UpdateSecurityStampAsync(loginResult.User);
        await _securityStampHandler.SetSecurityStampCacheItem(loginResult.User.TenantId, loginResult.User.Id,
            loginResult.User.SecurityStamp);
        loginResult.Identity.ReplaceClaim(new Claim(AppConsts.SecurityStampKey, loginResult.User.SecurityStamp));
    }

    #region Etc

    [AbpMvcAuthorize]
    [HttpGet]
    public async Task<ActionResult> TestNotification(string message = "", string severity = "info")
    {
        if (message.IsNullOrEmpty())
        {
            message = "This is a test notification, created at " + Clock.Now;
        }

        await _appNotifier.SendMessageAsync(
            AbpSession.ToUserIdentifier(),
            message,
            severity.ToPascalCase().ToEnum<NotificationSeverity>()
        );

        return Content("Sent notification: " + message);
    }

    #endregion

    private async Task<User> RegisterExternalUserAsync(ExternalAuthUserInfo externalLoginInfo)
    {
        string username;

        using (var providerManager =
               _externalLoginInfoManagerFactory.GetExternalLoginInfoManager(externalLoginInfo.Provider))
        {
            username = providerManager.Object.GetUserNameFromExternalAuthUserInfo(externalLoginInfo);
        }

        var user = await _userRegistrationManager.RegisterAsync(
            externalLoginInfo.Name,
            externalLoginInfo.Surname,
            externalLoginInfo.EmailAddress,
            username,
            await _userManager.CreateRandomPassword(),
            true,
            null
        );

        user.Logins = new List<UserLogin>
            {
                new UserLogin
                {
                    LoginProvider = externalLoginInfo.Provider,
                    ProviderKey = externalLoginInfo.ProviderKey,
                    TenantId = user.TenantId
                }
            };

        await CurrentUnitOfWork.SaveChangesAsync();

        return user;
    }

    private async Task<ExternalAuthUserInfo> GetExternalUserInfo(ExternalAuthenticateModel model)
    {
        var userInfo = await _externalAuthManager.GetUserInfo(model.AuthProvider, model.ProviderAccessCode);
        if (!ProviderKeysAreEqual(model, userInfo))
        {
            throw new UserFriendlyException(L("CouldNotValidateExternalUser"));
        }

        return userInfo;
    }

    private bool ProviderKeysAreEqual(ExternalAuthenticateModel model, ExternalAuthUserInfo userInfo)
    {
        if (userInfo.ProviderKey == model.ProviderKey)
        {
            return true;
        }

        return userInfo.ProviderKey == model.ProviderKey.Replace("-", "").TrimStart('0');
    }

    private async Task<bool> IsTwoFactorAuthRequiredAsync(AbpLoginResult<Tenant, User> loginResult,
        AuthenticateModel authenticateModel)
    {
        if (!await SettingManager.GetSettingValueAsync<bool>(AbpZeroSettingNames.UserManagement.TwoFactorLogin
                .IsEnabled))
        {
            return false;
        }

        if (!loginResult.User.IsTwoFactorEnabled)
        {
            return false;
        }

        if ((await _userManager.GetValidTwoFactorProvidersAsync(loginResult.User)).Count <= 0)
        {
            return false;
        }

        if (await TwoFactorClientRememberedAsync(loginResult.User.ToUserIdentifier(), authenticateModel))
        {
            return false;
        }

        return true;
    }

    private async Task<bool> TwoFactorClientRememberedAsync(UserIdentifier userIdentifier,
        AuthenticateModel authenticateModel)
    {
        if (!await SettingManager.GetSettingValueAsync<bool>(
                AbpZeroSettingNames.UserManagement.TwoFactorLogin.IsRememberBrowserEnabled)
           )
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(authenticateModel.TwoFactorRememberClientToken))
        {
            return false;
        }

        try
        {
            var validationParameters = new TokenValidationParameters
            {
                ValidAudience = _configuration.Audience,
                ValidIssuer = _configuration.Issuer,
                IssuerSigningKey = _configuration.SecurityKey
            };

            foreach (var validator in _jwtOptions.Value.AsyncSecurityTokenValidators)
            {
                if (validator.CanReadToken(authenticateModel.TwoFactorRememberClientToken))
                {
                    try
                    {
                        var (principal, _) = await validator.ValidateTwoFactorRememberClientToken(
                            authenticateModel.TwoFactorRememberClientToken,
                            validationParameters
                        );

                        var userIdentifierClaim = principal.FindFirst(c => c.Type == AppConsts.UserIdentifier);
                        if (userIdentifierClaim == null)
                        {
                            return false;
                        }

                        return userIdentifierClaim.Value == userIdentifier.ToString();
                    }
                    catch (Exception ex)
                    {
                        Logger.Debug(ex.ToString(), ex);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Debug(ex.ToString(), ex);
        }

        return false;
    }

    /* Checkes two factor code and returns a token to remember the client (browser) if needed */
    private async Task<string> TwoFactorAuthenticateAsync(AbpLoginResult<Tenant, User> loginResult,
        AuthenticateModel authenticateModel)
    {
        var twoFactorCodeCache = _cacheManager.GetTwoFactorCodeCache();
        var userIdentifier = loginResult.User.ToUserIdentifier().ToString();
        var cachedCode = await twoFactorCodeCache.GetOrDefaultAsync(userIdentifier);
        var provider = _cacheManager.GetCache("ProviderCache").Get("Provider", cache => cache).ToString();

        if (provider == GoogleAuthenticatorProvider.Name)
        {
            if (!await _googleAuthenticatorProvider.ValidateAsync("TwoFactor",
                    authenticateModel.TwoFactorVerificationCode, _userManager, loginResult.User))
            {
                await SaveTwoFactorFailedLoginAttempt(loginResult.Tenant, loginResult.User);
                throw new UserFriendlyException(L("InvalidSecurityCode"));
            }
        }
        else if (cachedCode?.Code == null || cachedCode.Code != authenticateModel.TwoFactorVerificationCode)
        {
            await SaveTwoFactorFailedLoginAttempt(loginResult.Tenant, loginResult.User);
            throw new UserFriendlyException(L("InvalidSecurityCode"));
        }

        //Delete from the cache since it was a single usage code
        await twoFactorCodeCache.RemoveAsync(userIdentifier);

        if (authenticateModel.RememberClient)
        {
            if (await SettingManager.GetSettingValueAsync<bool>(AbpZeroSettingNames.UserManagement.TwoFactorLogin
                    .IsRememberBrowserEnabled))
            {
                return CreateAccessToken(
                    await CreateJwtClaims(
                        loginResult.Identity,
                        loginResult.User,
                        tokenType: TokenType.TwoFactorRememberClientToken
                    ),
                    AppConsts.TwoFactorRememberClientTokenExpiration 
                );
            }
        }

        await _logInManager.SaveLoginAttemptAsync(
            loginResult,
            GetTenancyNameOrNull(),
            loginResult.User.UserName
        );

        return null;
    }

    private async Task SaveTwoFactorFailedLoginAttempt(Tenant tenant, User user)
    {
        var loginResult = new AbpLoginResult<Tenant, User>(AbpLoginResultType.FailedForOtherReason, tenant, user);
        loginResult.SetFailReason(GetLocalizableString("TwoFactorCodeVerificationFailed"));

        await _logInManager.SaveLoginAttemptAsync(
            loginResult,
            GetTenancyNameOrNull(),
            user.UserName
        );
    }

    private static ILocalizableString GetLocalizableString(string name)
    {
        return new LocalizableString(name, HCMv3Consts.LocalizationSourceName);
    }

    private string GetTenancyNameOrNull()
    {
        if (!AbpSession.TenantId.HasValue)
        {
            return null;
        }

        return _tenantCache.GetOrNull(AbpSession.TenantId.Value)?.TenancyName;
    }

    private async Task<AbpLoginResult<Tenant, User>> GetLoginResultAsync(string usernameOrEmailAddress,
        string password, string tenancyName)
    {
        var shouldLockout = await IsUserLockoutEnabled();
        var loginResult = await _logInManager.LoginAsync(usernameOrEmailAddress, password, tenancyName, shouldLockout);

        switch (loginResult.Result)
        {
            case AbpLoginResultType.Success:
                return loginResult;
            default:
                throw _abpLoginResultTypeHelper.CreateExceptionForFailedLoginAttempt(
                    loginResult.Result,
                    usernameOrEmailAddress,
                    tenancyName
                );
        }
    }

    private async Task<AbpLoginResult<Tenant, User>> GetPasswordlessLoginResultAsync(User user)
    {
        var loginResult = await _logInManager.CreateLoginResultAsync(user);

        switch (loginResult.Result)
        {
            case AbpLoginResultType.Success:
                return loginResult;
            default:
                throw _abpLoginResultTypeHelper.CreateExceptionForFailedLoginAttempt(
                    loginResult.Result,
                    user.EmailAddress,
                    GetTenancyNameOrNull()
                );
        }
    }

    private string CreateAccessToken(IEnumerable<Claim> claims, TimeSpan? expiration = null)
    {
        return CreateToken(claims, expiration ?? _configuration.AccessTokenExpiration);
    }

    private (string token, string key) CreateRefreshToken(IEnumerable<Claim> claims)
    {
        var claimsList = claims.ToList();
        return (CreateToken(claimsList, AppConsts.RefreshTokenExpiration),
            claimsList.First(c => c.Type == AppConsts.TokenValidityKey).Value);
    }

    private string CreateToken(IEnumerable<Claim> claims, TimeSpan? expiration = null)
    {
        var now = DateTime.UtcNow;

        var jwtSecurityToken = new JwtSecurityToken(
            issuer: _configuration.Issuer,
            audience: _configuration.Audience,
            claims: claims,
            notBefore: now,
            signingCredentials: _configuration.SigningCredentials,
            expires: expiration == null ? (DateTime?)null : now.Add(expiration.Value)
        );

        return new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
    }

    private static string GetEncryptedAccessToken(string accessToken)
    {
        return SimpleStringCipher.Instance.Encrypt(accessToken, AppConsts.DefaultPassPhrase);
    }

    private async Task<IEnumerable<Claim>> CreateJwtClaims(
        ClaimsIdentity identity, User user,
        TimeSpan? expiration = null,
        TokenType tokenType = TokenType.AccessToken,
        string refreshTokenKey = null,
        string sessionToken = null)
    {
        var tokenValidityKey = Guid.NewGuid().ToString();
        var claims = identity.Claims.ToList();
        var nameIdClaim = claims.First(c => c.Type == _identityOptions.ClaimsIdentity.UserIdClaimType);

        if (_identityOptions.ClaimsIdentity.UserIdClaimType != JwtRegisteredClaimNames.Sub)
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Sub, nameIdClaim.Value));
        }

        claims.AddRange(new[]
        {
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.Now.ToUnixTimeSeconds().ToString(),
                    ClaimValueTypes.Integer64),
                new Claim(AppConsts.TokenValidityKey, tokenValidityKey),
                new Claim(AppConsts.UserIdentifier, user.ToUserIdentifier().ToUserIdentifierString()),
                new Claim(AppConsts.TokenType, tokenType.To<int>().ToString())
            });

        if (!string.IsNullOrEmpty(refreshTokenKey))
        {
            claims.Add(new Claim(AppConsts.RefreshTokenValidityKey, refreshTokenKey));
        }

        if (!string.IsNullOrEmpty(sessionToken))
        {
            claims.Add(new Claim(AppConsts.SessionTokenKey, sessionToken));
        }

        if (!expiration.HasValue)
        {
            expiration = GetTokenExpirationByTokenType(tokenType);
        }

        var expirationDate = DateTime.UtcNow.Add(expiration.Value);

        await _cacheManager
            .GetCache(AppConsts.TokenValidityKey)
            .SetAsync(tokenValidityKey, "", absoluteExpireTime: new DateTimeOffset(expirationDate));

        await _userManager.AddTokenValidityKeyAsync(
            user,
            tokenValidityKey,
            expirationDate
        );

        return claims;
    }

    private TimeSpan GetTokenExpirationByTokenType(TokenType tokenType)
    {
        if (tokenType == TokenType.AccessToken)
        {
            return _configuration.AccessTokenExpiration;
        }

        if (tokenType == TokenType.RefreshToken)
        {
            return _configuration.RefreshTokenExpiration;
        }

        if (tokenType == TokenType.TwoFactorRememberClientToken)
        {
            return AppConsts.TwoFactorRememberClientTokenExpiration;
        }
        
        throw new ArgumentOutOfRangeException(nameof(tokenType), tokenType, null);
    }

    private static string AddSingleSignInParametersToReturnUrl(string returnUrl, string signInToken, long userId,
        int? tenantId)
    {
        returnUrl += (returnUrl.Contains("?") ? "&" : "?") +
                     "accessToken=" + signInToken +
                     "&userId=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(userId.ToString()));
        if (tenantId.HasValue)
        {
            returnUrl += "&tenantId=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(tenantId.Value.ToString()));
        }

        return returnUrl;
    }


    private async Task<(bool isValid, ClaimsPrincipal principal)> IsRefreshTokenValid(string refreshToken)
    {
        ClaimsPrincipal principal = null;

        try
        {
            var validationParameters = new TokenValidationParameters
            {
                ValidAudience = _configuration.Audience,
                ValidIssuer = _configuration.Issuer,
                IssuerSigningKey = _configuration.SecurityKey
            };

            foreach (var validator in _jwtOptions.Value.AsyncSecurityTokenValidators)
            {
                if (!validator.CanReadToken(refreshToken))
                {
                    continue;
                }

                try
                {
                    (principal, _) = await validator.ValidateRefreshToken(refreshToken, validationParameters);
                    return (true, principal);
                }
                catch (Exception ex)
                {
                    Logger.Debug(ex.ToString(), ex);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Debug(ex.ToString(), ex);
        }

        return (false, principal);
    }


    private bool AllowOneConcurrentLoginPerUser()
    {
        return _settingManager.GetSettingValue<bool>(AppSettings.UserManagement.AllowOneConcurrentLoginPerUser);
    }

    private async Task ValidateReCaptcha(string captchaResponse)
    {
        var requestUserAgent = Request.Headers["User-Agent"].ToString();

        if (requestUserAgent.IsNullOrEmpty())
        {
            return;
        }

        if (WebConsts.ReCaptchaIgnoreWhiteList.Contains(requestUserAgent.Trim()))
        {
            return;
        }

        await RecaptchaValidator.ValidateAsync(captchaResponse);
    }
    private async Task<string> CreateUserSessionIfEnabledAsync(long userId, int? tenantId)
    {
        if (!await _userSessionManager.IsSessionManagementEnabledAsync())
        {
            return null;
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers["User-Agent"].ToString();
        var sessionToken = await _userSessionManager.CreateSessionAsync(userId, tenantId, ipAddress, userAgent);

        if (AllowOneConcurrentLoginPerUser())
        {
            await _userSessionManager.InvalidateAllSessionsAsync(userId, tenantId, sessionToken);
        }

        return sessionToken;
    }

    private async Task<bool> IsUserLockoutEnabled()
    {
        return await SettingManager.GetSettingValueAsync<bool>(AbpZeroSettingNames.UserManagement.UserLockOut.IsEnabled);
    }
}

public class QrLoginAuthenticateModel
{
    public string ConnectionId { get; set; }

    public string SessionId { get; set; }
}