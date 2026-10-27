using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Authorization.Users;
using Abp.Configuration;
using Abp.UI;
using Castle.MicroKernel.Registration;
using HCMv3.Administration.Tools;
using HCMv3.Administration.Tools.Dto;
using HCMv3.Authorization;
using HCMv3.Authorization.Roles;
using HCMv3.Authorization.Users;
using HCMv3.Configuration;
using Microsoft.AspNetCore.Identity;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Administration;

/// <summary>
/// Reset User Passwords tool (legacy uscRandomPassword / uscChangePassword "by role", items 304/311).
/// The e-mailer is substituted, so these tests see exactly what Zero's credentials mail would have been sent.
/// </summary>
// ReSharper disable once InconsistentNaming
public class UserCredentialResetAppService_Tests : AppTestBase
{
    /// <summary>Hash of "123qwe", the seeded test password.</summary>
    private const string KnownPasswordHash = "AM4OLBpptxBYmM79lGOX9egzZk3vIQU3d/gFCJzaBjAPXzYIK3tQ2N7X4fcrHtElTw==";

    private readonly IUserEmailer _userEmailer;

    /// <summary>User id -> the plain password the e-mailer was handed for them.</summary>
    private readonly Dictionary<long, string> _emailedPasswords = new();

    public UserCredentialResetAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();

        _userEmailer = Substitute.For<IUserEmailer>();
        _userEmailer
            .When(e => e.SendEmailActivationLinkAsync(Arg.Any<User>(), Arg.Any<string>(), Arg.Any<string>()))
            .Do(call => _emailedPasswords[call.Arg<User>().Id] = call.ArgAt<string>(2));

        LocalIocManager.IocContainer.Register(Component.For<IUserEmailer>().Instance(_userEmailer).IsDefault());

