using Abp;
using Abp.Authorization;
using Abp.Configuration;
using Abp.Extensions;
using Abp.Runtime.Caching;
using Abp.Runtime.Security;
using Abp.Runtime.Session;
using Abp.Timing;
using Abp.UI;
using Abp.Zero.Configuration;
using Microsoft.AspNetCore.RateLimiting;
using HCMv3.Authentication.PasswordlessLogin;
using HCMv3.Authorization.Accounts.Dto;
using HCMv3.Authorization.Delegation;
using HCMv3.Authorization.Impersonation;
using HCMv3.Authorization.PasswordlessLogin;
using HCMv3.Authorization.Users;
using HCMv3.Configuration;
using HCMv3.MultiTenancy;
using HCMv3.Net.Sms;
using HCMv3.Security.Recaptcha;
using HCMv3.Url;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace HCMv3.Authorization.Accounts;

public class AccountAppService : HCMv3AppServiceBase, IAccountAppService
{
    public IAppUrlService AppUrlService { get; set; }

    public IRecaptchaValidator RecaptchaValidator { get; set; }

    private readonly IUserEmailer _userEmailer;
    private readonly UserRegistrationManager _userRegistrationManager;
    private readonly IImpersonationManager _impersonationManager;
    private readonly IUserLinkManager _userLinkManager;
    private readonly IWebUrlService _webUrlService;
    private readonly IUserDelegationManager _userDelegationManager;
    private readonly ISmsSender _smsSender;
    private readonly ICacheManager _cacheManager;
    private readonly IPasswordlessLoginManager _passwordlessLoginManager;

    public AccountAppService(
        IUserEmailer userEmailer,
        UserRegistrationManager userRegistrationManager,
        IImpersonationManager impersonationManager,
        IUserLinkManager userLinkManager,
        IWebUrlService webUrlService,
        IUserDelegationManager userDelegationManager,
        ISmsSender smsSender,
        ICacheManager cacheManager,
        IPasswordlessLoginManager passwordlessLoginManager)
    {
        _userEmailer = userEmailer;
        _userRegistrationManager = userRegistrationManager;
        _impersonationManager = impersonationManager;
        _userLinkManager = userLinkManager;
        _webUrlService = webUrlService;
        _smsSender = smsSender;

        AppUrlService = NullAppUrlService.Instance;
        RecaptchaValidator = NullRecaptchaValidator.Instance;
        _userDelegationManager = userDelegationManager;
        _cacheManager = cacheManager;
        _passwordlessLoginManager = passwordlessLoginManager;
    }

    public async Task<IsTenantAvailableOutput> IsTenantAvailable(IsTenantAvailableInput input)
    {
        var tenant = await TenantManager.FindByTenancyNameAsync(input.TenancyName);
        if (tenant == null)
        {
            return new IsTenantAvailableOutput(TenantAvailabilityState.NotFound);
        }

        if (!tenant.IsActive)
        {
            return new IsTenantAvailableOutput(TenantAvailabilityState.InActive);
        }

        return new IsTenantAvailableOutput(TenantAvailabilityState.Available, tenant.Id,
            _webUrlService.GetServerRootAddress(input.TenancyName));
    }

    public Task<int?> ResolveTenantId(ResolveTenantIdInput input)
    {
        if (string.IsNullOrEmpty(input.c))
        {
            return Task.FromResult(AbpSession.TenantId);
        }

        var parameters = SimpleStringCipher.Instance.Decrypt(input.c);
        var query = HttpUtility.ParseQueryString(parameters);

        if (query["tenantId"] == null)
        {
            return Task.FromResult<int?>(null);
        }

        var tenantId = Convert.ToInt32(query["tenantId"]) as int?;
        return Task.FromResult(tenantId);
    }

    public async Task<RegisterOutput> Register(RegisterInput input)
    {
        if (UseCaptchaOnRegistration())
        {
            await RecaptchaValidator.ValidateAsync(input.CaptchaResponse);
        }

        var user = await _userRegistrationManager.RegisterAsync(
            input.Name,
            input.Surname,
            input.EmailAddress,
            input.UserName,
            input.Password,
            false,
            AppUrlService.CreateEmailActivationUrlFormat(AbpSession.TenantId)
        );

        var isEmailConfirmationRequiredForLogin =
            await SettingManager.GetSettingValueAsync<bool>(AbpZeroSettingNames.UserManagement
                .IsEmailConfirmationRequiredForLogin);

        return new RegisterOutput
        {
            CanLogin = user.IsActive && (user.IsEmailConfirmed || !isEmailConfirmationRequiredForLogin)
        };
    }

