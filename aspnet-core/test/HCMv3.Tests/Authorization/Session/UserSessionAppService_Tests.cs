using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Abp;
using Abp.Authorization;
using Abp.Configuration;
using Abp.UI;
using Microsoft.AspNetCore.Http;
using HCMv3.Authorization.Session;
using HCMv3.Authorization.Session.Dto;
using HCMv3.Configuration;
using HCMv3.Test.Base;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Authorization.Session;

public class UserSessionAppService_Tests : AppTestBase<HCMv3TestModule>
{
    private readonly IUserSessionAppService _userSessionAppService;
    private readonly IUserSessionManager _userSessionManager;
    private readonly ISettingManager _settingManager;

    public UserSessionAppService_Tests()
    {
        _userSessionAppService = Resolve<IUserSessionAppService>();
        _userSessionManager = Resolve<IUserSessionManager>();
        _settingManager = Resolve<ISettingManager>();
    }

    private async Task EnableSessionManagement()
    {
        await _settingManager.ChangeSettingForApplicationAsync(
            AppSettings.UserManagement.SessionManagement.IsEnabled, "true");
        await _settingManager.ChangeSettingForApplicationAsync(
            AppSettings.UserManagement.SessionManagement.IsSessionRevocationEnabled, "true");
    }

    private async Task DisableSessionManagement()
    {
        await _settingManager.ChangeSettingForApplicationAsync(
            AppSettings.UserManagement.SessionManagement.IsEnabled, "false");
    }

    [Fact]
    public async Task Should_Get_User_Sessions()
    {
        // Arrange
        await EnableSessionManagement();
        LoginAsDefaultTenantAdmin();

        var userId = AbpSession.UserId!.Value;
        var tenantId = AbpSession.TenantId;

        await _userSessionManager.CreateSessionAsync(userId, tenantId, "192.168.1.1", "Chrome/120.0");
        await _userSessionManager.CreateSessionAsync(userId, tenantId, "192.168.1.2", "Firefox/119.0");

        // Act
        var result = await _userSessionAppService.GetSessions(new GetUserSessionsInput());

        // Assert
        result.Items.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Throw_When_GetSessions_And_SessionManagement_Disabled()
    {
        // Arrange - session management is disabled by default
        LoginAsDefaultTenantAdmin();

        // Act & Assert
        var exception = await Assert.ThrowsAnyAsync<Exception>(
            () => _userSessionAppService.GetSessions(new GetUserSessionsInput())
        );

        exception.ShouldBeAssignableTo<AbpException>();
    }

    [Fact]
    public async Task Should_Revoke_Session()
    {
        // Arrange
        await EnableSessionManagement();
        LoginAsDefaultTenantAdmin();

        var userId = AbpSession.UserId!.Value;
        var tenantId = AbpSession.TenantId;

        var sessionToken = await _userSessionManager.CreateSessionAsync(
            userId, tenantId, "192.168.1.1", "Chrome/120.0");

        var sessions = await _userSessionManager.GetActiveSessionsAsync(userId, tenantId);
        var sessionId = sessions[0].Id;

        // Act
        await _userSessionAppService.RevokeSession(new Abp.Application.Services.Dto.EntityDto<long>(sessionId));

        // Assert
        var isValid = await _userSessionManager.ValidateSessionAsync(
            sessionToken, "192.168.1.1", "Chrome/120.0");
        isValid.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Throw_When_RevokeSession_And_SessionManagement_Disabled()
    {
        // Arrange - session management is disabled by default
        LoginAsDefaultTenantAdmin();

        // Act & Assert
        var exception = await Assert.ThrowsAnyAsync<Exception>(
            () => _userSessionAppService.RevokeSession(new Abp.Application.Services.Dto.EntityDto<long>(1))
        );

        exception.ShouldBeAssignableTo<AbpException>();
    }

    [Fact]
    public async Task Should_Revoke_All_Other_Sessions()
    {
        // Arrange
        await EnableSessionManagement();
        LoginAsDefaultTenantAdmin();

        var userId = AbpSession.UserId!.Value;
        var tenantId = AbpSession.TenantId;

        var currentToken = await _userSessionManager.CreateSessionAsync(
            userId, tenantId, "192.168.1.1", "Chrome/120.0");
        var otherToken = await _userSessionManager.CreateSessionAsync(
            userId, tenantId, "192.168.1.2", "Firefox/119.0");

        SetCurrentSessionToken(currentToken);

        // Act
        await _userSessionAppService.RevokeAllOtherSessions();

        // Assert - current session should be preserved, only the other one revoked
        var sessions = await _userSessionManager.GetActiveSessionsAsync(userId, tenantId);
        sessions.Count.ShouldBe(1);
        sessions[0].SessionToken.ShouldBe(currentToken);
    }

    [Fact]
    public async Task Should_Throw_When_RevokeAllOtherSessions_And_SessionManagement_Disabled()
    {
        // Arrange - session management is disabled by default
        LoginAsDefaultTenantAdmin();

        // Act & Assert
        var exception = await Assert.ThrowsAnyAsync<Exception>(
            () => _userSessionAppService.RevokeAllOtherSessions()
        );

        exception.ShouldBeAssignableTo<AbpException>();
    }

    private void SetCurrentSessionToken(string sessionToken)
    {
        var claims = new List<Claim>
        {
            new Claim(AppConsts.SessionTokenKey, sessionToken)
        };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = principal
        };

        var httpContextAccessor = Resolve<IHttpContextAccessor>();
        httpContextAccessor.HttpContext = httpContext;
    }
}
