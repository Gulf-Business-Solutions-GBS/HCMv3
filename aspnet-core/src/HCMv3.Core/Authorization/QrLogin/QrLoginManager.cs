using System;
using System.Linq;
using System.Threading.Tasks;
using Abp;
using Abp.Runtime.Caching;
using Abp.UI;
using HCMv3.Authorization.Users;

namespace HCMv3.Authorization.QrLogin;

public class QrLoginManager : HCMv3DomainServiceBase, IQrLoginManager
{
    private readonly ICacheManager _cacheManager;
    private readonly UserManager _userManager;
    private readonly SignalRQrLoginCommunicator _signalRQrLoginCommunicator;

    public QrLoginManager(
        ICacheManager cacheManager,
        UserManager userManager,
        SignalRQrLoginCommunicator signalRQrLoginCommunicator)
    {
        _cacheManager = cacheManager;
        _userManager = userManager;
        _signalRQrLoginCommunicator = signalRQrLoginCommunicator;
    }

    public async Task<bool> VerifySessionId(string connectionId, string sessionId)
    {
        var cache = await _cacheManager.GetQrLoginSessionIdCacheItem().GetOrDefaultAsync(connectionId);

        if (cache == null)
        {
            throw new UserFriendlyException(L("QrLoginConnectionIdNotFoundCache"));
        }

        if (cache.Code != sessionId)
        {
            throw new UserFriendlyException(L("WrongQrLoginSessionId"));
        }

        return true;
    }

    public async Task SendAuthData(string connectionId, QrLoginAuthenticateResultModel model)
    {
        await _signalRQrLoginCommunicator.SendAuthDataToClient(connectionId, model);
    }

    public async Task<string> GenerateSessionId(string connectionId)
    {
        var sessionId = Guid.NewGuid().ToString("N");

        // Remove old cache
        await RemoveQrLoginCache(connectionId);

        var cacheItem = new QrLoginSessionIdCacheItem(sessionId);

        await _cacheManager.GetQrLoginSessionIdCacheItem().SetAsync(connectionId, cacheItem);

        return sessionId;
    }

    public async Task RemoveQrLoginCache(string connectionId)
    {
        await _cacheManager.GetQrLoginSessionIdCacheItem().RemoveAsync(
            connectionId
        );
    }

    public Task<User> GetUserByUserIdentifierClaimAsync(UserIdentifier userIdentifier)
    {
        var user = _userManager.GetUserByIdAsync(userIdentifier.UserId);

        if (user == null)
        {
            throw new UserFriendlyException(L("UserNotFound"));
        }

        return user;
    }
}