using System.Threading.Tasks;

namespace HCMv3.Authorization.Accounts;

public class ProxyTokenAuthControllerService : ProxyControllerBase
{
    public async Task SendTwoFactorAuthCode(long userId, string provider)
    {
        await ApiClient
            .PostAsync("api/" + GetEndpoint(nameof(SendTwoFactorAuthCode)), new { UserId = userId, Provider = provider });
    }

    public async Task AuthenticateWithQrCode(string connectionId, string sessionId)
    {
        await ApiClient
            .PostAsync("api/" + GetEndpoint(nameof(AuthenticateWithQrCode)), new { ConnectionId = connectionId, SessionId = sessionId });
    }
}

