namespace HCMv3.Integrations;

/// <summary>
/// Stable identifier for every integration shown in the Integration Center.
///
/// These strings are persisted (in <c>IntegrationSetting.IntegrationKey</c> and
/// <c>IntegrationConnectionLog.IntegrationKey</c>) and are quoted in support tickets, so they are
/// append-only: renaming one orphans a tenant's saved configuration.
/// </summary>
public static class IntegrationKeys
{
    /// <summary>Microsoft Entra ID (Azure AD) single sign-on.</summary>
    public const string AzureSso = "AzureSso";

    /// <summary>Outbound e-mail over SMTP.</summary>
    public const string Smtp = "Smtp";

    /// <summary>Dynamics 365 Finance &amp; Operations journal posting.</summary>
    public const string D365Finance = "D365Finance";

    /// <summary>BioTime-style time-attendance device/cloud API.</summary>
    public const string BioTime = "BioTime";

    /// <summary>Outbound webhooks (ABP webhook subscriptions).</summary>
    public const string Webhooks = "Webhooks";
}
