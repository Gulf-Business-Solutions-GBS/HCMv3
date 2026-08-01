using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization.Users;
using Abp.Configuration;
using Abp.Domain.Uow;
using Abp.Timing;
using Abp.UI;
using Castle.MicroKernel.Registration;
using HCMv3.Authorization.Roles;
using HCMv3.Careers;
using HCMv3.Careers.Dto;
using HCMv3.Configuration;
using HCMv3.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Careers;

/// <summary>
/// Tests for the PUBLIC candidate-identity surface (wave R2 registration / forgot password / reset).
///
/// DELIBERATE DEVIATION FROM R1'S HARNESS -- read this before "fixing" it. R1's
/// <see cref="CareersAppService_Tests"/> runs on the bespoke <c>RecruitmentTestDbContext</c> because the
/// entities it queries have no ABP data filters there, which reproduces the worst case for a READ endpoint.
/// R2 does not read, it CREATES ABP USERS, and the whole claim under test is "the user row lands on the
/// resolved tenant and nowhere else". Proving that against fakes would only prove the fakes. So these tests
/// run on the REAL ABP container and the REAL <c>HCMv3DbContext</c>: real
/// <c>UserRegistrationManager</c>, real <c>UserManager</c>/ASP.NET Identity, real settings, real unit of
/// work. The assertions then read the actual <c>AbpUsers</c> / <c>Applicants</c> rows back, with query
/// filters ignored, so a user created on the wrong tenant (or on host) cannot hide from the test.
///
/// The only substituted pieces are the two things that genuinely cannot exist in a test process:
/// <see cref="ICareersTenantResolver"/> (there is no inbound HTTP request, so no subdomain) and
/// <see cref="ICareersUrlService"/> (which reads <c>App:CareersDomainFormat</c> -- deliberately unset).
///
/// Tenant A = the seeded default tenant (id 1). Tenant B = a second tenant created below.
/// </summary>
// ReSharper disable once InconsistentNaming
public class CareersAccountAppService_Tests : AppTestBase
{
    private const string TenantBTenancyName = "globex";

    private readonly ICareersTenantResolver _tenantResolver = Substitute.For<ICareersTenantResolver>();
    private readonly ICareersUrlService _careersUrlService = Substitute.For<ICareersUrlService>();

    private readonly ICareersAccountAppService _careersAccountAppService;
    private readonly ISettingManager _settingManager;

    private readonly int _tenantA;
    private readonly int _tenantB;

    public CareersAccountAppService_Tests()
    {
        _careersAccountAppService = Resolve<ICareersAccountAppService>();
        _settingManager = Resolve<ISettingManager>();

        _tenantA = GetTenantIdByTenancyName("Default");
        _tenantB = CreateSecondTenant();

        // Both tenants opt in, so that a test which expects a refusal is proving the TENANT RESOLUTION
        // guard and not merely that the feature switch is off.
        EnableCandidateRegistration(_tenantA);
        EnableCandidateRegistration(_tenantB);

        // A careers request is anonymous. AppTestBase's constructor logs in as the default tenant admin,
        // which would mask exactly the bug under test (an ambient tenant that happens to be right). Clear
        // it: from here on AbpSession.TenantId is null, i.e. ABP considers the caller HOST and the
        // MustHaveTenant filter is off -- the production worst case.
        AbpSession.TenantId = null;
        AbpSession.UserId = null;

        _careersUrlService.CreateEmailActivationUrlFormatOrNull(Arg.Any<string>())
            .Returns(call => "https://" + call.Arg<string>() +
                             ".careers.example.com/account/confirm-email" +
                             "?userId={userId}&confirmationCode={confirmationCode}&tenantId={tenantId}");

        _careersUrlService.CreatePasswordResetUrlFormatOrNull(Arg.Any<string>())
            .Returns(call => "https://" + call.Arg<string>() +
                             ".careers.example.com/account/reset-password" +
                             "?userId={userId}&resetCode={resetCode}&expireDate={expireDate}&tenantId={tenantId}");
    }

    protected override void PreInitialize()
    {
        base.PreInitialize();

        LocalIocManager.IocContainer.Register(
            Component.For<ICareersTenantResolver>().Instance(_tenantResolver).IsDefault(),
            Component.For<ICareersUrlService>().Instance(_careersUrlService).IsDefault()
        );
    }

    // ---------------------------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------------------------

