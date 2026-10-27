namespace HCMv3.Integrations;

/// <summary>
/// One field's currently stored value, as handed to the configuration form.
/// </summary>
/// <param name="Key">Matches <see cref="IntegrationFieldDefinition.Key"/>.</param>
/// <param name="Value">
/// The stored value, or <c>null</c> for a secret field -- secrets never leave the server. Use
/// <paramref name="HasStoredValue"/> to tell "not set" apart from "set but withheld".
/// </param>
/// <param name="HasStoredValue">Whether anything is stored for this field.</param>
public record IntegrationFieldValue(string Key, string Value, bool HasStoredValue);
