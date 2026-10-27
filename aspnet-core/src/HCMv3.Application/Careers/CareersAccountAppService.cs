using System;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Configuration;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.MultiTenancy;
using Abp.Timing;
using Abp.UI;
using Abp.Zero.Configuration;
using HCMv3.Authorization.Roles;
using HCMv3.Authorization.Users;
using HCMv3.Careers.Dto;
using HCMv3.Configuration;
using HCMv3.Recruitment;
using HCMv3.Security.Recaptcha;

namespace HCMv3.Careers;

/// <summary>
/// PUBLIC (anonymous) candidate-identity surface of the careers portal -- wave R2. Replaces legacy
/// <c>HCM10.Recruiting.Controllers.AccountController</c>.
///
/// WHAT WAS REPLACED AND WHY (legacy line references are to HCM10.Recruiting):
/// * <c>AccountController.Login</c> (<c>AccountController.cs:105-106</c>, <c>:168-169</c>) authenticated with
///   <c>a.UserName == form &amp;&amp; a.Password == form</c> -- a plaintext string comparison against
///   <c>Applicant.Password</c>. Gone: candidates are ABP users and sign in through the stock
///   <c>/api/TokenAuth/Authenticate</c>. There is no login method on this service on purpose.
/// * <c>AccountController.ForgotPasswordRecovery</c> (<c>AccountController.cs:443-445</c>) mailed the stored
///   plaintext password back to the candidate. Gone: replaced by ASP.NET Zero's single-use, expiring reset
///   code (<see cref="SendPasswordResetCode"/> / <see cref="ResetPassword"/>).
/// * <c>VacancyController.QuickApplyForJob</c> (<c>VacancyController.cs:685</c>) minted a password as
///   <c>FirstName[0] + "@1234"</c> and mailed it in cleartext (<c>:779</c>). Gone: no server-generated
///   passwords anywhere; the candidate chooses one and it is hashed by ASP.NET Identity.
/// * <c>AccountController.IsUserNameExist</c> (<c>AccountController.cs:45-67</c>) was an anonymous endpoint
///   that answered "does this account exist?" for any address. Deliberately NOT ported -- it is an account
///   enumeration oracle.
///
/// SECURITY MODEL -- identical stance to <see cref="CareersAppService"/>:
/// 1. Anonymous request =&gt; <c>AbpSession.TenantId</c> is <c>null</c> =&gt; ABP treats the caller as HOST and
///    DISABLES the <c>MustHaveTenant</c> filter. So the tenant is resolved explicitly from the subdomain via
///    <see cref="ICareersTenantResolver"/> and every method fails closed when it cannot be resolved. An
///    unresolvable subdomain must never create a host user.
/// 2. Once resolved, the tenant is pushed onto BOTH <c>CurrentUnitOfWork</c> (data filters) and
///    <c>AbpSession</c> (because <see cref="UserRegistrationManager"/> and <c>UserManager</c> read
///    <c>AbpSession.TenantId</c> to decide which tenant the new user belongs to --
///    <c>UserRegistrationManager.CheckForTenant</c> throws outright on a null session tenant, which is the
///    backstop behind guarantee 1).
/// 3. Settings are always read with the explicit <c>...ForTenantAsync(tenantId, ...)</c> overloads rather
///    than the ambient ones, so a mistake in (2) cannot silently fall back to host-level settings.
/// </summary>
[AbpAllowAnonymous]
public class CareersAccountAppService : HCMv3AppServiceBase, ICareersAccountAppService
{
    /// <summary>
    /// Legacy <c>QuickApplyForJob</c> used this exact sentinel when it had no birth date for a candidate
    /// (<c>VacancyController.cs:671</c>). <see cref="Applicant.BirthDate"/> is non-nullable, so the sentinel
    /// is kept rather than inventing <c>DateTime.MinValue</c> (which SQL Server's <c>datetime</c> cannot
    /// store) or silently defaulting to today.
    /// </summary>
    public static readonly DateTime UnknownBirthDate = new DateTime(1950, 1, 1);