    public async Task SendPasswordResetCode(SendPasswordResetCodeInput input)
    {
        var user = await UserManager.FindByEmailAsync(input.EmailAddress);
        if (user == null)
        {
            await Task.Delay(
                new Random(DateTime.Now.Millisecond)
                    .Next(2000,
                        5000)); // delay a random duration between 2 and 5 seconds to simulate sending an email
            return;
        }

        user.SetNewPasswordResetCode();
        await _userEmailer.SendPasswordResetLinkAsync(
            user,
            AppUrlService.CreatePasswordResetUrlFormat(AbpSession.TenantId)
        );
    }

    public async Task<ResetPasswordOutput> ResetPassword(ResetPasswordInput input)
    {
        if (input.ExpireDate < Clock.Now)
        {
            throw new UserFriendlyException(L("PasswordResetLinkExpired"));
        }

        var user = await UserManager.GetUserByIdAsync(input.UserId);
        if (user == null || user.PasswordResetCode.IsNullOrEmpty() || user.PasswordResetCode != input.ResetCode)
        {
            throw new UserFriendlyException(L("InvalidPasswordResetCode"), L("InvalidPasswordResetCode_Detail"));
        }

        await UserManager.InitializeOptionsAsync(AbpSession.TenantId);
        CheckErrors(await UserManager.ChangePasswordAsync(user, input.Password));
        user.PasswordResetCode = null;
        user.IsEmailConfirmed = true;
        user.ShouldChangePasswordOnNextLogin = false;

        await UserManager.UpdateAsync(user);

        return new ResetPasswordOutput
        {
            CanLogin = user.IsActive,
            UserName = user.UserName
        };
    }

    public async Task SendEmailActivationLink(SendEmailActivationLinkInput input)
    {
        if (UseCaptchaOnEmailActivation())
        {
            await RecaptchaValidator.ValidateAsync(input.CaptchaResponse);
        }

        var user = await UserManager.FindByEmailAsync(input.EmailAddress);

        if (user == null)
        {
            throw new AbpException(L("UserNotFoundForEmail"));
        }

        await SendEmailActivationLinkInternal(user);
    }

    public async Task SendPasswordlessLoginCode(SendPasswordlessLoginCodeInput input)
    {
        if (input.ProviderType == PasswordlessLoginProviderType.Email)
        {
            await SendEmailPasswordlessCode(input.ProviderValue);
        }
        else if (input.ProviderType == PasswordlessLoginProviderType.Sms)
        {
            await SendSmsPasswordlessCode(input.ProviderValue);
        }
    }

    private async Task SendEmailPasswordlessCode(string emailAddress)
    {
        var user = await UserManager.FindByEmailAsync(emailAddress);
        if (user == null)
        {
            return;
        }

        var code = await _passwordlessLoginManager.GeneratePasswordlessLoginCode(
            AbpSession.TenantId,
            emailAddress
        );

        await _userEmailer.SendPasswordlessLoginCodeAsync(user, code);
    }

    private async Task SendSmsPasswordlessCode(string phoneNumber)
    {
        if (phoneNumber.IsNullOrEmpty())
        {
            return;
        }

        var user = UserManager.Users.FirstOrDefault(u => u.PhoneNumber == phoneNumber);
        if (user == null)
        {
            return;
        }

        var code = await _passwordlessLoginManager.GeneratePasswordlessLoginCode(
            AbpSession.TenantId,
            phoneNumber
        );

        var message = string.Format(L("PasswordlessLogin_SmsMessage", HCMv3Consts.ProductName, code));
        await _smsSender.SendAsync(phoneNumber, message);
    }

    [EnableRateLimiting("PasswordlessLoginLimiter")]
    public async Task VerifyPasswordlessLoginCode(VerifyPasswordlessLoginCodeInput input)
    {
        await _passwordlessLoginManager.VerifyPasswordlessLoginCode(
            AbpSession.TenantId,
            input.ProviderValue,
            input.Code
        );
    }

    public async Task ActivateEmail(ActivateEmailInput input)
    {
        var user = await UserManager.FindByIdAsync(input.UserId.ToString());
        if (user != null && user.IsEmailConfirmed)
        {
            return;
        }

        if (user == null || user.EmailConfirmationCode.IsNullOrEmpty() ||
            user.EmailConfirmationCode != input.ConfirmationCode)
        {
            throw new UserFriendlyException(L("InvalidEmailConfirmationCode"),
                L("InvalidEmailConfirmationCode_Detail"));
        }

        user.IsEmailConfirmed = true;
        user.EmailConfirmationCode = null;

        await UserManager.UpdateAsync(user);
    }

