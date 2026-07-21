using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Auditing;
using Abp.Configuration;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Microsoft.EntityFrameworkCore;
using HCMv3.Configuration;

namespace HCMv3.Authorization.Session;

public class UserSessionManager : IUserSessionManager, ITransientDependency
{
    private readonly IRepository<UserSession, long> _userSessionRepository;
    private readonly ISettingManager _settingManager;

    private const int LastActivityUpdateThresholdMinutes = 5;

    public UserSessionManager(
        IRepository<UserSession, long> userSessionRepository,
        ISettingManager settingManager)
    {
        _userSessionRepository = userSessionRepository;
        _settingManager = settingManager;
    }

    [UnitOfWork]
    public virtual async Task<string> CreateSessionAsync(long userId, int? tenantId, string ipAddress, string userAgent)
    {
        var deviceInfo = ParseDeviceInfo(userAgent);

        var session = new UserSession(userId, tenantId, ipAddress, userAgent, deviceInfo);

        // Recompute fingerprint based on the configured policy
        var policy = await GetFingerprintPolicyAsync();
        session.ClientFingerprint = UserSession.ComputeFingerprintByPolicy(ipAddress, userAgent, policy);

        await _userSessionRepository.InsertAsync(session);

        return session.SessionToken;
    }

    [UnitOfWork]
    [DisableAuditing]
    public virtual async Task<bool> ValidateSessionAsync(string sessionToken, string currentIpAddress, string currentUserAgent)
    {
        if (string.IsNullOrEmpty(sessionToken))
        {
            return false;
        }

        var session = await _userSessionRepository.FirstOrDefaultAsync(s =>
            s.SessionToken == sessionToken && s.IsActive);

        if (session == null)
        {
            return false;
        }

        var timeoutMinutes = await _settingManager.GetSettingValueAsync<int>(
            AppSettings.UserManagement.SessionManagement.SessionAbsoluteTimeoutMinutes);

        if (timeoutMinutes > 0 && session.SignInTime.AddMinutes(timeoutMinutes) < DateTime.UtcNow)
        {
            session.Invalidate();
            return false;
        }

        var isFingerprintEnabled = await _settingManager.GetSettingValueAsync<bool>(
            AppSettings.UserManagement.SessionManagement.IsSessionFingerprintValidationEnabled);

        if (isFingerprintEnabled)
        {
            var policy = await GetFingerprintPolicyAsync();
            var currentFingerprint = UserSession.ComputeFingerprintByPolicy(currentIpAddress, currentUserAgent, policy);

            if (!session.MatchesFingerprint(currentFingerprint))
            {
                return false;
            }
        }

        // Throttled last activity update
        if ((DateTime.UtcNow - session.LastActivityTime).TotalMinutes >= LastActivityUpdateThresholdMinutes)
        {
            session.UpdateLastActivity();
        }

        return true;
    }

    [UnitOfWork]
    public virtual async Task InvalidateSessionAsync(string sessionToken)
    {
        if (string.IsNullOrEmpty(sessionToken))
        {
            return;
        }

        var session = await _userSessionRepository.FirstOrDefaultAsync(s =>
            s.SessionToken == sessionToken && s.IsActive);

        session?.Invalidate();
    }

    [UnitOfWork]
    public virtual async Task InvalidateAllSessionsAsync(long userId, int? tenantId, string exceptSessionToken = null)
    {
        var query = _userSessionRepository.GetAll()
            .Where(s => s.UserId == userId && s.IsActive);

        if (!string.IsNullOrEmpty(exceptSessionToken))
        {
            query = query.Where(s => s.SessionToken != exceptSessionToken);
        }

        await query.ExecuteUpdateAsync(setters =>
            setters.SetProperty(s => s.IsActive, false));
    }

    [UnitOfWork]
    public virtual async Task<List<UserSession>> GetActiveSessionsAsync(long userId, int? tenantId)
    {
        return await _userSessionRepository.GetAllListAsync(s =>
            s.UserId == userId && s.IsActive);
    }

    [UnitOfWork]
    [DisableAuditing]
    public virtual async Task UpdateLastActivityAsync(string sessionToken)
    {
        if (string.IsNullOrEmpty(sessionToken))
        {
            return;
        }

        var session = await _userSessionRepository.FirstOrDefaultAsync(s =>
            s.SessionToken == sessionToken && s.IsActive);

        if (session != null &&
            (DateTime.UtcNow - session.LastActivityTime).TotalMinutes >= LastActivityUpdateThresholdMinutes)
        {
            session.UpdateLastActivity();
        }
    }

    [UnitOfWork]
    [DisableAuditing]
    public virtual async Task<bool> IsSessionManagementEnabledAsync()
    {
        return await _settingManager.GetSettingValueAsync<bool>(
            AppSettings.UserManagement.SessionManagement.IsEnabled);
    }

    private async Task<SessionFingerprintValidationPolicy> GetFingerprintPolicyAsync()
    {
        var policyValue = await _settingManager.GetSettingValueAsync(
            AppSettings.UserManagement.SessionManagement.SessionFingerprintValidationPolicy);

        if (Enum.TryParse<SessionFingerprintValidationPolicy>(policyValue, ignoreCase: true, out var policy))
        {
            return policy;
        }

        return SessionFingerprintValidationPolicy.IpAndUserAgent;
    }

    private static string ParseDeviceInfo(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
        {
            return "Unknown Device";
        }

        // Simple UA parsing for display purposes
        var browser = "Unknown Browser";
        var os = "Unknown OS";

        if (userAgent.Contains("Edg/", StringComparison.OrdinalIgnoreCase))
            browser = "Edge";
        else if (userAgent.Contains("Chrome/", StringComparison.OrdinalIgnoreCase))
            browser = "Chrome";
        else if (userAgent.Contains("Firefox/", StringComparison.OrdinalIgnoreCase))
            browser = "Firefox";
        else if (userAgent.Contains("Safari/", StringComparison.OrdinalIgnoreCase) && !userAgent.Contains("Chrome", StringComparison.OrdinalIgnoreCase))
            browser = "Safari";

        if (userAgent.Contains("Windows", StringComparison.OrdinalIgnoreCase))
            os = "Windows";
        else if (userAgent.Contains("Mac OS", StringComparison.OrdinalIgnoreCase))
            os = "macOS";
        else if (userAgent.Contains("Linux", StringComparison.OrdinalIgnoreCase))
            os = "Linux";
        else if (userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase))
            os = "Android";
        else if (userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase) || userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase))
            os = "iOS";

        return $"{browser} on {os}";
    }
}