    /// <summary>
    /// Property-injected, like <c>AccountAppService</c>: the real implementation lives in the Web layer
    /// (<c>HCMv3.Web.Security.Recaptcha.RecaptchaValidator</c>) which the Application layer must not depend
    /// on. Defaults to <see cref="NullRecaptchaValidator"/>.
    ///
    /// NOTE: a null validator means "captcha not enforced". That is safe here only because the captcha check
    /// is gated on a setting that defaults to TRUE and the whole endpoint is additionally gated on
    /// <see cref="AppSettings.Careers.AllowCandidateSelfRegistration"/>, which defaults to FALSE.
    /// </summary>
    public IRecaptchaValidator RecaptchaValidator { get; set; }

    private readonly UserRegistrationManager _userRegistrationManager;
    private readonly IUserEmailer _userEmailer;
    private readonly IRepository<Applicant> _applicantRepository;
    private readonly ICareersTenantResolver _tenantResolver;
    private readonly ICareersUrlService _careersUrlService;
    private readonly ITenantCache _tenantCache;

    public CareersAccountAppService(
        UserRegistrationManager userRegistrationManager,
        IUserEmailer userEmailer,
        IRepository<Applicant> applicantRepository,
        ICareersTenantResolver tenantResolver,
        ICareersUrlService careersUrlService,
        ITenantCache tenantCache)
    {
        _userRegistrationManager = userRegistrationManager;
        _userEmailer = userEmailer;
        _applicantRepository = applicantRepository;
        _tenantResolver = tenantResolver;
        _careersUrlService = careersUrlService;
        _tenantCache = tenantCache;

        RecaptchaValidator = NullRecaptchaValidator.Instance;
    }

    public async Task<CareersRegisterOutput> Register(CareersRegisterInput input)
    {
        var tenantId = await ResolveTenantIdAsync();

        using (CurrentUnitOfWork?.SetTenantId(tenantId))
        using (AbpSession.Use(tenantId, null))
        {
            // Gate 1 (careers-specific, default FALSE). A tenant that has not deliberately opened its
            // careers portal cannot have ABP users created on it by unauthenticated internet traffic.
            var allowCandidateRegistration = await SettingManager.GetSettingValueForTenantAsync<bool>(
                AppSettings.Careers.AllowCandidateSelfRegistration, tenantId);

            if (!allowCandidateRegistration)
            {
                throw new UserFriendlyException(L("CareersRegistrationIsDisabled"));
            }

            // Gate 2 (ABP's own AllowSelfRegistration) is enforced inside RegisterAsync below.

            var useCaptcha = await SettingManager.GetSettingValueForTenantAsync<bool>(
                AppSettings.UserManagement.UseCaptchaOnRegistration, tenantId);

            if (useCaptcha)
            {
                await RecaptchaValidator.ValidateAsync(input.CaptchaResponse);
            }

            var tenancyName = _tenantCache.GetOrNull(tenantId)?.TenancyName;

            // Fail closed when no careers activation link can be built (i.e. App:CareersDomainFormat is not
            // configured -- its state today). Two reasons, and the first is a live defect in the stock code:
            //
            // (a) UserRegistrationManager.RegisterAsync always calls
            //     IUserEmailer.SendEmailActivationLinkAsync when isEmailConfirmed is false, and
            //     UserEmailer.SendEmailActivationLinkAsync dereferences the link unguarded --
            //     `link = link.Replace("{userId}", ...)` at UserEmailer.cs:85. A null link there is a
            //     NullReferenceException (HTTP 500) AFTER the user row has already been committed by
            //     RegisterAsync's SaveChangesAsync. Note SendPasswordResetLinkAsync DOES guard this
            //     (UserEmailer.cs:161) -- the activation path just does not.
            // (b) Even with that fixed, registering a candidate who can never receive an activation link
            //     produces an account they cannot confirm and we cannot mail. Refusing is the honest answer.
            var emailActivationUrlFormat = _careersUrlService.CreateEmailActivationUrlFormatOrNull(tenancyName);
            if (emailActivationUrlFormat == null)
            {
                throw new UserFriendlyException(L("CareersRegistrationIsDisabled"));
            }

            // Reused wholesale from ASP.NET Zero: max-user-count policy, restricted email domain,
            // AllowSelfRegistration, IsNewRegisteredUserActiveByDefault, password policy + hashing via
            // UserManager.CreateAsync, email confirmation code, notification subscriptions.
            // The email address doubles as the user name, matching legacy AccountController.cs:290-292.
            var user = await _userRegistrationManager.RegisterAsync(
                input.FirstNameEn,
                input.LastNameEn,
                input.EmailAddress,
                input.EmailAddress,
                input.Password,
                false,
                emailActivationUrlFormat);

            // RegisterAsync assigns every role flagged IsDefault -- normally the tenant "User" role, which a
            // tenant admin may well have granted staff permissions to. A candidate must not inherit those,
            // so the role set is OVERWRITTEN (not appended to) with exactly the permissionless Candidate
            // role. Strip, never add.
            CheckErrors(await UserManager.SetRolesAsync(user, new[] { StaticRoleNames.Tenants.Candidate }));

            await CreateApplicantForAsync(user, input, tenantId);

            var isEmailConfirmationRequiredForLogin = await SettingManager.GetSettingValueForTenantAsync<bool>(
                AbpZeroSettingNames.UserManagement.IsEmailConfirmationRequiredForLogin, tenantId);

            return new CareersRegisterOutput
            {
                CanLogin = user.IsActive && (user.IsEmailConfirmed || !isEmailConfirmationRequiredForLogin)
            };
        }
    }

