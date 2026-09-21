using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.Integrations.Dto;

/// <summary>
/// Submitted configuration for one integration.
///
/// SECRET SEMANTICS: omitting a secret field, or sending it blank, keeps whatever is already stored. The
/// form never receives the secret in the first place, so treating blank as "clear it" would wipe the
/// credential every time an administrator edited an unrelated field.
/// </summary>
public class SaveIntegrationSettingsInput
{
    [Required]
    [StringLength(64)]
    public string Key { get; set; }

    public Dictionary<string, string> Values { get; set; }
}
