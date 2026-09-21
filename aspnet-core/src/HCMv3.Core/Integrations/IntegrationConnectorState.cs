namespace HCMv3.Integrations;

/// <summary>
/// What a connector knows about itself. Deliberately narrow: the overall
/// <see cref="IntegrationStatusType"/> shown on the card, and the last test/sync timestamps, are derived
/// centrally from this plus <see cref="IntegrationConnectionLog"/>, so that the five connectors cannot
/// drift in how they describe the same three states.
/// </summary>
public class IntegrationConnectorState
{
    /// <summary>Whether enough settings are stored for the integration to run at all.</summary>
    public bool IsConfigured { get; set; }

    /// <summary>
    /// Whether it is switched on. For connectors whose <c>SupportsEnableToggle</c> is false this simply
    /// mirrors <see cref="IsConfigured"/>.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Optional extra line for the card, already localized -- e.g. "3 endpoints active" for webhooks.
    /// </summary>
    public string Detail { get; set; }
}
