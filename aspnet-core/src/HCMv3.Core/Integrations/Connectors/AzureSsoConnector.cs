using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Abp.Configuration;
using Abp.Extensions;
using Abp.Json;
using Abp.Runtime.Session;
using HCMv3.Authentication;
using HCMv3.Configuration;
using HCMv3.Integrations.Services;

namespace HCMv3.Integrations.Connectors;

/// <summary>
/// Microsoft Entra ID (Azure AD) single sign-on, via the OpenID Connect external login provider.
///
/// WHY OpenID CONNECT AND NOT THE "MICROSOFT" PROVIDER: ASP.NET Zero ships both. The Microsoft provider
/// is for consumer Microsoft accounts; work and school accounts sign in through OpenID Connect against a
/// tenant-specific authority, which is what an organisation deploying HCMv3 means by "Azure SSO".
///
/// STORAGE: ABP's <c>SettingManager</c>, as a JSON blob, because the login pipeline deserialises it there
/// at authentication time.
///
/// KNOWN LIMITATION: the client secret sits inside that blob in plain text, and encrypting it would mean
/// the framework could no longer read it. The Integration Center does not make this worse -- it never
/// returns the secret to the browser -- but it cannot fix it either, and it is recorded here rather than
/// left to be rediscovered.
/// </summary>
public class AzureSsoConnector : HCMv3DomainServiceBase, IIntegrationConnector
{
    public const string FieldClientId = "ClientId";
    public const string FieldClientSecret = "ClientSecret";
    public const string FieldAuthority = "Authority";
    public const string FieldValidateIssuer = "ValidateIssuer";
    public const string FieldResponseType = "ResponseType";

    private readonly ISettingManager _settingManager;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAbpSession _abpSession;

    public AzureSsoConnector(
        ISettingManager settingManager,
        IHttpClientFactory httpClientFactory,
        IAbpSession abpSession)
    {
        _settingManager = settingManager;
        _httpClientFactory = httpClientFactory;
        _abpSession = abpSession;
    }

    public string Key => IntegrationKeys.AzureSso;

    public IntegrationCategory Category => IntegrationCategory.Identity;

    public string NameKey => "Integration_AzureSso";

    public string DescriptionKey => "Integration_AzureSso_Description";

    public bool SupportsTest => true;

    public string ExclusiveGroup => null;

    public bool SupportsEnableToggle => true;

    public string ExternalManageRoute => "/app/admin/settings";

    public IReadOnlyList<IntegrationFieldDefinition> Fields { get; } = new List<IntegrationFieldDefinition>
    {
        new(FieldAuthority, "Integration_AzureSso_Authority", IntegrationFieldType.Url, true, false,
            "Integration_AzureSso_Authority_Help"),
        new(FieldClientId, "Integration_AzureSso_ClientId", IntegrationFieldType.Text, true, false,
            "Integration_AzureSso_ClientId_Help"),
        new(FieldClientSecret, "Integration_AzureSso_ClientSecret", IntegrationFieldType.Password, true, true),
        new(FieldResponseType, "Integration_AzureSso_ResponseType", IntegrationFieldType.Text, false, false,
            "Integration_AzureSso_ResponseType_Help", "code"),
        new(FieldValidateIssuer, "Integration_AzureSso_ValidateIssuer", IntegrationFieldType.Boolean, false, false,
            "Integration_AzureSso_ValidateIssuer_Help", "true")
    };

    public async Task<IntegrationConnectorState> GetStateAsync()
    {
        var settings = await GetProviderSettingsAsync();

        var isConfigured = !settings.ClientId.IsNullOrWhiteSpace()
                           && !settings.Authority.IsNullOrWhiteSpace();

        var isDeactivated = await _settingManager.GetSettingValueForTenantAsync<bool>(
            AppSettings.ExternalLoginProvider.Tenant.OpenIdConnect_IsDeactivated,
            _abpSession.GetTenantId());

        return new IntegrationConnectorState
        {
            IsConfigured = isConfigured,
            IsEnabled = isConfigured && !isDeactivated
        };
    }

    public async Task<IReadOnlyList<IntegrationFieldValue>> GetValuesAsync()
    {
        var settings = await GetProviderSettingsAsync();

        return new List<IntegrationFieldValue>
        {
            new(FieldAuthority, settings.Authority, !settings.Authority.IsNullOrWhiteSpace()),
            new(FieldClientId, settings.ClientId, !settings.ClientId.IsNullOrWhiteSpace()),
            new(FieldClientSecret, null, !settings.ClientSecret.IsNullOrWhiteSpace()),
            new(FieldResponseType, settings.ResponseType, !settings.ResponseType.IsNullOrWhiteSpace()),
            new(FieldValidateIssuer, settings.ValidateIssuer.ToString(CultureInfo.InvariantCulture), true)
        };
    }

