using System.Threading.Tasks;
using Abp.Dependency;
using Microsoft.AspNetCore.SignalR;

namespace HCMv3.Authorization.QrLogin;

public class SignalRQrLoginCommunicator : ITransientDependency
{
    private readonly IHubContext<QrLoginHub> _chatHub;

    public SignalRQrLoginCommunicator(
        IHubContext<QrLoginHub> chatHub)
    {
        _chatHub = chatHub;
    }

    public async Task SendAuthDataToClient(string connectionId, QrLoginAuthenticateResultModel model)
    {
        await _chatHub.Clients.Client(connectionId).SendAsync("getAuthData", model);
    }
}