    /// <summary>
    /// ACCOUNT-ENUMERATION HANDLING. This method returns <c>void</c> and takes the same code path shape for
    /// every outcome that is reachable once the subdomain resolves:
    /// * address belongs to a candidate of this tenant -&gt; reset code set, email queued;
    /// * address unknown, or belongs to a user of ANOTHER tenant, or belongs to a non-candidate (staff)
    ///   user of this tenant -&gt; nothing happens;
    /// and in all of those cases the caller gets the identical empty 200. No <c>UserNotFoundForEmail</c>
    /// exception (which is what ASP.NET Zero's <c>AccountAppService.SendEmailActivationLink</c> throws and
    /// what makes THAT endpoint an oracle), no differing message, no differing status code.
    ///
    /// The one response an attacker can still distinguish is the fail-closed "portal not available" thrown
    /// when the SUBDOMAIN cannot be resolved. That leaks whether a careers subdomain exists, not whether an
    /// account does, and it is the same signal R1's read endpoints already give.
    ///
    /// TIMING: deliberately NOT mirrored from ASP.NET Zero's <c>AccountAppService.SendPasswordResetCode</c>,
    /// which sleeps a random 2-5s on the miss path only (<c>AccountAppService.cs:139-143</c>). That makes
    /// the HIT path the fast one and so remains distinguishable -- it inverts the signal rather than
    /// removing it. Rather than ship a delay that reads as protection but is not, no artificial delay is
    /// added here. Genuinely closing the timing channel needs the email to be handed to a background job so
    /// both paths return immediately; that is called out in this wave's report, not silently half-done.
    /// </summary>
    public async Task SendPasswordResetCode(CareersSendPasswordResetCodeInput input)
    {
        var tenantId = await ResolveTenantIdAsync();

        using (CurrentUnitOfWork?.SetTenantId(tenantId))
        using (AbpSession.Use(tenantId, null))
        {
            var useCaptcha = await SettingManager.GetSettingValueForTenantAsync<bool>(
                AppSettings.UserManagement.UseCaptchaOnResetPassword, tenantId);

            if (useCaptcha)
            {
                await RecaptchaValidator.ValidateAsync(input.CaptchaResponse);
            }

            // Scoped by the UoW tenant filter set above, so this can only ever find a user of the resolved
            // tenant even though the ambient session started out as host.
            var user = await UserManager.FindByEmailAsync(input.EmailAddress);
            if (user == null || user.TenantId != tenantId)
            {
                return;
            }

            // Staff accounts do not get password-reset mail triggered from the public careers surface. Their
            // reset flow is the admin app's. Restricting this keeps the anonymous careers endpoint from
            // being usable to spray reset mail at a tenant's employees.
            if (!await UserManager.IsInRoleAsync(user, StaticRoleNames.Tenants.Candidate))
            {
                return;
            }

            user.SetNewPasswordResetCode();

            var tenancyName = _tenantCache.GetOrNull(tenantId)?.TenancyName;
            await _userEmailer.SendPasswordResetLinkAsync(
                user,
                _careersUrlService.CreatePasswordResetUrlFormatOrNull(tenancyName));
        }
    }

