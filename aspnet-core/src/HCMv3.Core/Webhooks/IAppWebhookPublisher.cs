using System.Threading.Tasks;
using HCMv3.Authorization.Users;

namespace HCMv3.WebHooks;

public interface IAppWebhookPublisher
{
    Task PublishTestWebhook();
}

