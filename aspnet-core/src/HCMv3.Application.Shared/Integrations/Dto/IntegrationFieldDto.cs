namespace HCMv3.Integrations.Dto;

/// <summary>
/// One field of an integration's configuration form, with its current value.
///
/// A secret field always arrives with a null <see cref="Value"/> -- credentials do not travel to the
/// browser. <see cref="HasStoredValue"/> is how the form knows whether to show a masked placeholder
/// ("a password is saved") or an empty box ("nothing saved yet").
/// </summary>
public class IntegrationFieldDto
{
    public string Key { get; set; }

    /// <summary>Localization key for the label; the client resolves it.</summary>
    public string LabelKey { get; set; }

    /// <summary>Localization key for hint text under the field, or null.</summary>
    public string HelpKey { get; set; }

    public IntegrationFieldType Type { get; set; }

    public bool IsRequired { get; set; }

    public bool IsSecret { get; set; }

    /// <summary>Null for a secret field, whatever is stored otherwise.</summary>
    public string Value { get; set; }

    public bool HasStoredValue { get; set; }

    /// <summary>Pre-filled when nothing is stored yet, so a form starts with sensible values.</summary>
    public string DefaultValue { get; set; }
}
