using System.Threading.Tasks;
using Abp.Domain.Services;

namespace HCMv3.Integrations.Services;

/// <summary>
/// Where the Dynamics 365 Finance settings come from.
///
/// This exists because those settings are mid-move. The Integration Center writes them to
/// <see cref="IntegrationSetting"/>, where the client secret is encrypted and out of reach of the general
/// configuration grid; tenants configured before the Integration Center existed still have them as plain
/// rows in <c>SysTenantConfig</c>. Both the journal posting and the connection test have to work for
/// either kind of tenant, so the lookup order lives in one place rather than being written twice and
/// drifting.
/// </summary>
public interface ID365FinanceConfigReader : IDomainService
{
    /// <summary>
    /// One setting from <see cref="D365FinanceSettingKeys"/>, or <c>null</c> when neither store has it.
    /// </summary>
    Task<string> GetAsync(string settingKey);
}
