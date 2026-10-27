using System.Threading.Tasks;
using Abp.Webhooks;

namespace HCMv3.WebHooks;

public interface IWebhookEventAppService
{
    Task<WebhookEvent> Get(string id);
}

