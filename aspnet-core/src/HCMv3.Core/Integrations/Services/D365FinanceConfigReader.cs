using System.Threading.Tasks;
using HCMv3.SystemSettings.Services;

namespace HCMv3.Integrations.Services;

/// <inheritdoc cref="ID365FinanceConfigReader"/>
/// <remarks>
/// The Integration Center is authoritative; <c>SysTenantConfig</c> is the fallback for tenants whose
/// settings have not been moved across yet.
///
/// THE ORDER MATTERS. Reading the legacy table first would mean that a tenant who has just re-entered
/// their credentials in the Integration Center keeps posting with the old ones, which is the kind of
/// fault that shows up as a finance reconciliation problem weeks later rather than as an error.
///
/// The fallback is deliberately temporary: once the one-off migration has moved every tenant's keys into
/// <see cref="IntegrationSetting"/> and removed the originals, this class should read one store and the
/// <c>SysTenantConfig</c> dependency should go.
/// </remarks>
public class D365FinanceConfigReader : HCMv3DomainServiceBase, ID365FinanceConfigReader
{
    private readonly IIntegrationSettingStore _store;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;

    public D365FinanceConfigReader(
        IIntegrationSettingStore store,
        ISysSettingsAccessor sysSettingsAccessor)
    {
        _store = store;
        _sysSettingsAccessor = sysSettingsAccessor;
    }

    public async Task<string> GetAsync(string settingKey)
    {
        var current = await _store.GetAsync(IntegrationKeys.D365Finance, settingKey);
        if (!string.IsNullOrWhiteSpace(current))
        {
            return current;
        }

        // GetSysTenantConfigAsync returns "" rather than null when there is no row, so blank has to count
        // as "not found" here or every caller would see an empty string instead of falling through.
        var legacy = await _sysSettingsAccessor.GetSysTenantConfigAsync(settingKey);

        return string.IsNullOrWhiteSpace(legacy) ? null : legacy;
    }
}