    private void ResolvesTo(int? tenantId)
    {
        _tenantResolver.ResolveTenantIdOrNullAsync().Returns(Task.FromResult(tenantId));
    }

    private int GetTenantIdByTenancyName(string tenancyName)
    {
        return UsingDbContext(null, ctx => ctx.Tenants.IgnoreQueryFilters()
            .Single(t => t.TenancyName == tenancyName).Id);
    }

    /// <summary>
    /// Adds a second tenant plus the roles a candidate registration needs on it. Created directly against
    /// the context rather than through <c>TenantManager.CreateWithAdminUserAsync</c> because the test only
    /// needs the tenant to be a legitimate destination for a user, not a fully provisioned one.
    /// </summary>
    private int CreateSecondTenant()
    {
        return UsingDbContext(null, ctx =>
        {
            var existing = ctx.Tenants.IgnoreQueryFilters()
                .FirstOrDefault(t => t.TenancyName == TenantBTenancyName);
            if (existing != null)
            {
                return existing.Id;
            }

            var tenant = ctx.Tenants.Add(new Tenant(TenantBTenancyName, "Globex") { IsActive = true }).Entity;
            ctx.SaveChanges();

            ctx.Roles.Add(new Role(tenant.Id, StaticRoleNames.Tenants.Candidate, StaticRoleNames.Tenants.Candidate)
            {
                IsStatic = true
            });
            ctx.SaveChanges();

            return tenant.Id;
        });
    }

    private void EnableCandidateRegistration(int tenantId)
    {
        _settingManager.ChangeSettingForTenant(
            tenantId, AppSettings.Careers.AllowCandidateSelfRegistration, "true");
    }

    private static CareersRegisterInput NewRegistration(string email = "candidate@example.com")
    {
        return new CareersRegisterInput
        {
            EmailAddress = email,
            FirstNameEn = "Jane",
            LastNameEn = "Doe",
            FirstNameAr = "جين",
            LastNameAr = "دو",
            Password = "Str0ng!Passw0rd"
        };
    }

    private AbpUserBase GetUserOrNull(string email)
    {
        return UsingDbContext(null, ctx => ctx.Users.IgnoreQueryFilters()
            .FirstOrDefault(u => u.EmailAddress == email));
    }

    // ---------------------------------------------------------------------------------------------
    // Tenant isolation -- the core claim of this wave
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public async Task Should_Create_The_User_On_The_Resolved_Tenant()
    {
        ResolvesTo(_tenantA);

        await _careersAccountAppService.Register(NewRegistration());

        var user = GetUserOrNull("candidate@example.com");
        user.ShouldNotBeNull();
        user.TenantId.ShouldBe(_tenantA);
    }

    [Fact]
    public async Task Should_Create_The_User_On_The_Other_Tenant_When_That_Subdomain_Is_Resolved()
    {
        ResolvesTo(_tenantB);

        await _careersAccountAppService.Register(NewRegistration());

        var user = GetUserOrNull("candidate@example.com");
        user.ShouldNotBeNull();

        // The bug this exists to catch: landing on tenant 1 (or host) because that is what the ambient
        // session/filters would have produced.
        user.TenantId.ShouldBe(_tenantB);
        user.TenantId.ShouldNotBe(_tenantA);
    }

    [Fact]
    public async Task Should_Refuse_To_Register_When_No_Tenant_Can_Be_Resolved()
    {
        ResolvesTo(null);

        await Should.ThrowAsync<UserFriendlyException>(
            () => _careersAccountAppService.Register(NewRegistration()));
    }

