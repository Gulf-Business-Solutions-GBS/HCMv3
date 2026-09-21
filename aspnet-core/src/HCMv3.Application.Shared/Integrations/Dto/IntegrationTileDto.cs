using System;

namespace HCMv3.Integrations.Dto;

/// <summary>
/// One card on the Integration Center.
///
/// <see cref="NameKey"/> and <see cref="DescriptionKey"/> are localization keys, not text: the client
/// resolves them through the same localization source, so the card follows the user's language without a
/// round trip. <see cref="StatusDetail"/> is the exception -- it carries counts that have to be formatted
/// server-side, so it arrives already localized.
/// </summary>
public class IntegrationTileDto
{
    public string Key { get; set; }

    public string NameKey { get; set; }

    public string DescriptionKey { get; set; }

    public IntegrationCategory Category { get; set; }

    public IntegrationStatusType Status { get; set; }

    public bool IsConfigured { get; set; }

    public bool IsEnabled { get; set; }

    /// <summary>Already localized. Null when the integration has nothing extra to report.</summary>
    public string StatusDetail { get; set; }

    public bool SupportsTest { get; set; }

    public bool SupportsEnableToggle { get; set; }

    /// <summary>
    /// Non-null when this integration may only be enabled while its group siblings are off. The client
    /// uses it to present the group as a choice rather than as independent switches.
    /// </summary>
    public string ExclusiveGroup { get; set; }

    /// <summary>Route to a dedicated screen, when one exists.</summary>
    public string ExternalManageRoute { get; set; }

    /// <summary>
    /// False when the integration has no field form -- the Configure button then navigates to
    /// <see cref="ExternalManageRoute"/> instead of opening the modal.
    /// </summary>
    public bool HasFields { get; set; }

    public DateTime? LastTestTime { get; set; }

    public bool? LastTestSucceeded { get; set; }

    public string LastTestMessage { get; set; }

    public DateTime? LastSyncTime { get; set; }

    public bool? LastSyncSucceeded { get; set; }

    public int? LastSyncItemCount { get; set; }
}