    public async Task<CareersResetPasswordOutput> ResetPassword(CareersResetPasswordInput input)
    {
        var tenantId = await ResolveTenantIdAsync();

        using (CurrentUnitOfWork?.SetTenantId(tenantId))
        using (AbpSession.Use(tenantId, null))
        {
            if (input.ExpireDate < Clock.Now)
            {
                throw new UserFriendlyException(L("PasswordResetLinkExpired"));
            }

            var user = await UserManager.FindByIdAsync(input.UserId.ToString());

            // A single generic failure for "no such user", "wrong tenant", "not a candidate" and "bad code".
            // Distinguishing them would turn a leaked/guessed reset link into a probe for which user ids
            // exist on which tenant.
            if (user == null ||
                user.TenantId != tenantId ||
                user.PasswordResetCode.IsNullOrEmpty() ||
                user.PasswordResetCode != input.ResetCode ||
                !await UserManager.IsInRoleAsync(user, StaticRoleNames.Tenants.Candidate))
            {
                throw new UserFriendlyException(L("InvalidPasswordResetCode"), L("InvalidPasswordResetCode_Detail"));
            }

            await UserManager.InitializeOptionsAsync(tenantId);
            CheckErrors(await UserManager.ChangePasswordAsync(user, input.Password));

            user.PasswordResetCode = null;
            user.IsEmailConfirmed = true;
            user.ShouldChangePasswordOnNextLogin = false;

            await UserManager.UpdateAsync(user);

            return new CareersResetPasswordOutput
            {
                CanLogin = user.IsActive,
                UserName = user.UserName
            };
        }
    }

    /// <summary>
    /// Creates the <see cref="Applicant"/> profile row for a newly registered candidate and links it to the
    /// ABP user.
    ///
    /// DELIBERATELY DOES NOT ADOPT AN EXISTING APPLICANT ROW WITH THE SAME EMAIL. Legacy looked the
    /// applicant up by <c>UserName</c> first and reused it (<c>AccountController.cs:235-238</c>,
    /// <c>VacancyController.cs:657-661</c>). Replaying that on an anonymous endpoint would mean: anyone who
    /// knows the email address on a recruiter-created applicant record can register with it and inherit that
    /// person's profile, CVs and application history -- without ever proving control of the mailbox when the
    /// tenant has email confirmation switched off. Whether recruiter-created applicants should be claimable
    /// (and under what proof) is a product decision, so this wave takes the option that cannot be abused and
    /// leaves the merge question open. Consequence to be aware of: a tenant can end up with two Applicant
    /// rows for one person.
    /// </summary>
    private async Task CreateApplicantForAsync(User user, CareersRegisterInput input, int tenantId)
    {
        var applicant = new Applicant
        {
            TenantId = tenantId,
            UserId = user.Id,
            Email = input.EmailAddress,
            FirstNameEn = input.FirstNameEn,
            LastNameEn = input.LastNameEn,
            FirstNameAr = input.FirstNameAr,
            LastNameAr = input.LastNameAr,
            BirthDate = UnknownBirthDate
        };

        await _applicantRepository.InsertAsync(applicant);
    }

    private async Task<int> ResolveTenantIdAsync()
    {
        var tenantId = await _tenantResolver.ResolveTenantIdOrNullAsync();
        if (tenantId == null)
        {
            // Fail closed. A null tenant is NOT "host" -- creating a host user here is precisely the
            // outcome this guard exists to prevent.
            throw new UserFriendlyException(L("CareersTenantNotResolved"));
        }

        return tenantId.Value;
    }
}