    public async Task SaveValuesAsync(IReadOnlyDictionary<string, string> values)
    {
        string Submitted(string key) => values != null && values.TryGetValue(key, out var v) ? v : null;

        var settings = await GetProviderSettingsAsync();

        settings.Authority = Submitted(FieldAuthority);
        settings.ClientId = Submitted(FieldClientId);
        settings.ResponseType = Submitted(FieldResponseType);
        settings.ValidateIssuer = ParseBool(Submitted(FieldValidateIssuer), settings.ValidateIssuer);

        var clientSecret = Submitted(FieldClientSecret);
        if (!clientSecret.IsNullOrWhiteSpace())
        {
            // Blank means "keep the stored secret".
            settings.ClientSecret = clientSecret;
        }

        // IsValid() throws UserFriendlyException on a non-https authority, which is the validation the
        // login pipeline applies -- better raised here, while an administrator is looking at the form,
        // than at someone's next sign-in attempt.
        var isValid = settings.IsValid();

        await _settingManager.ChangeSettingForTenantAsync(
            _abpSession.GetTenantId(),
            AppSettings.ExternalLoginProvider.Tenant.OpenIdConnect,
            isValid ? settings.ToJsonString() : "");
    }

    /// <summary>
    /// Fetches the authority's OpenID Connect discovery document. That is what the login pipeline does
    /// first at sign-in, so a failure here is exactly the failure a user would hit -- and unlike a full
    /// sign-in it needs no interactive browser session.
    /// </summary>
    public async Task<IntegrationTestResult> TestConnectionAsync()
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var settings = await GetProviderSettingsAsync();
            if (settings.Authority.IsNullOrWhiteSpace())
            {
                return Fail(stopwatch, L("IntegrationNotConfiguredYet"));
            }

            var discoveryUrl = settings.Authority.TrimEnd('/') + "/.well-known/openid-configuration";

            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(30);

            using var response = await client.GetAsync(discoveryUrl);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return Fail(stopwatch, L("IntegrationTestHttpFailure", (int)response.StatusCode, Shorten(body)));
            }

            using var document = JsonDocument.Parse(body);
            if (!document.RootElement.TryGetProperty("authorization_endpoint", out _))
            {
                return Fail(stopwatch, L("Integration_AzureSso_DiscoveryIncomplete"));
            }

            stopwatch.Stop();

            return new IntegrationTestResult
            {
                Succeeded = true,
                Message = L("Integration_AzureSso_DiscoveryReached"),
                DurationMs = (int)stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            return Fail(stopwatch, ex.Message);
        }
    }

    public async Task SetEnabledAsync(bool enabled)
    {
        await _settingManager.ChangeSettingForTenantAsync(
            _abpSession.GetTenantId(),
            AppSettings.ExternalLoginProvider.Tenant.OpenIdConnect_IsDeactivated,
            (!enabled).ToString(CultureInfo.InvariantCulture));
    }

    private async Task<OpenIdConnectExternalLoginProviderSettings> GetProviderSettingsAsync()
    {
        var json = await _settingManager.GetSettingValueForTenantAsync(
            AppSettings.ExternalLoginProvider.Tenant.OpenIdConnect,
            _abpSession.GetTenantId());

        return json.IsNullOrWhiteSpace()
            ? new OpenIdConnectExternalLoginProviderSettings()
            : json.FromJsonString<OpenIdConnectExternalLoginProviderSettings>();
    }

    private static bool ParseBool(string value, bool fallback)
    {
        return bool.TryParse(value, out var parsed) ? parsed : fallback;
    }

    private static string Shorten(string body)
    {
        if (string.IsNullOrEmpty(body))
        {
            return "";
        }

        return body.Length <= 500 ? body : body.Substring(0, 500);
    }

    private static IntegrationTestResult Fail(Stopwatch stopwatch, string message)
    {
        if (stopwatch.IsRunning)
        {
            stopwatch.Stop();
        }

        return new IntegrationTestResult
        {
            Succeeded = false,
            Message = message,
            DurationMs = (int)stopwatch.ElapsedMilliseconds
        };
    }
}
