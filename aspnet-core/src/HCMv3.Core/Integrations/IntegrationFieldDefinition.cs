namespace HCMv3.Integrations;

/// <summary>
/// Describes one configuration field so the Integration Center can render a form for it without any
/// integration-specific frontend code. Adding an integration is then a backend-only change.
/// </summary>
/// <param name="Key">Field name as stored, e.g. <c>CLIENT_SECRET</c>.</param>
/// <param name="LabelKey">Localization key for the field label.</param>
/// <param name="Type">How the field renders.</param>
/// <param name="IsRequired">Whether the connector refuses to save without it.</param>
/// <param name="IsSecret">
/// Whether the value is encrypted at rest and withheld from the browser. A secret field's current value
/// is never sent to the client; the form shows a masked placeholder and an empty submission means
/// "leave the stored value alone".
/// </param>
/// <param name="HelpKey">Optional localization key for hint text under the field.</param>
/// <param name="DefaultValue">Optional value pre-filled when nothing is stored yet.</param>
public record IntegrationFieldDefinition(
    string Key,
    string LabelKey,
    IntegrationFieldType Type,
    bool IsRequired,
    bool IsSecret,
    string HelpKey = null,
    string DefaultValue = null);
