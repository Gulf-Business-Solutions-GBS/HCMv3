using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Runtime.Session;
using Abp.Webhooks;
using HCMv3.Integrations.Services;

namespace HCMv3.Integrations.Connectors;

/// <summary>
/// Outbound webhooks: HCMv3 calling other systems the moment something happens, instead of those systems
/// polling for changes.
///
/// NO FIELDS. A webhook configuration is a list of endpoints, each with its own secret and its own set of
/// subscribed events -- one-to-many, which no flat field list can express. The card therefore carries no
/// form; its Configure button opens the existing Webhook Subscriptions screen, which already handles
/// subscriptions, retries, the send-attempt log and auto-deactivation of a failing endpoint.
///
/// The card still earns its place: it is how an administrator discovers that outbound webhooks exist at
/// all, and it reports how many endpoints are live without their having to go and look.
/// </summary>
public class WebhooksConnector : HCMv3DomainServiceBase, IIntegrationConnector
{
    private readonly IWebhookSubscriptionManager _webhookSubscriptionManager;
    private readonly IAbpSession _abpSession;

    public WebhooksConnector(IWebhookSubscriptionManager webhookSubscriptionManager, IAbpSession abpSession)
    {
        _webhookSubscriptionManager = webhookSubscriptionManager;
        _abpSession = abpSession;
    }

    public string Key => IntegrationKeys.Webhooks;

    public IntegrationCategory Category => IntegrationCategory.Outbound;

    public string NameKey => "Integration_Webhooks";

    public string DescriptionKey => "Integration_Webhooks_Description";

    public bool SupportsTest => false;

    public string ExclusiveGroup => null;

    /// <summary>
    /// Webhooks are activated per subscription, on their own screen. A single card-level switch would
    /// have to mean "deactivate every endpoint", which is not something to offer behind one click.
    /// </summary>
    public bool SupportsEnableToggle => false;

    public string ExternalManageRoute => "/app/admin/webhook-subscriptions";

    public IReadOnlyList<IntegrationFieldDefinition> Fields { get; } = new List<IntegrationFieldDefinition>();

    public async Task<IntegrationConnectorState> GetStateAsync()
    {
        var subscriptions = await _webhookSubscriptionManager.GetAllSubscriptionsAsync(_abpSession.TenantId);

        var total = subscriptions?.Count ?? 0;
        var active = subscriptions?.Count(s => s.IsActive) ?? 0;

        return new IntegrationConnectorState
        {
            IsConfigured = total > 0,
            IsEnabled = active > 0,
            Detail = total == 0 ? null : L("Integration_Webhooks_EndpointCount", active, total)
        };
    }

    public Task<IReadOnlyList<IntegrationFieldValue>> GetValuesAsync()
    {
        return Task.FromResult((IReadOnlyList<IntegrationFieldValue>)new List<IntegrationFieldValue>());
    }

    public Task SaveValuesAsync(IReadOnlyDictionary<string, string> values)
    {
        throw new NotSupportedException(
            "Webhook subscriptions are edited on their own screen; this connector exposes no fields.");
    }

    public Task<IntegrationTestResult> TestConnectionAsync()
    {
        throw new NotSupportedException(
            "There is no single endpoint to test; each subscription is tested from the Webhook Subscriptions screen.");
    }

    public Task SetEnabledAsync(bool enabled)
    {
        throw new NotSupportedException(
            "Webhooks are activated per subscription, not as a whole.");
    }
}
