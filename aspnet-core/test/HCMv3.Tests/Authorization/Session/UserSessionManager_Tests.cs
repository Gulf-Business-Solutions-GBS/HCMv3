using System.Threading.Tasks;
using Abp.Configuration;
using HCMv3.Authorization.Session;
using HCMv3.Configuration;
using HCMv3.Test.Base;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Authorization.Session;

public class UserSessionManager_Tests : AppTestBase<HCMv3TestModule>
{
    private readonly IUserSessionManager _userSessionManager;
    private readonly ISettingManager _settingManager;

    public UserSessionManager_Tests()
    {
        _userSessionManager = Resolve<IUserSessionManager>();
        _settingManager = Resolve<ISettingManager>();
    }

    private async Task EnableSessionManagement()
    {
        await _settingManager.ChangeSettingForApplicationAsync(
            AppSettings.UserManagement.SessionManagement.IsEnabled, "true");
    }

    [Fact]
    public async Task Should_Create_Session()
    {
        // Arrange
        await EnableSessionManagement();
        var userId = AbpSession.UserId!.Value;
        var tenantId = AbpSession.TenantId;

        // Act
        var sessionToken = await _userSessionManager.CreateSessionAsync(
            userId, tenantId, "192.168.1.1", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/120.0");

        // Assert
        sessionToken.ShouldNotBeNullOrEmpty();

        var sessions = await _userSessionManager.GetActiveSessionsAsync(userId, tenantId);
        sessions.ShouldNotBeEmpty();
        sessions.ShouldContain(s => s.SessionToken == sessionToken);
    }

    [Fact]
    public async Task Should_Validate_Valid_Session()
    {
        // Arrange
        await EnableSessionManagement();
        var userId = AbpSession.UserId!.Value;
        var tenantId = AbpSession.TenantId;

        var sessionToken = await _userSessionManager.CreateSessionAsync(
            userId, tenantId, "192.168.1.1", "Mozilla/5.0 Chrome/120.0");

        // Act
        var isValid = await _userSessionManager.ValidateSessionAsync(
            sessionToken, "192.168.1.1", "Mozilla/5.0 Chrome/120.0");

        // Assert
        isValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Reject_Session_With_Mismatched_Fingerprint()
    {
        // Arrange
        await EnableSessionManagement();
        await _settingManager.ChangeSettingForApplicationAsync(
            AppSettings.UserManagement.SessionManagement.IsSessionFingerprintValidationEnabled, "true");
        await _settingManager.ChangeSettingForApplicationAsync(
            AppSettings.UserManagement.SessionManagement.SessionFingerprintValidationPolicy, nameof(SessionFingerprintValidationPolicy.IpAndUserAgent));

        var userId = AbpSession.UserId!.Value;
        var sessionToken = await _userSessionManager.CreateSessionAsync(
            userId, AbpSession.TenantId, "192.168.1.1", "Mozilla/5.0 Chrome/120.0");

        // Act - different IP and UserAgent (simulates cookie theft)
        var isValid = await _userSessionManager.ValidateSessionAsync(
            sessionToken, "10.0.0.5", "Mozilla/5.0 Firefox/119.0");

        // Assert
        isValid.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Reject_Session_With_Different_Ip_When_IpOnly_Policy()
    {
        // Arrange
        await EnableSessionManagement();
        await _settingManager.ChangeSettingForApplicationAsync(
            AppSettings.UserManagement.SessionManagement.IsSessionFingerprintValidationEnabled, "true");
        await _settingManager.ChangeSettingForApplicationAsync(
            AppSettings.UserManagement.SessionManagement.SessionFingerprintValidationPolicy, nameof(SessionFingerprintValidationPolicy.IpOnly));

        var userId = AbpSession.UserId!.Value;
        var sessionToken = await _userSessionManager.CreateSessionAsync(
            userId, AbpSession.TenantId, "192.168.1.1", "Mozilla/5.0 Chrome/120.0");

        // Act - same UA, different IP
        var isValid = await _userSessionManager.ValidateSessionAsync(
            sessionToken, "10.0.0.5", "Mozilla/5.0 Chrome/120.0");

        // Assert
        isValid.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Allow_Session_With_Different_Ip_When_UserAgentOnly_Policy()
    {
        // Arrange
        await EnableSessionManagement();
        await _settingManager.ChangeSettingForApplicationAsync(
            AppSettings.UserManagement.SessionManagement.IsSessionFingerprintValidationEnabled, "true");
        await _settingManager.ChangeSettingForApplicationAsync(
            AppSettings.UserManagement.SessionManagement.SessionFingerprintValidationPolicy, nameof(SessionFingerprintValidationPolicy.UserAgentOnly));

        var userId = AbpSession.UserId!.Value;
        var sessionToken = await _userSessionManager.CreateSessionAsync(
            userId, AbpSession.TenantId, "192.168.1.1", "Mozilla/5.0 Chrome/120.0");

        // Act - different IP, same UA
        var isValid = await _userSessionManager.ValidateSessionAsync(
            sessionToken, "10.0.0.5", "Mozilla/5.0 Chrome/120.0");

        // Assert
        isValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Invalidate_Session()
    {
        // Arrange
        await EnableSessionManagement();
        var userId = AbpSession.UserId!.Value;
        var sessionToken = await _userSessionManager.CreateSessionAsync(
            userId, AbpSession.TenantId, "192.168.1.1", "Mozilla/5.0 Chrome/120.0");

        // Act
        await _userSessionManager.InvalidateSessionAsync(sessionToken);

        // Assert
        var isValid = await _userSessionManager.ValidateSessionAsync(
            sessionToken, "192.168.1.1", "Mozilla/5.0 Chrome/120.0");
        isValid.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Invalidate_All_Sessions_Except_Current()
    {
        // Arrange
        await EnableSessionManagement();
        var userId = AbpSession.UserId!.Value;
        var tenantId = AbpSession.TenantId;

        var session1 = await _userSessionManager.CreateSessionAsync(
            userId, tenantId, "192.168.1.1", "Mozilla/5.0 Chrome/120.0");
        var session2 = await _userSessionManager.CreateSessionAsync(
            userId, tenantId, "192.168.1.2", "Mozilla/5.0 Firefox/119.0");
        var session3 = await _userSessionManager.CreateSessionAsync(
            userId, tenantId, "192.168.1.3", "Mozilla/5.0 Safari/17.0");

        // Act - invalidate all except session2
        await _userSessionManager.InvalidateAllSessionsAsync(userId, tenantId, session2);

        // Assert
        (await _userSessionManager.ValidateSessionAsync(session1, "192.168.1.1", "Mozilla/5.0 Chrome/120.0")).ShouldBeFalse();
        (await _userSessionManager.ValidateSessionAsync(session2, "192.168.1.2", "Mozilla/5.0 Firefox/119.0")).ShouldBeTrue();
        (await _userSessionManager.ValidateSessionAsync(session3, "192.168.1.3", "Mozilla/5.0 Safari/17.0")).ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Not_Validate_When_Session_Management_Disabled()
    {
        // Arrange - session management is disabled by default
        var isEnabled = await _userSessionManager.IsSessionManagementEnabledAsync();

        // Assert
        isEnabled.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Skip_Fingerprint_When_Disabled()
    {
        // Arrange
        await EnableSessionManagement();
        await _settingManager.ChangeSettingForApplicationAsync(
            AppSettings.UserManagement.SessionManagement.IsSessionFingerprintValidationEnabled, "false");

        var userId = AbpSession.UserId!.Value;
        var sessionToken = await _userSessionManager.CreateSessionAsync(
            userId, AbpSession.TenantId, "192.168.1.1", "Mozilla/5.0 Chrome/120.0");

        // Act - completely different fingerprint
        var isValid = await _userSessionManager.ValidateSessionAsync(
            sessionToken, "10.0.0.5", "Mozilla/5.0 Firefox/119.0");

        // Assert - should still be valid because fingerprint validation is disabled
        isValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Reject_Null_SessionToken()
    {
        // Arrange
        await EnableSessionManagement();

        // Act
        var isValid = await _userSessionManager.ValidateSessionAsync(
            null, "192.168.1.1", "Mozilla/5.0 Chrome/120.0");

        // Assert
        isValid.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Reject_NonExistent_SessionToken()
    {
        // Arrange
        await EnableSessionManagement();

        // Act
        var isValid = await _userSessionManager.ValidateSessionAsync(
            "nonexistent-token", "192.168.1.1", "Mozilla/5.0 Chrome/120.0");

        // Assert
        isValid.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Get_Active_Sessions()
    {
        // Arrange
        await EnableSessionManagement();
        var userId = AbpSession.UserId!.Value;
        var tenantId = AbpSession.TenantId;

        await _userSessionManager.CreateSessionAsync(userId, tenantId, "192.168.1.1", "Chrome");
        await _userSessionManager.CreateSessionAsync(userId, tenantId, "192.168.1.2", "Firefox");

        // Act
        var sessions = await _userSessionManager.GetActiveSessionsAsync(userId, tenantId);

        // Assert
        sessions.Count.ShouldBe(2);
    }
}
