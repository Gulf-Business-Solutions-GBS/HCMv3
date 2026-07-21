using System.Linq;
using System.Threading.Tasks;
using Abp.Configuration;
using Microsoft.EntityFrameworkCore;
using HCMv3.Authorization.Session;
using HCMv3.Configuration;
using HCMv3.Test.Base;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Authorization.Session;

public class SessionCreationForAuth_Tests : AppTestBase<HCMv3TestModule>
{
    private readonly IUserSessionManager _userSessionManager;
    private readonly ISettingManager _settingManager;

    public SessionCreationForAuth_Tests()
    {
        _userSessionManager = Resolve<IUserSessionManager>();
        _settingManager = Resolve<ISettingManager>();
    }

    private async Task EnableSessionManagement()
    {
        await _settingManager.ChangeSettingForApplicationAsync(
            AppSettings.UserManagement.SessionManagement.IsEnabled, "true");
    }

    private async Task EnableOneConcurrentLogin()
    {
        await _settingManager.ChangeSettingForApplicationAsync(
            AppSettings.UserManagement.AllowOneConcurrentLoginPerUser, "true");
    }

    [Fact]
    public async Task Should_Return_Null_When_Session_Management_Disabled()
    {
        var isEnabled = await _userSessionManager.IsSessionManagementEnabledAsync();
        isEnabled.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Create_Session_For_Host_User()
    {
        LoginAsHostAdmin();
        await EnableSessionManagement();

        var userId = AbpSession.UserId!.Value;
        var sessionToken = await _userSessionManager.CreateSessionAsync(
            userId, null, "10.0.0.1", "TestBrowser/1.0");

        sessionToken.ShouldNotBeNullOrEmpty();

        var sessions = await _userSessionManager.GetActiveSessionsAsync(userId, null);
        sessions.ShouldContain(s => s.SessionToken == sessionToken);
    }

    [Fact]
    public async Task Should_Create_Session_For_Tenant_User()
    {
        LoginAsDefaultTenantAdmin();
        await EnableSessionManagement();

        var userId = AbpSession.UserId!.Value;
        var tenantId = AbpSession.TenantId;
        var sessionToken = await _userSessionManager.CreateSessionAsync(
            userId, tenantId, "10.0.0.2", "TestBrowser/2.0");

        sessionToken.ShouldNotBeNullOrEmpty();

        var sessions = await _userSessionManager.GetActiveSessionsAsync(userId, tenantId);
        sessions.ShouldContain(s => s.SessionToken == sessionToken);
    }

    [Fact]
    public async Task Should_Invalidate_Other_Sessions_When_Concurrent_Login_Restricted()
    {
        LoginAsDefaultTenantAdmin();
        await EnableSessionManagement();
        await EnableOneConcurrentLogin();

        var userId = AbpSession.UserId!.Value;
        var tenantId = AbpSession.TenantId;

        var firstSession = await _userSessionManager.CreateSessionAsync(
            userId, tenantId, "10.0.0.1", "Chrome/120.0");

        var secondSession = await _userSessionManager.CreateSessionAsync(
            userId, tenantId, "10.0.0.2", "Firefox/119.0");

        await _userSessionManager.InvalidateAllSessionsAsync(userId, tenantId, secondSession);

        (await _userSessionManager.ValidateSessionAsync(firstSession, "10.0.0.1", "Chrome/120.0")).ShouldBeFalse();
        (await _userSessionManager.ValidateSessionAsync(secondSession, "10.0.0.2", "Firefox/119.0")).ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Keep_All_Sessions_When_Concurrent_Login_Not_Restricted()
    {
        LoginAsDefaultTenantAdmin();
        await EnableSessionManagement();

        var userId = AbpSession.UserId!.Value;
        var tenantId = AbpSession.TenantId;

        var firstSession = await _userSessionManager.CreateSessionAsync(
            userId, tenantId, "10.0.0.1", "Chrome/120.0");

        var secondSession = await _userSessionManager.CreateSessionAsync(
            userId, tenantId, "10.0.0.2", "Firefox/119.0");

        (await _userSessionManager.ValidateSessionAsync(firstSession, "10.0.0.1", "Chrome/120.0")).ShouldBeTrue();
        (await _userSessionManager.ValidateSessionAsync(secondSession, "10.0.0.2", "Firefox/119.0")).ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Create_Independent_Sessions_For_Different_Tenants()
    {
        await EnableSessionManagement();

        LoginAsHostAdmin();
        var hostUserId = AbpSession.UserId!.Value;
        var hostSession = await _userSessionManager.CreateSessionAsync(
            hostUserId, null, "10.0.0.1", "Chrome/120.0");

        (await _userSessionManager.ValidateSessionAsync(hostSession, "10.0.0.1", "Chrome/120.0")).ShouldBeTrue();
        var hostSessions = await _userSessionManager.GetActiveSessionsAsync(hostUserId, null);
        hostSessions.Count.ShouldBe(1);

        LoginAsDefaultTenantAdmin();
        var tenantUserId = AbpSession.UserId!.Value;
        var tenantId = AbpSession.TenantId;
        var tenantSession = await _userSessionManager.CreateSessionAsync(
            tenantUserId, tenantId, "10.0.0.2", "Firefox/119.0");

        (await _userSessionManager.ValidateSessionAsync(tenantSession, "10.0.0.2", "Firefox/119.0")).ShouldBeTrue();
        var tenantSessions = await _userSessionManager.GetActiveSessionsAsync(tenantUserId, tenantId);
        tenantSessions.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Invalidate_Only_Same_User_Sessions_Not_Other_Users()
    {
        LoginAsDefaultTenantAdmin();
        await EnableSessionManagement();
        await EnableOneConcurrentLogin();

        var adminUserId = AbpSession.UserId!.Value;
        var tenantId = AbpSession.TenantId;

        var adminSession = await _userSessionManager.CreateSessionAsync(
            adminUserId, tenantId, "10.0.0.1", "Chrome/120.0");

        var adminNewSession = await _userSessionManager.CreateSessionAsync(
            adminUserId, tenantId, "10.0.0.2", "Firefox/119.0");
        await _userSessionManager.InvalidateAllSessionsAsync(adminUserId, tenantId, adminNewSession);

        (await _userSessionManager.ValidateSessionAsync(adminSession, "10.0.0.1", "Chrome/120.0")).ShouldBeFalse();
        (await _userSessionManager.ValidateSessionAsync(adminNewSession, "10.0.0.2", "Firefox/119.0")).ShouldBeTrue();

        LoginAsHostAdmin();
        var hostUserId = AbpSession.UserId!.Value;
        var hostSession = await _userSessionManager.CreateSessionAsync(
            hostUserId, null, "10.0.0.3", "Safari/17.0");

        (await _userSessionManager.ValidateSessionAsync(hostSession, "10.0.0.3", "Safari/17.0")).ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Create_Multiple_Sessions_For_Same_User_Different_Devices()
    {
        LoginAsDefaultTenantAdmin();
        await EnableSessionManagement();

        var userId = AbpSession.UserId!.Value;
        var tenantId = AbpSession.TenantId;

        var desktopSession = await _userSessionManager.CreateSessionAsync(
            userId, tenantId, "10.0.0.1", "Chrome/120.0");
        var mobileSession = await _userSessionManager.CreateSessionAsync(
            userId, tenantId, "10.0.0.2", "MobileApp/1.0");
        var tabletSession = await _userSessionManager.CreateSessionAsync(
            userId, tenantId, "10.0.0.3", "TabletApp/1.0");

        var sessions = await _userSessionManager.GetActiveSessionsAsync(userId, tenantId);
        sessions.Count.ShouldBe(3);
        sessions.ShouldContain(s => s.SessionToken == desktopSession);
        sessions.ShouldContain(s => s.SessionToken == mobileSession);
        sessions.ShouldContain(s => s.SessionToken == tabletSession);
    }
}
