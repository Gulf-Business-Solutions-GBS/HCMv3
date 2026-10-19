using System.Collections.Generic;
using Abp.Configuration;

namespace HCMv3.HcmNotifications;

/// <summary>
/// Defines the ABP settings HCM notifications keep. A separate provider so Zero's template
/// <c>AppSettingProvider</c> stays untouched; added by <see cref="HCMv3HcmNotificationsModule"/>.
/// </summary>
public class HcmNotificationSettingProvider : SettingProvider
{
    public override IEnumerable<SettingDefinition> GetSettingDefinitions(SettingDefinitionProviderContext context)
    {
        return new[]
        {
            // Internal bookkeeping, never edited by hand and never sent to the browser.
            new SettingDefinition(
                HcmNotificationSettingNames.SubscribedDefinitions,
                string.Empty,
                scopes: SettingScopes.Tenant,
                isVisibleToClients: false)
        };
    }
}
