namespace HCMv3.Integrations;

/// <summary>
/// What the badge on an Integration Center card says.
///
/// <see cref="Configured"/> versus <see cref="Enabled"/> is a real distinction, not cosmetic: credentials
/// can be saved and verified while the integration is deliberately switched off (a tenant preparing for
/// go-live, or an administrator pausing a misbehaving sync without discarding its settings).
/// </summary>
public enum IntegrationStatusType
{
    /// <summary>No settings saved yet -- the card shows a "Connect" button.</summary>
    NotConfigured = 0,

    /// <summary>Settings saved but the integration is switched off.</summary>
    Configured = 1,

    /// <summary>Settings saved and the integration is live.</summary>
    Enabled = 2,

    /// <summary>Settings saved, but the last connection test or sync failed.</summary>
    Error = 3
}