    [Fact]
    public async Task Should_Not_Create_A_Host_User_When_No_Tenant_Can_Be_Resolved()
    {
        ResolvesTo(null);

        await Should.ThrowAsync<UserFriendlyException>(
            () => _careersAccountAppService.Register(NewRegistration()));

        // The specific failure mode: "no tenant" being read as "host" and producing a host user, which in
        // ABP is the MOST privileged place a user can land. Nothing at all may have been created.
        GetUserOrNull("candidate@example.com").ShouldBeNull();

        var hostUsers = UsingDbContext(null, ctx => ctx.Users.IgnoreQueryFilters()
            .Count(u => u.TenantId == null && u.EmailAddress == "candidate@example.com"));
        hostUsers.ShouldBe(0);

        var applicants = UsingDbContext(null, ctx => ctx.Applicants.IgnoreQueryFilters()
            .Count(a => a.Email == "candidate@example.com"));
        applicants.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Create_The_Applicant_Profile_On_The_Resolved_Tenant_And_Link_It_To_The_User()
    {
        ResolvesTo(_tenantB);

        await _careersAccountAppService.Register(NewRegistration());

        var user = GetUserOrNull("candidate@example.com");

        var applicant = UsingDbContext(null, ctx => ctx.Applicants.IgnoreQueryFilters()
            .Single(a => a.Email == "candidate@example.com"));

        applicant.TenantId.ShouldBe(_tenantB);
        applicant.UserId.ShouldBe(user.Id);
        applicant.FirstNameEn.ShouldBe("Jane");
        applicant.LastNameAr.ShouldBe("دو");
    }

    [Fact]
    public async Task Should_Allow_The_Same_Email_To_Register_Separately_On_Each_Tenant()
    {
        // Candidates are per-tenant identities. The same person applying to two employers gets two
        // independent accounts; neither can see the other's tenant.
        ResolvesTo(_tenantA);
        await _careersAccountAppService.Register(NewRegistration());

        ResolvesTo(_tenantB);
        await _careersAccountAppService.Register(NewRegistration());

        var tenantIds = UsingDbContext(null, ctx => ctx.Users.IgnoreQueryFilters()
            .Where(u => u.EmailAddress == "candidate@example.com")
            .Select(u => u.TenantId)
            .ToList());

        tenantIds.Count.ShouldBe(2);
        tenantIds.ShouldContain(_tenantA);
        tenantIds.ShouldContain(_tenantB);
    }

    // ---------------------------------------------------------------------------------------------
    // Privilege of the created account
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public async Task Should_Give_The_New_Candidate_Only_The_Candidate_Role()
    {
        ResolvesTo(_tenantA);

        await _careersAccountAppService.Register(NewRegistration());

        var user = GetUserOrNull("candidate@example.com");

        var roleNames = UsingDbContext(null, ctx =>
            (from userRole in ctx.UserRoles.IgnoreQueryFilters()
             join role in ctx.Roles.IgnoreQueryFilters() on userRole.RoleId equals role.Id
             where userRole.UserId == user.Id
             select role.Name).ToList());

        // UserRegistrationManager assigns every IsDefault role -- normally the tenant "User" role, which a
        // tenant admin may have granted staff permissions to. The app service must OVERWRITE that, not
        // append to it.
        roleNames.ShouldBe(new[] { StaticRoleNames.Tenants.Candidate });
        roleNames.ShouldNotContain(StaticRoleNames.Tenants.User);
        roleNames.ShouldNotContain(StaticRoleNames.Tenants.Admin);
    }

    [Fact]
    public async Task Should_Not_Store_The_Password_Anywhere_In_Plaintext()
    {
        ResolvesTo(_tenantA);

        await _careersAccountAppService.Register(NewRegistration());

        var user = GetUserOrNull("candidate@example.com");

        // The whole point of retiring Applicant.UserName/Password: the chosen password exists only as an
        // ASP.NET Identity hash on the user row.
        user.Password.ShouldNotBe("Str0ng!Passw0rd");
        user.Password.ShouldNotBeNullOrEmpty();

        // And nothing on the applicant row carries it. Asserted structurally so re-adding a credential
        // column to the entity fails here rather than in a security review.
        var applicantProperties = typeof(HCMv3.Recruitment.Applicant)
            .GetProperties().Select(p => p.Name).ToList();
        applicantProperties.ShouldNotContain("Password");
        applicantProperties.ShouldNotContain("UserName");
        applicantProperties.ShouldContain("UserId");
    }

    // ---------------------------------------------------------------------------------------------
    // Registration gate
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public async Task Should_Refuse_Registration_When_The_Tenant_Has_Not_Opted_In()
    {
        _settingManager.ChangeSettingForTenant(
            _tenantA, AppSettings.Careers.AllowCandidateSelfRegistration, "false");

        ResolvesTo(_tenantA);

        await Should.ThrowAsync<UserFriendlyException>(
            () => _careersAccountAppService.Register(NewRegistration()));

        GetUserOrNull("candidate@example.com").ShouldBeNull();
    }

    [Fact]
    public async Task Should_Read_The_Registration_Gate_From_The_Resolved_Tenant_Not_The_Ambient_One()
    {
        // Tenant A is closed, tenant B is open. A request that resolves to B must succeed even though the
        // ambient session (null => host) and tenant A both say otherwise, and vice versa.
        _settingManager.ChangeSettingForTenant(
            _tenantA, AppSettings.Careers.AllowCandidateSelfRegistration, "false");

        ResolvesTo(_tenantB);
        await _careersAccountAppService.Register(NewRegistration("open@example.com"));
        GetUserOrNull("open@example.com").ShouldNotBeNull();

        ResolvesTo(_tenantA);
        await Should.ThrowAsync<UserFriendlyException>(
            () => _careersAccountAppService.Register(NewRegistration("closed@example.com")));
        GetUserOrNull("closed@example.com").ShouldBeNull();
    }

    // ---------------------------------------------------------------------------------------------
    // Password reset -- enumeration resistance and cross-tenant reach
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public async Task Should_Not_Distinguish_A_Known_From_An_Unknown_Email_On_Forgot_Password()
    {
        ResolvesTo(_tenantA);
        await _careersAccountAppService.Register(NewRegistration());

        // Known address: succeeds silently.
        await _careersAccountAppService.SendPasswordResetCode(
            new CareersSendPasswordResetCodeInput { EmailAddress = "candidate@example.com" });

        // Unknown address: also succeeds silently. Same void return, no exception, nothing for a caller to
        // branch on. Legacy's IsUserNameExist endpoint answered this question directly
        // (AccountController.cs:45-67) and is not ported.
        await _careersAccountAppService.SendPasswordResetCode(
            new CareersSendPasswordResetCodeInput { EmailAddress = "nobody@example.com" });
    }

    [Fact]
    public async Task Should_Set_A_Reset_Code_Only_For_A_Candidate_Of_The_Resolved_Tenant()
    {
        ResolvesTo(_tenantB);
        await _careersAccountAppService.Register(NewRegistration());

        // Same address, asked for from TENANT A's subdomain. Tenant A has no such user, so nothing may
        // happen -- and in particular tenant B's user must not receive a reset code triggered from another
        // tenant's portal.
        ResolvesTo(_tenantA);
        await _careersAccountAppService.SendPasswordResetCode(
            new CareersSendPasswordResetCodeInput { EmailAddress = "candidate@example.com" });

        var resetCode = UsingDbContext(null, ctx => ctx.Users.IgnoreQueryFilters()
            .Single(u => u.EmailAddress == "candidate@example.com" && u.TenantId == _tenantB)
            .PasswordResetCode);

        resetCode.ShouldBeNull();

        // From tenant B's own subdomain it does work.
        ResolvesTo(_tenantB);
        await _careersAccountAppService.SendPasswordResetCode(
            new CareersSendPasswordResetCodeInput { EmailAddress = "candidate@example.com" });

        resetCode = UsingDbContext(null, ctx => ctx.Users.IgnoreQueryFilters()
            .Single(u => u.EmailAddress == "candidate@example.com" && u.TenantId == _tenantB)
            .PasswordResetCode);

        resetCode.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Should_Not_Send_Reset_Mail_For_A_Staff_Account()
    {
        // The seeded tenant admin is a real user with a real email address on tenant A. The public careers
        // endpoint must not be usable to trigger password-reset mail at a tenant's employees.
        var adminEmail = UsingDbContext(null, ctx => ctx.Users.IgnoreQueryFilters()
            .Single(u => u.TenantId == _tenantA && u.UserName == AbpUserBase.AdminUserName).EmailAddress);

        ResolvesTo(_tenantA);
        await _careersAccountAppService.SendPasswordResetCode(
            new CareersSendPasswordResetCodeInput { EmailAddress = adminEmail });

        var resetCode = UsingDbContext(null, ctx => ctx.Users.IgnoreQueryFilters()
            .Single(u => u.TenantId == _tenantA && u.UserName == AbpUserBase.AdminUserName).PasswordResetCode);

        resetCode.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Refuse_Forgot_Password_When_No_Tenant_Can_Be_Resolved()
    {
        ResolvesTo(null);

        await Should.ThrowAsync<UserFriendlyException>(
            () => _careersAccountAppService.SendPasswordResetCode(
                new CareersSendPasswordResetCodeInput { EmailAddress = "candidate@example.com" }));
    }

    // ---------------------------------------------------------------------------------------------
    // Reset password
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public async Task Should_Reset_The_Password_With_A_Valid_Code()
    {
        ResolvesTo(_tenantA);
        await _careersAccountAppService.Register(NewRegistration());
        await _careersAccountAppService.SendPasswordResetCode(
            new CareersSendPasswordResetCodeInput { EmailAddress = "candidate@example.com" });

        var user = UsingDbContext(null, ctx => ctx.Users.IgnoreQueryFilters()
            .Single(u => u.EmailAddress == "candidate@example.com"));

        var result = await _careersAccountAppService.ResetPassword(new CareersResetPasswordInput
        {
            UserId = user.Id,
            ResetCode = user.PasswordResetCode,
            Password = "An0ther!Passw0rd",
            ExpireDate = Clock.Now.AddHours(1)
        });

        result.UserName.ShouldBe("candidate@example.com");

        var updated = UsingDbContext(null, ctx => ctx.Users.IgnoreQueryFilters()
            .Single(u => u.EmailAddress == "candidate@example.com"));

        updated.PasswordResetCode.ShouldBeNull();   // single use
        updated.Password.ShouldNotBe(user.Password);
        updated.Password.ShouldNotBe("An0ther!Passw0rd");
    }

    [Fact]
    public async Task Should_Not_Let_A_Reset_Code_Be_Used_From_Another_Tenants_Subdomain()
    {
        ResolvesTo(_tenantB);
        await _careersAccountAppService.Register(NewRegistration());
        await _careersAccountAppService.SendPasswordResetCode(
            new CareersSendPasswordResetCodeInput { EmailAddress = "candidate@example.com" });

        var user = UsingDbContext(null, ctx => ctx.Users.IgnoreQueryFilters()
            .Single(u => u.EmailAddress == "candidate@example.com" && u.TenantId == _tenantB));

        // A genuine code for a tenant B user, replayed against tenant A's careers subdomain.
        ResolvesTo(_tenantA);

        await Should.ThrowAsync<UserFriendlyException>(
            () => _careersAccountAppService.ResetPassword(new CareersResetPasswordInput
            {
                UserId = user.Id,
                ResetCode = user.PasswordResetCode,
                Password = "An0ther!Passw0rd",
                ExpireDate = Clock.Now.AddHours(1)
            }));

        var unchanged = UsingDbContext(null, ctx => ctx.Users.IgnoreQueryFilters()
            .Single(u => u.Id == user.Id));
        unchanged.Password.ShouldBe(user.Password);
    }

    [Fact]
    public async Task Should_Reject_An_Expired_Or_Wrong_Reset_Code()
    {
        ResolvesTo(_tenantA);
        await _careersAccountAppService.Register(NewRegistration());
        await _careersAccountAppService.SendPasswordResetCode(
            new CareersSendPasswordResetCodeInput { EmailAddress = "candidate@example.com" });

        var user = UsingDbContext(null, ctx => ctx.Users.IgnoreQueryFilters()
            .Single(u => u.EmailAddress == "candidate@example.com"));

        await Should.ThrowAsync<UserFriendlyException>(
            () => _careersAccountAppService.ResetPassword(new CareersResetPasswordInput
            {
                UserId = user.Id,
                ResetCode = user.PasswordResetCode,
                Password = "An0ther!Passw0rd",
                ExpireDate = Clock.Now.AddHours(-1)
            }));

        await Should.ThrowAsync<UserFriendlyException>(
            () => _careersAccountAppService.ResetPassword(new CareersResetPasswordInput
            {
                UserId = user.Id,
                ResetCode = "not-the-code",
                Password = "An0ther!Passw0rd",
                ExpireDate = Clock.Now.AddHours(1)
            }));
    }

    [Fact]
    public async Task Should_Refuse_Reset_When_No_Tenant_Can_Be_Resolved()
    {
        ResolvesTo(_tenantA);
        await _careersAccountAppService.Register(NewRegistration());
        await _careersAccountAppService.SendPasswordResetCode(
            new CareersSendPasswordResetCodeInput { EmailAddress = "candidate@example.com" });

        var user = UsingDbContext(null, ctx => ctx.Users.IgnoreQueryFilters()
            .Single(u => u.EmailAddress == "candidate@example.com"));

        ResolvesTo(null);

        await Should.ThrowAsync<UserFriendlyException>(
            () => _careersAccountAppService.ResetPassword(new CareersResetPasswordInput
            {
                UserId = user.Id,
                ResetCode = user.PasswordResetCode,
                Password = "An0ther!Passw0rd",
                ExpireDate = Clock.Now.AddHours(1)
            }));
    }
}
