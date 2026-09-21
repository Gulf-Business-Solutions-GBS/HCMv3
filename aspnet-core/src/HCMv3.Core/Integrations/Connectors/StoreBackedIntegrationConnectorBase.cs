using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.Integrations.Services;

namespace HCMv3.Integrations.Connectors;

/// <summary>
/// Base class for integrations whose configuration lives in <see cref="IntegrationSetting"/> -- the
/// default for everything added from the Integration Center onwards. A subclass supplies its identity,
/// its field list and a real connection test; the field handling, secret semantics and enable flag are
/// all inherited.
///
/// Integrations whose settings are owned by the framework (SMTP, Azure SSO, webhooks) implement
/// <see cref="IIntegrationConnector"/> directly instead and write through to their own storage.
/// </summary>
public abstract class StoreBackedIntegrationConnectorBase : HCMv3DomainServiceBase, IIntegrationConnector
{
    /// <summary>
    /// Reserved key holding the on/off flag. Double-underscored so it cannot collide with a real field
    /// name from an external vendor's configuration.
    /// </summary>
    public const string EnabledSettingKey = "__ENABLED";

    protected readonly IIntegrationSettingStore Store;

    protected StoreBackedIntegrationConnectorBase(IIntegrationSettingStore store)
    {
        Store = store;
    }

    public abstract string Key { get; }

    public abstract IntegrationCategory Category { get; }

    public abstract string NameKey { get; }

    public abstract string DescriptionKey { get; }

    public abstract IReadOnlyList<IntegrationFieldDefinition> Fields { get; }

    public virtual bool SupportsTest => true;

    public virtual string ExclusiveGroup => null;

    public virtual bool SupportsEnableToggle => true;

    public virtual string ExternalManageRoute => null;

    public abstract Task<IntegrationTestResult> TestConnectionAsync();

    public virtual async Task<IntegrationConnectorState> GetStateAsync()
    {
        var present = await Store.GetKeysWithValueAsync(Key);
        var isConfigured = Fields
            .Where(f => f.IsRequired)
            .All(f => present.Contains(f.Key));

        var enabledRaw = await Store.GetAsync(Key, EnabledSettingKey);

        return new IntegrationConnectorState
        {
            IsConfigured = isConfigured,
            IsEnabled = isConfigured && string.Equals(enabledRaw, "true", System.StringComparison.OrdinalIgnoreCase)
        };
    }

    public virtual async Task<IReadOnlyList<IntegrationFieldValue>> GetValuesAsync()
    {
        var stored = await Store.GetAllAsync(Key);

        return Fields
            .Select(f =>
            {
                var hasValue = stored.TryGetValue(f.Key, out var value) && !string.IsNullOrWhiteSpace(value);

                // A secret's value never crosses to the browser -- only the fact that one is stored.
                return new IntegrationFieldValue(f.Key, f.IsSecret ? null : value, hasValue);
            })
            .ToList();
    }

    public virtual async Task SaveValuesAsync(IReadOnlyDictionary<string, string> values)
    {
        string Submitted(IntegrationFieldDefinition field) =>
            values != null && values.TryGetValue(field.Key, out var raw) ? raw : null;

        // A secret submitted blank means "keep the stored one". The form never receives the secret, so
        // treating blank as a clear would wipe the credential on every unrelated edit.
        bool KeepsStoredSecret(IntegrationFieldDefinition field) =>
            field.IsSecret && string.IsNullOrWhiteSpace(Submitted(field));

        // Validate BEFORE writing anything, against what the save would leave behind. Checking afterwards
        // by re-reading the table does not work inside one unit of work -- the inserts are still pending,
        // so the query cannot see them -- and it would also leave a rejected save half applied.
        var alreadyStored = await Store.GetKeysWithValueAsync(Key);

        var missing = Fields.FirstOrDefault(f =>
            f.IsRequired &&
            string.IsNullOrWhiteSpace(Submitted(f)) &&
            !(KeepsStoredSecret(f) && alreadyStored.Contains(f.Key)));

        if (missing != null)
        {
            throw new UserFriendlyException(L("IntegrationRequiredFieldMissing", L(missing.LabelKey)));
        }

        foreach (var field in Fields)
        {
            if (KeepsStoredSecret(field))
            {
                continue;
            }

            await Store.SetAsync(Key, field.Key, Submitted(field), field.IsSecret);
        }
    }

    public virtual async Task SetEnabledAsync(bool enabled)
    {
        if (enabled)
        {
            await GuardRequiredFieldsAsync();
        }

        await Store.SetAsync(Key, EnabledSettingKey, enabled ? "true" : "false", isSecret: false);
    }

    /// <summary>
    /// Refuses a half-configured integration. Checked after saving rather than before, so that a partial
    /// edit is reported against what the tenant would actually end up with.
    /// </summary>
    protected virtual async Task GuardRequiredFieldsAsync()
    {
        var present = await Store.GetKeysWithValueAsync(Key);

        var missing = Fields.FirstOrDefault(f => f.IsRequired && !present.Contains(f.Key));
        if (missing != null)
        {
            throw new UserFriendlyException(L("IntegrationRequiredFieldMissing", L(missing.LabelKey)));
        }
    }
}
