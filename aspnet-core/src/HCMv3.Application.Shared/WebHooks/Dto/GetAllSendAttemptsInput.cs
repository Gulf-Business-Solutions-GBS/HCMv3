using HCMv3.Dto;

namespace HCMv3.WebHooks.Dto;

public class GetAllSendAttemptsInput : PagedInputDto
{
    public string SubscriptionId { get; set; }
}

