using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using HCMv3.Integrations.Services;

namespace HCMv3.Integrations.Connectors;

/// <summary>
/// Dynamics 365 Finance &amp; Operations. The posting itself stays in
/// <c>D365FinancePostingClient</c>; this connector owns only the configuration and the connection test.
///
/// Its settings moved out of the general-purpose <c>SysTenantConfig</c> table because they were being
/// shown, in full, in that table's ordinary admin grid -- including <c>CLIENT_SECRET</c>. Here the secret
/// is encrypted at rest, never returned to the browser, and sits behind a permission granted separately
/// from general configuration editing.
/// </summary>
public class D365FinanceConnector : StoreBackedIntegrationConnectorBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ID365FinanceConfigReader _configReader;

    public D365FinanceConnector(
        IIntegrationSettingStore store,
        IHttpClientFactory httpClientFactory,
        ID365FinanceConfigReader configReader)
        : base(store)
    {
        _httpClientFactory = httpClientFactory;
        _configReader = configReader;
    }

    public override string Key => IntegrationKeys.D365Finance;

    public override IntegrationCategory Category => IntegrationCategory.Finance;

    public override string NameKey => "Integration_D365Finance";

    public override string DescriptionKey => "Integration_D365Finance_Description";

    public override IReadOnlyList<IntegrationFieldDefinition> Fields { get; } = new List<IntegrationFieldDefinition>
    {
        new(D365FinanceSettingKeys.RequestUrl, "Integration_D365_RequestUrl", IntegrationFieldType.Url, true, false,
            "Integration_D365_RequestUrl_Help"),
        new(D365FinanceSettingKeys.ClientId, "Integration_D365_ClientId", IntegrationFieldType.Text, true, false),
        new(D365FinanceSettingKeys.ClientSecret, "Integration_D365_ClientSecret", IntegrationFieldType.Password, true, true),
        new(D365FinanceSettingKeys.GrantType, "Integration_D365_GrantType", IntegrationFieldType.Text, true, false,
            null, "client_credentials"),
        new(D365FinanceSettingKeys.Resource, "Integration_D365_Resource", IntegrationFieldType.Url, true, false,
            "Integration_D365_Resource_Help"),
        new(D365FinanceSettingKeys.RequestUrlJournalHeader, "Integration_D365_JournalHeaderUrl", IntegrationFieldType.Url, true, false),
        new(D365FinanceSettingKeys.RequestUrlJournalLine, "Integration_D365_JournalLineUrl", IntegrationFieldType.Url, true, false),
        new(D365FinanceSettingKeys.JournalHeaderODataETag, "Integration_D365_JournalHeaderETag", IntegrationFieldType.Text, true, false),
        new(D365FinanceSettingKeys.ChangesetLimit, "Integration_D365_ChangesetLimit", IntegrationFieldType.Number, true, false,
            "Integration_D365_ChangesetLimit_Help", "100")
    };

    /// <summary>
    /// Acquires a real OAuth token against the configured endpoint. Mirrors
    /// <c>D365FinancePostingClient.AcquireAccessTokenAsync</c>, including the vendor's
    /// <c>client_Id</c> casing, so that a passing test means the posting path will authenticate too.
    /// </summary>
    public override async Task<IntegrationTestResult> TestConnectionAsync()
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Read through the config reader, not the store: a tenant configured before the Integration
            // Center existed still has its settings in SysTenantConfig, and "Test Connection" saying "not
            // set up" about a working integration would be a worse answer than no button at all.
            async Task<string> Value(string key) => await _configReader.GetAsync(key);

            var requestUrl = await Value(D365FinanceSettingKeys.RequestUrl);
            if (string.IsNullOrWhiteSpace(requestUrl))
            {
                return IntegrationTestResult.Failure(L("IntegrationNotConfiguredYet"));
            }

            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(30);

            using var content = new MultipartFormDataContent
            {
                { new StringContent(await Value(D365FinanceSettingKeys.ClientId) ?? ""), "client_Id" },
                { new StringContent(await Value(D365FinanceSettingKeys.GrantType) ?? ""), "grant_type" },
                { new StringContent(await Value(D365FinanceSettingKeys.Resource) ?? ""), "resource" },
                { new StringContent(await Value(D365FinanceSettingKeys.ClientSecret) ?? ""), "client_secret" }
            };

            using var response = await client.PostAsync(requestUrl, content);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return Fail(stopwatch, L("IntegrationTestHttpFailure", (int)response.StatusCode, Shorten(body)));
            }

            using var document = JsonDocument.Parse(body);
            if (!document.RootElement.TryGetProperty("access_token", out _))
            {
                return Fail(stopwatch, L("Integration_D365_NoAccessToken"));
            }

            stopwatch.Stop();
            return new IntegrationTestResult
            {
                Succeeded = true,
                Message = L("Integration_D365_TokenAcquired"),
                DurationMs = (int)stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            return Fail(stopwatch, ex.Message);
        }
    }

    private static IntegrationTestResult Fail(Stopwatch stopwatch, string message)
    {
        stopwatch.Stop();
        return new IntegrationTestResult
        {
            Succeeded = false,
            Message = message,
            DurationMs = (int)stopwatch.ElapsedMilliseconds
        };
    }

    private static string Shorten(string body)
    {
        if (string.IsNullOrEmpty(body))
        {
            return "";
        }

        return body.Length <= 500 ? body : body.Substring(0, 500);
    }
}
