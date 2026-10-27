using Abp.Mapperly;
using Abp.Webhooks;
using HCMv3.WebHooks.Dto;
using Riok.Mapperly.Abstractions;

namespace HCMv3.Mappers;

[Mapper]
public partial class WebhookSubscriptionToGetAllSubscriptionsOutputMapper : MapperBase<WebhookSubscription, GetAllSubscriptionsOutput>
{
    public override partial GetAllSubscriptionsOutput Map(WebhookSubscription source);
    public override partial void Map(WebhookSubscription source, GetAllSubscriptionsOutput destination);
}

[Mapper]
public partial class WebhookSendAttemptToGetAllSendAttemptsOutputMapper : MapperBase<WebhookSendAttempt, GetAllSendAttemptsOutput>
{
    public override GetAllSendAttemptsOutput Map(WebhookSendAttempt source)
    {
        var dto = MapInternal(source);
        dto.WebhookName = source.WebhookEvent?.WebhookName;
        dto.Data = source.WebhookEvent?.Data;
        return dto;
    }

    public override void Map(WebhookSendAttempt source, GetAllSendAttemptsOutput destination)
    {
        MapInternal(source, destination);
        destination.WebhookName = source.WebhookEvent?.WebhookName;
        destination.Data = source.WebhookEvent?.Data;
    }

    [MapperIgnoreTarget(nameof(GetAllSendAttemptsOutput.WebhookName))]
    [MapperIgnoreTarget(nameof(GetAllSendAttemptsOutput.Data))]
    private partial GetAllSendAttemptsOutput MapInternal(WebhookSendAttempt source);

    [MapperIgnoreTarget(nameof(GetAllSendAttemptsOutput.WebhookName))]
    [MapperIgnoreTarget(nameof(GetAllSendAttemptsOutput.Data))]
    private partial void MapInternal(WebhookSendAttempt source, GetAllSendAttemptsOutput destination);
}

[Mapper]
public partial class WebhookSendAttemptToGetAllSendAttemptsOfWebhookEventOutputMapper : MapperBase<WebhookSendAttempt, GetAllSendAttemptsOfWebhookEventOutput>
{
    public override partial GetAllSendAttemptsOfWebhookEventOutput Map(WebhookSendAttempt source);
    public override partial void Map(WebhookSendAttempt source, GetAllSendAttemptsOfWebhookEventOutput destination);
}
