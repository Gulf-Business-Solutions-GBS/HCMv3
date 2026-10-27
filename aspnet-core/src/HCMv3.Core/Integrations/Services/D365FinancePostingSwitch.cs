using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.Integrations.Connectors;

namespace HCMv3.Integrations.Services;

/// <inheritdoc cref="ID365FinancePostingSwitch"/>
/// <remarks>
/// <para><b>The three cases.</b></para>
/// <list type="number">
/// <item><description>The switch has been set: on means post, off means refuse. This is what the
/// Integration Center card shows.</description></item>
/// <item><description>Never set, but settings were entered in the Integration Center: refuse. The card shows
/// such a tenant as "Set up, switched off", and posting has to agree with what the card says.</description></item>
/// <item><description>Never set and nothing stored in the Integration Center at all: allow. That is a tenant
/// configured before the Integration Center existed, whose credentials still live in <c>SysTenantConfig</c>
/// (see <see cref="D365FinanceConfigReader"/>). Refusing it would stop a working integration the day this
/// shipped. Switching the card off writes "false", which then refuses it as well.</description></item>
/// </list>
/// </remarks>
public class D365FinancePostingSwitch : HCMv3DomainServiceBase, ID365FinancePostingSwitch
{
    private readonly IIntegrationSettingStore _store;

    public D365FinancePostingSwitch(IIntegrationSettingStore store)
    {
        _store = store;
    }

    public async Task<bool> IsPostingAllowedAsync()
    {
        var flag = await _store.GetAsync(IntegrationKeys.D365Finance, StoreBackedIntegrationConnectorBase.EnabledSettingKey);

        if (string.Equals(flag, "true", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(flag, "false", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var stored = await _store.GetKeysWithValueAsync(IntegrationKeys.D365Finance);

        return !(stored ?? Array.Empty<string>())
            .Any(k => !string.Equals(k, StoreBackedIntegrationConnectorBase.EnabledSettingKey, StringComparison.Ordinal));
    }
}
