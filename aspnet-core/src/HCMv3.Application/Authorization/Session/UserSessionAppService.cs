using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Configuration;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Runtime.Session;
using Abp.UI;
using Microsoft.AspNetCore.Http;
using HCMv3.Authorization.Session.Dto;
using HCMv3.Configuration;

namespace HCMv3.Authorization.Session;

[AbpAuthorize(AppPermissions.Pages_Administration_ActiveSessions)]
public class UserSessionAppService : ApplicationService, IUserSessionAppService
{
    private readonly IRepository<UserSession, long> _userSessionRepository;
    private readonly IUserSessionManager _userSessionManager;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserSessionAppService(
        IRepository<UserSession, long> userSessionRepository,
        IUserSessionManager userSessionManager,
        IHttpContextAccessor httpContextAccessor)
    {
        _userSessionRepository = userSessionRepository;
        _userSessionManager = userSessionManager;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ListResultDto<UserSessionDto>> GetSessions(GetUserSessionsInput input)
    {
        await EnsureSessionManagementEnabledAsync();

        var userId = input.UserId ?? AbpSession.GetUserId();

        // If requesting another user's sessions, require admin permission
        if (input.UserId.HasValue && input.UserId.Value != AbpSession.GetUserId())
        {
            await PermissionChecker.AuthorizeAsync(AppPermissions.Pages_Administration_Users);
        }

        var sessions = await _userSessionManager.GetActiveSessionsAsync(userId, AbpSession.TenantId);

        var currentSessionToken = GetCurrentSessionToken();

        var dtos = sessions
            .OrderByDescending(s => s.LastActivityTime)
            .Select(s => new UserSessionDto
            {
                Id = s.Id,
                IpAddress = s.IpAddress,
                DeviceInfo = s.DeviceInfo,
                SignInTime = s.SignInTime,
                LastActivityTime = s.LastActivityTime,
                IsCurrent = s.SessionToken == currentSessionToken
            })
            .ToList();

        return new ListResultDto<UserSessionDto>(dtos);
    }

    public async Task RevokeSession(EntityDto<long> input)
    {
        await EnsureSessionManagementEnabledAsync();

        var isRevocationEnabled = await SettingManager.GetSettingValueAsync<bool>(
            AppSettings.UserManagement.SessionManagement.IsSessionRevocationEnabled);

        if (!isRevocationEnabled)
        {
            throw new UserFriendlyException(L("SessionRevocationIsDisabled"));
        }

        var session = await _userSessionRepository.GetAsync(input.Id);

        // Users can only revoke their own sessions, unless they are admin
        if (session.UserId != AbpSession.GetUserId())
        {
            await PermissionChecker.AuthorizeAsync(AppPermissions.Pages_Administration_Users);
        }

        session.Invalidate();
        await CurrentUnitOfWork.SaveChangesAsync();
    }

    public async Task RevokeAllOtherSessions()
    {
        await EnsureSessionManagementEnabledAsync();

        var isRevocationEnabled = await SettingManager.GetSettingValueAsync<bool>(
            AppSettings.UserManagement.SessionManagement.IsSessionRevocationEnabled);

        if (!isRevocationEnabled)
        {
            throw new UserFriendlyException(L("SessionRevocationIsDisabled"));
        }

        var currentSessionToken = GetCurrentSessionToken();

        await _userSessionManager.InvalidateAllSessionsAsync(
            AbpSession.GetUserId(),
            AbpSession.TenantId,
            currentSessionToken);
    }

    private async Task EnsureSessionManagementEnabledAsync()
    {
        if (!await _userSessionManager.IsSessionManagementEnabledAsync())
        {
            throw new UserFriendlyException(L("SessionManagementIsDisabled"));
        }
    }

    private string GetCurrentSessionToken()
    {
        return _httpContextAccessor.HttpContext?.User?.Claims
            .FirstOrDefault(c => c.Type == AppConsts.SessionTokenKey)?.Value;
    }
}