        // The caller is an ordinary user holding only the tool's permission -- not the admin, who is never a
        // target and should not be needed to run it.
        AbpSession.UserId = CreateUser("caller", AppPermissions.Pages_Administration_Tools_ResetUserCredentials);
    }

    private IUserCredentialResetAppService Service => Resolve<IUserCredentialResetAppService>();

    private int TenantId => AbpSession.TenantId ?? 1;

    [Fact]
    public async Task Should_Reset_The_Password_And_Require_A_Change_At_Next_Sign_In()
    {
        var targetId = CreateUser("target");

        var result = await Service.ResetCredentials(new ResetUserCredentialsInput { UserIds = new List<long> { targetId } });

        result.PasswordsResetCount.ShouldBe(1);
        result.EmailsSentCount.ShouldBe(1);

        var user = GetUser(targetId);
        user.Password.ShouldNotBe(KnownPasswordHash);
        user.ShouldChangePasswordOnNextLogin.ShouldBeTrue();

        // The password that was e-mailed is the one now stored.
        Resolve<IPasswordHasher<User>>()
            .VerifyHashedPassword(user, user.Password, _emailedPasswords[targetId])
            .ShouldNotBe(PasswordVerificationResult.Failed);
    }

    [Fact]
    public async Task Should_Email_The_Credentials_Through_Zero_UserEmailer()
    {
        var targetId = CreateUser("mailed");

        await Service.ResetCredentials(new ResetUserCredentialsInput { UserIds = new List<long> { targetId } });

        await _userEmailer.Received(1).SendEmailActivationLinkAsync(
            Arg.Is<User>(u => u.Id == targetId),
            Arg.Any<string>(),
            Arg.Is<string>(p => !string.IsNullOrEmpty(p)));
    }

    [Fact]
    public async Task Should_Generate_A_Different_Strong_Password_For_Each_User()
    {
        var firstId = CreateUser("first");
        var secondId = CreateUser("second");

        await Service.ResetCredentials(new ResetUserCredentialsInput { UserIds = new List<long> { firstId, secondId } });

        _emailedPasswords[firstId].ShouldNotBe(_emailedPasswords[secondId]);
        _emailedPasswords[firstId].Length.ShouldBeGreaterThanOrEqualTo(CredentialPasswordGenerator.MinimumLength);
    }

    [Fact]
    public async Task Should_Reset_Every_Eligible_User_In_A_Role()
    {
        var roleId = CreateRole("CredentialResetRole");
        var inRoleA = CreateUser("rolea", roleId: roleId);
        var inRoleB = CreateUser("roleb", roleId: roleId);
        var outsideRole = CreateUser("outside");

        var result = await Service.ResetCredentials(new ResetUserCredentialsInput { RoleId = roleId });

        result.PasswordsResetCount.ShouldBe(2);
        _emailedPasswords.Keys.ShouldBe(new[] { inRoleA, inRoleB }, ignoreOrder: true);
        GetUser(outsideRole).Password.ShouldBe(KnownPasswordHash);
    }

    [Fact]
    public async Task Should_Never_Reset_An_Administrator()
    {
        var adminId = UsingDbContext(c => c.Users.Single(u => u.TenantId == TenantId && u.UserName == AbpUserBase.AdminUserName).Id);
        var adminHashBefore = GetUser(adminId).Password;

        var result = await Service.ResetCredentials(new ResetUserCredentialsInput { UserIds = new List<long> { adminId } });

        result.PasswordsResetCount.ShouldBe(0);
        result.Users.Single().PasswordReset.ShouldBeFalse();
        GetUser(adminId).Password.ShouldBe(adminHashBefore);
        _emailedPasswords.ShouldNotContainKey(adminId);
    }

    [Fact]
    public async Task Should_Leave_The_Password_Unchanged_When_The_Email_Fails()
    {
        var targetId = CreateUser("nomail");
        _userEmailer
            .SendEmailActivationLinkAsync(Arg.Any<User>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns<Task>(_ => throw new Exception("SMTP is down"));

        var result = await Service.ResetCredentials(new ResetUserCredentialsInput { UserIds = new List<long> { targetId } });

        result.PasswordsResetCount.ShouldBe(0);
        result.EmailsSentCount.ShouldBe(0);
        var user = GetUser(targetId);
        user.Password.ShouldBe(KnownPasswordHash);
        user.ShouldChangePasswordOnNextLogin.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Not_Reset_A_User_Without_An_Email_Address()
    {
        var targetId = CreateUser("blankmail", emailAddress: "");

        var result = await Service.ResetCredentials(new ResetUserCredentialsInput { UserIds = new List<long> { targetId } });

        result.PasswordsResetCount.ShouldBe(0);
        GetUser(targetId).Password.ShouldBe(KnownPasswordHash);
    }

    [Fact]
    public async Task Should_Refuse_When_Outbound_Email_Is_Switched_Off()
    {
        var targetId = CreateUser("gated");
        await Resolve<ISettingManager>().ChangeSettingForTenantAsync(TenantId, AppSettings.Email.IsOutboundEmailEnabled, "false");

        await Should.ThrowAsync<UserFriendlyException>(() =>
            Service.ResetCredentials(new ResetUserCredentialsInput { UserIds = new List<long> { targetId } }));

        GetUser(targetId).Password.ShouldBe(KnownPasswordHash);
    }

    [Fact]
    public async Task Should_Refuse_A_Caller_Without_The_Permission()
    {
        var targetId = CreateUser("protected");
        AbpSession.UserId = CreateUser("nopermission");

        await Should.ThrowAsync<AbpAuthorizationException>(() =>
            Service.ResetCredentials(new ResetUserCredentialsInput { UserIds = new List<long> { targetId } }));
        await Should.ThrowAsync<AbpAuthorizationException>(() =>
            Service.GetUsers(new GetCredentialResetUsersInput { MaxResultCount = 10 }));

        GetUser(targetId).Password.ShouldBe(KnownPasswordHash);
    }

    [Fact]
    public async Task Should_List_Resettable_Users_But_Not_The_Caller_Or_Administrators()
    {
        var targetId = CreateUser("listed");
        var callerId = AbpSession.UserId.Value;

        var result = await Service.GetUsers(new GetCredentialResetUsersInput { MaxResultCount = 1000 });

        result.Items.ShouldContain(u => u.Id == targetId);
        result.Items.ShouldNotContain(u => u.Id == callerId);
        result.Items.ShouldNotContain(u => u.UserName == AbpUserBase.AdminUserName);
    }

    private User GetUser(long userId) => UsingDbContext(c => c.Users.Single(u => u.Id == userId));

    private int CreateRole(string name)
    {
        return UsingDbContext(context =>
        {
            var role = context.Roles.Add(new Role(TenantId, name, name)).Entity;
            context.SaveChanges();
            return role.Id;
        });
    }

    private long CreateUser(string prefix, string grantedPermission = null, int? roleId = null, string emailAddress = null)
    {
        var userId = 0L;

        UsingDbContext(context =>
        {
            var unique = prefix + Guid.NewGuid().ToString("N").Substring(0, 6);
            var user = new User
            {
                TenantId = TenantId,
                UserName = unique,
                Name = prefix,
                Surname = "Tester",
                EmailAddress = emailAddress ?? unique + "@testdomain.com",
                IsEmailConfirmed = true,
                IsActive = true,
                Password = KnownPasswordHash
            };
            user.SetNormalizedNames();

            context.Users.Add(user);
            context.SaveChanges();
            userId = user.Id;

            if (grantedPermission != null)
            {
                context.UserPermissions.Add(new UserPermissionSetting
                {
                    TenantId = TenantId,
                    UserId = userId,
                    Name = grantedPermission,
                    IsGranted = true
                });
            }

            if (roleId.HasValue)
            {
                context.UserRoles.Add(new UserRole(TenantId, userId, roleId.Value));
            }
        });

        return userId;
    }
}
