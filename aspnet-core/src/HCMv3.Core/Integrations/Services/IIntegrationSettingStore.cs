using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Domain.Services;

namespace HCMv3.Integrations.Services;

/// <summary>
/// Reads and writes <see cref="IntegrationSetting"/> rows, encrypting the ones marked secret.
///
/// Callers work in plain text throughout; whether a value is encrypted on disk is this store's business
/// alone.
/// </summary>
public interface IIntegrationSettingStore : IDomainService
{
    /// <summary>Plain-text value, or <c>null</c> when nothing is stored.</summary>
    Task<string> GetAsync(string integrationKey, string settingKey);

    /// <summary>Every stored field for one integration, decrypted, keyed by setting key.</summary>
    Task<IReadOnlyDictionary<string, string>> GetAllAsync(string integrationKey);

    /// <summary>
    /// Which fields have a non-empty stored value. Lets the configuration form show "a secret is set"
    /// without the secret itself crossing to the browser.
    /// </summary>
    Task<IReadOnlyCollection<string>> GetKeysWithValueAsync(string integrationKey);

    /// <summary>
    /// Stores one value, encrypting it when <paramref name="isSecret"/> is set. A null or blank
    /// <paramref name="value"/> removes the row.
    /// </summary>
    Task SetAsync(string integrationKey, string settingKey, string value, bool isSecret);

    Task RemoveAllAsync(string integrationKey);
}
