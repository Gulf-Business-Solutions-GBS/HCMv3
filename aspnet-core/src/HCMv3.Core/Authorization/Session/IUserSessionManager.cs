using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Domain.Services;

namespace HCMv3.Authorization.Session;

public interface IUserSessionManager : IDomainService
{
    Task<string> CreateSessionAsync(long userId, int? tenantId, string ipAddress, string userAgent);

    Task<bool> ValidateSessionAsync(string sessionToken, string currentIpAddress, string currentUserAgent);

    Task InvalidateSessionAsync(string sessionToken);

    Task InvalidateAllSessionsAsync(long userId, int? tenantId, string exceptSessionToken = null);

    Task<List<UserSession>> GetActiveSessionsAsync(long userId, int? tenantId);

    Task UpdateLastActivityAsync(string sessionToken);

    Task<bool> IsSessionManagementEnabledAsync();
}