    public async Task ChangeEmail(ChangeEmailInput input)
    {
        var user = await UserManager.FindByIdAsync(input.UserId.ToString());

        if (user == null)
        {
            throw new AbpException(L("UserNotFound"));
        }

        if (user.EmailAddress != input.OldEmailAddress)
        {
            throw new UserFriendlyException(L("EmailAddressesDidNotMatch"));
        }

        user.EmailAddress = input.EmailAddress;
        user.IsEmailConfirmed = false;

        // May user don't have access new email address. So, we need to reset email confirmation code.
        await SendEmailActivationLinkInternal(user);

        await UserManager.UpdateAsync(user);
    }

    [AbpAuthorize(AppPermissions.Pages_Administration_Users_Impersonation)]
    public virtual async Task<ImpersonateOutput> ImpersonateUser(ImpersonateUserInput input)
    {
        return new ImpersonateOutput
        {
            ImpersonationToken =
                await _impersonationManager.GetImpersonationToken(input.UserId, AbpSession.TenantId),
            TenancyName = await GetTenancyNameOrNullAsync(input.TenantId)
        };
    }

    [AbpAuthorize(AppPermissions.Pages_Tenants_Impersonation)]
    public virtual async Task<ImpersonateOutput> ImpersonateTenant(ImpersonateTenantInput input)
    {
        return new ImpersonateOutput
        {
            ImpersonationToken = await _impersonationManager.GetImpersonationToken(input.UserId, input.TenantId),
            TenancyName = await GetTenancyNameOrNullAsync(input.TenantId)
        };
    }

    public virtual async Task<ImpersonateOutput> DelegatedImpersonate(DelegatedImpersonateInput input)
    {
        var userDelegation = await _userDelegationManager.GetAsync(input.UserDelegationId);
        if (userDelegation.TargetUserId != AbpSession.GetUserId())
        {
            throw new UserFriendlyException("User delegation error.");
        }

        return new ImpersonateOutput
        {
            ImpersonationToken =
                await _impersonationManager.GetImpersonationToken(userDelegation.SourceUserId,
                    userDelegation.TenantId),
            TenancyName = await GetTenancyNameOrNullAsync(userDelegation.TenantId)
        };
    }

    public virtual async Task<ImpersonateOutput> BackToImpersonator()
    {
        return new ImpersonateOutput
        {
            ImpersonationToken = await _impersonationManager.GetBackToImpersonatorToken(),
            TenancyName = await GetTenancyNameOrNullAsync(AbpSession.ImpersonatorTenantId)
        };
    }

    public virtual async Task<SwitchToLinkedAccountOutput> SwitchToLinkedAccount(SwitchToLinkedAccountInput input)
    {
        if (!await _userLinkManager.AreUsersLinked(AbpSession.ToUserIdentifier(), input.ToUserIdentifier()))
        {
            throw new Exception(L("This account is not linked to your account"));
        }

        return new SwitchToLinkedAccountOutput
        {
            SwitchAccountToken =
                await _userLinkManager.GetAccountSwitchToken(input.TargetUserId, input.TargetTenantId),
            TenancyName = await GetTenancyNameOrNullAsync(input.TargetTenantId)
        };
    }

    private bool UseCaptchaOnRegistration()
    {
        return SettingManager.GetSettingValue<bool>(AppSettings.UserManagement.UseCaptchaOnRegistration);
    }

    private bool UseCaptchaOnEmailActivation()
    {
        return SettingManager.GetSettingValue<bool>(AppSettings.TenantManagement.UseCaptchaOnEmailActivation);
    }

    private async Task<Tenant> GetActiveTenantAsync(int tenantId)
    {
        var tenant = await TenantManager.FindByIdAsync(tenantId);
        if (tenant == null)
        {
            throw new UserFriendlyException(L("UnknownTenantId{0}", tenantId));
        }

        if (!tenant.IsActive)
        {
            throw new UserFriendlyException(L("TenantIdIsNotActive{0}", tenantId));
        }

        return tenant;
    }

    private async Task<string> GetTenancyNameOrNullAsync(int? tenantId)
    {
        return tenantId.HasValue ? (await GetActiveTenantAsync(tenantId.Value)).TenancyName : null;
    }

    private async Task SendEmailActivationLinkInternal(User user)
    {
        user.SetNewEmailConfirmationCode();
        await _userEmailer.SendEmailActivationLinkAsync(
            user,
            AppUrlService.CreateEmailActivationUrlFormat(AbpSession.TenantId)
        );
    }
}
