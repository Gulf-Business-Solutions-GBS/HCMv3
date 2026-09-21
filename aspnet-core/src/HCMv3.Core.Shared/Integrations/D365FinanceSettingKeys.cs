namespace HCMv3.Integrations;

/// <summary>
/// Setting keys for the Dynamics 365 Finance &amp; Operations integration.
///
/// These are the legacy <c>SysTenantConfig</c> key names, read verbatim at legacy
/// <c>uscCostingFile.ascx.cs</c> lines 1102-1111. They keep their original spelling so that a tenant's
/// existing values migrate across unchanged; only their storage moves, from the general-purpose
/// <c>SysTenantConfig</c> table into <c>IntegrationSetting</c>, where <see cref="ClientSecret"/> is
/// encrypted at rest and never returned to the browser.
///
/// They live in Core.Shared rather than next to the posting client because both the Application-layer
/// client and the Core-layer Integration Center connector need them, and Core cannot reference Application.
/// </summary>
public static class D365FinanceSettingKeys
{
    public const string RequestUrl = "REQUEST_URL";
    public const string ClientId = "CLIENT_ID";
    public const string GrantType = "GRANT_TYPE";
    public const string Resource = "RESOURCE";
    public const string ClientSecret = "CLIENT_SECRET";
    public const string ChangesetLimit = "CHANGESET_LIMIT";
    public const string RequestUrlJournalHeader = "REQUEST_URL_JOURNAL_HEADER";
    public const string RequestUrlJournalLine = "REQUEST_URL_JOURNAL_LINE";
    public const string JournalHeaderODataETag = "JOURNAL_HEADER_ODATA_ETAG";

    /// <summary>Every key above, in the order the configuration form shows them.</summary>
    public static readonly string[] All =
    {
        RequestUrl,
        ClientId,
        GrantType,
        Resource,
        ClientSecret,
        ChangesetLimit,
        RequestUrlJournalHeader,
        RequestUrlJournalLine,
        JournalHeaderODataETag
    };
}
