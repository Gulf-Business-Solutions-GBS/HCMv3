using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Domain.Services;

namespace HCMv3.Integrations.Services;

/// <summary>
/// One integration, as the Integration Center sees it.
///
/// THE POINT OF THIS INTERFACE is that it says nothing about where configuration is stored. HCMv3's
/// integrations keep their settings in four different places -- ABP's <c>SettingManager</c> (SMTP, Azure
/// SSO), ABP's webhook subscription tables, the <c>TimeAttAPI</c> entity (BioTime) and
/// <see cref="IntegrationSetting"/> (Dynamics 365, and everything added from now on). Three of those are
/// framework-owned and cannot be moved without fighting ABP on every upgrade. A uniform *interface* gives
/// the single pane of glass that uniform *storage* was supposed to buy, and costs no data migration of
/// live credentials.
///
/// Implementations are domain services, so they are discovered and registered automatically.
/// </summary>
public interface IIntegrationConnector : IDomainService
{
    /// <summary>One of the <see cref="IntegrationKeys"/> constants.</summary>
    string Key { get; }

    IntegrationCategory Category { get; }

    /// <summary>Localization key for the card title.</summary>
    string NameKey { get; }

    /// <summary>Localization key for the card's descriptive blurb.</summary>
    string DescriptionKey { get; }

    /// <summary>
    /// The configuration form, in display order. Empty when
    /// <see cref="ExternalManageRoute"/> is set.
    /// </summary>
    IReadOnlyList<IntegrationFieldDefinition> Fields { get; }

    /// <summary>Whether the card offers a "Test Connection" button.</summary>
    bool SupportsTest { get; }

    /// <summary>
    /// Name of a mutually exclusive group this integration belongs to (see
    /// <see cref="IntegrationExclusiveGroups"/>), or <c>null</c> when it can run alongside anything else.
    /// Enabling a member of a group switches its siblings off in the same transaction.
    ///
    /// The case this exists for is the assistant's model provider: Azure OpenAI, Bedrock and a
    /// self-hosted Qwen are interchangeable answers to one question, and only one can be the answer.
    /// Grouping them rather than merging them into a single connector with a provider dropdown keeps each
    /// provider's credentials saved while it is switched off, so changing back is a toggle instead of a
    /// re-entry of keys.
    /// </summary>
    string ExclusiveGroup { get; }

    /// <summary>
    /// Whether the card offers an on/off switch. False where the concept does not apply -- webhooks are
    /// enabled per subscription, not globally.
    /// </summary>
    bool SupportsEnableToggle { get; }

    /// <summary>
    /// Angular route to an existing dedicated screen for this integration, when one exists.
    ///
    /// When <see cref="Fields"/> is empty the card's Configure button navigates there instead of opening
    /// the generic modal -- webhooks are configured per subscription, which no flat field list could
    /// express. When fields are present the modal shows it as a "more settings" link, for configuration
    /// that is domain data rather than connection data: BioTime's punch-code mapping resolves to TransType
    /// records and belongs on its own screen, not in a credentials form.
    /// </summary>
    string ExternalManageRoute { get; }

    Task<IntegrationConnectorState> GetStateAsync();

    /// <summary>
    /// Current values for <see cref="Fields"/>. Secret fields come back with a null value and
    /// <c>HasStoredValue</c> telling the form whether to show a masked placeholder.
    /// </summary>
    Task<IReadOnlyList<IntegrationFieldValue>> GetValuesAsync();

    /// <summary>
    /// Saves the submitted fields.
    ///
    /// SECRET SEMANTICS: a secret field that is absent from <paramref name="values"/>, or present but
    /// blank, means "keep what is stored" -- not "clear it". Without this rule every save from a form
    /// that (correctly) never received the secret would wipe the credential.
    /// </summary>
    Task SaveValuesAsync(IReadOnlyDictionary<string, string> values);

    /// <summary>
    /// Reaches the external system for real. Implementations return a failure result rather than throwing
    /// for expected problems (bad credentials, unreachable host); the caller logs either outcome.
    /// </summary>
    Task<IntegrationTestResult> TestConnectionAsync();

    /// <summary>
    /// Switches the integration on or off. Implementations must write the flag the runtime actually
    /// reads -- a toggle that only changes what the card displays, while a sync job keeps running, is
    /// worse than no toggle at all.
    /// </summary>
    Task SetEnabledAsync(bool enabled);
}
