using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using HCMv3.WebHooks.Dto;

namespace HCMv3.WebHooks;

public interface IWebhookAttemptAppService
{
    Task<PagedResultDto<GetAllSendAttemptsOutput>> GetAllSendAttempts(GetAllSendAttemptsInput input);

    Task<ListResultDto<GetAllSendAttemptsOfWebhookEventOutput>> GetAllSendAttemptsOfWebhookEvent(GetAllSendAttemptsOfWebhookEventInput input);
}

