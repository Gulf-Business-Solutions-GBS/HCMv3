using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.UI;
using HCMv3.OrganizationManagement;
using HCMv3.PayrollManagement.Costing;
using HCMv3.Integrations;
using HCMv3.Integrations.Services;

namespace HCMv3.PayrollManagement;

/// <inheritdoc cref="IFinancePostingClient"/>
public class D365FinancePostingClient : HCMv3AppServiceBase, IFinancePostingClient, ITransientDependency
{

    /// <summary>Legacy hardcoded journal name (line 1159).</summary>
    public const string JournalName = "General";

    private readonly ID365FinanceConfigReader _configReader;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IRepository<CostElement> _costElementRepository;

    public D365FinancePostingClient(
        ID365FinanceConfigReader configReader,
        IHttpClientFactory httpClientFactory,
        IRepository<CostElement> costElementRepository)
    {
        _configReader = configReader;
        _httpClientFactory = httpClientFactory;
        _costElementRepository = costElementRepository;
    }

    public async Task<string> PostJournalAsync(PayrollArea payrollArea, List<JournalVoucherLine> lines,
                                               string journalHeaderName)
    {
        var requestUrl = await RequireConfigAsync(D365FinanceSettingKeys.RequestUrl);
        var clientId = await RequireConfigAsync(D365FinanceSettingKeys.ClientId);
        var grantType = await RequireConfigAsync(D365FinanceSettingKeys.GrantType);
        var resource = await RequireConfigAsync(D365FinanceSettingKeys.Resource);
        var clientSecret = await RequireConfigAsync(D365FinanceSettingKeys.ClientSecret);
        var journalHeaderUrl = await RequireConfigAsync(D365FinanceSettingKeys.RequestUrlJournalHeader);
        var journalLineUrl = await RequireConfigAsync(D365FinanceSettingKeys.RequestUrlJournalLine);
        var odataETag = await RequireConfigAsync(D365FinanceSettingKeys.JournalHeaderODataETag);

        var changesetLimitRaw = await RequireConfigAsync(D365FinanceSettingKeys.ChangesetLimit);
        if (!int.TryParse(changesetLimitRaw, NumberStyles.Integer, CultureInfo.InvariantCulture,
                          out var changesetLimit) || changesetLimit <= 0)
        {
            throw new UserFriendlyException(L("FiPostingIntegrationNotConfigured", D365FinanceSettingKeys.ChangesetLimit));
        }

        var companyName = payrollArea.FICompanyId == null
            ? ""
            : _costElementRepository.GetAll()
                  .Where(c => c.Id == payrollArea.FICompanyId.Value)
                  .Select(c => c.Code)
                  .FirstOrDefault() ?? "";

        var accessToken = await AcquireAccessTokenAsync(requestUrl, clientId, grantType, resource, clientSecret);

        var journalBatchNumber = await CreateJournalHeaderAsync(
            journalHeaderUrl, odataETag, accessToken, companyName, journalHeaderName);

        await PostJournalLinesAsync(journalLineUrl, accessToken, companyName, journalBatchNumber,
                                    lines, changesetLimit);

        return journalBatchNumber;
    }

    /// <summary>Legacy lines 1118-1157: client-credentials token request as multipart form data.</summary>
    private async Task<string> AcquireAccessTokenAsync(string requestUrl, string clientId, string grantType,
                                                       string resource, string clientSecret)
    {
        using var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromMinutes(5);

        using var content = new MultipartFormDataContent
        {
            { new StringContent(clientId), "client_Id" },
            { new StringContent(grantType), "grant_type" },
            { new StringContent(resource), "resource" },
            { new StringContent(clientSecret), "client_secret" }
        };

        using var response = await client.PostAsync(requestUrl, content);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new UserFriendlyException($"Finance token request failed ({(int)response.StatusCode}): {body}");
        }

        using var document = JsonDocument.Parse(body);
        if (!document.RootElement.TryGetProperty("access_token", out var token))
        {
            throw new UserFriendlyException("Finance token response did not contain an access_token.");
        }

        return token.GetString();
    }

    /// <summary>Legacy <c>PostJournalHeader</c> (lines 1305-1400).</summary>
    private async Task<string> CreateJournalHeaderAsync(string journalHeaderUrl, string odataETag,
                                                        string accessToken, string companyName,
                                                        string description)
    {
        using var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromMinutes(5);

        var payload = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["@odata.etag"] = odataETag,
            ["dataAreaId"] = companyName,
            ["JournalName"] = JournalName,
            ["Description"] = description
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, journalHeaderUrl)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new UserFriendlyException($"Finance journal header creation failed ({(int)response.StatusCode}): {body}");
        }

        using var document = JsonDocument.Parse(body);
        if (!document.RootElement.TryGetProperty("JournalBatchNumber", out var batchNumber))
        {
            throw new UserFriendlyException("Finance journal header response did not contain a JournalBatchNumber.");
        }

        return batchNumber.GetString();
    }

    /// <summary>
    /// Legacy journal-line loop (lines 1173-1292): each line becomes one OData changeset inside a
    /// multipart/mixed batch, and the batch is flushed every <c>CHANGESET_LIMIT</c> lines.
    /// </summary>
    private async Task PostJournalLinesAsync(string journalLineUrl, string accessToken, string companyName,
                                             string journalBatchNumber, List<JournalVoucherLine> lines,
                                             int changesetLimit)
    {
        var batchBoundary = "batch_" + Guid.NewGuid().ToString("N");

        foreach (var chunk in lines.Select((line, index) => new { line, index })
                                   .GroupBy(x => x.index / changesetLimit)
                                   .Select(g => g.Select(x => x.line).ToList()))
        {
            var builder = new StringBuilder();
            var contentId = 0;

            foreach (var line in chunk)
            {
                contentId++;
                var changeSet = "changeset_" + contentId;

                builder.Append("--").Append(batchBoundary).Append("\r\n");
                builder.Append("Content-Type: multipart/mixed; boundary=").Append(changeSet).Append("\r\n\r\n");
                builder.Append("--").Append(changeSet).Append("\r\n");
                builder.Append("Content-Type: application/http\r\n");
                builder.Append("Content-Transfer-Encoding: binary\r\n\r\n");
                builder.Append("POST LedgerJournalLines?cross-company=true HTTP/1.1\r\n");
                builder.Append("Content-ID: ").Append(contentId).Append("\r\n");
                builder.Append("Accept: application/json;q=0.9, */*;q=0.1\r\n");
                builder.Append("OData-Version: 4.0\r\n");
                builder.Append("Content-Type: application/json\r\n");
                builder.Append("OData-MaxVersion: 4.0\r\n\r\n");
                builder.Append(BuildLinePayload(companyName, journalBatchNumber, line)).Append("\r\n\r\n");
                builder.Append("--").Append(changeSet).Append("--\r\n");
            }

            builder.Append("--").Append(batchBoundary).Append("--\r\n");

            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(5);

            using var request = new HttpRequestMessage(HttpMethod.Post, journalLineUrl)
            {
                Content = new StringContent(builder.ToString(), Encoding.UTF8, "text/plain")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Content.Headers.ContentType =
                MediaTypeHeaderValue.Parse("multipart/mixed;boundary=" + batchBoundary);

            using var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                throw new UserFriendlyException(
                    $"Finance journal line batch failed ({(int)response.StatusCode}) for journal " +
                    $"{journalBatchNumber}: {body}");
            }
        }
    }

    /// <summary>
    /// Legacy line payload (lines 1194-1261). The <c>AccountDisplayValue</c> assembly is the interesting
    /// part and is reproduced exactly:
    /// <list type="bullet">
    /// <item><description><b>Ledger</b> lines concatenate five dimension segments plus three trailing empty
    /// segments: <c>MainAccount-Brand-Department-Location-Employees---</c> (legacy line 1231). Note the
    /// dimension ORDER here differs from the export-column order.</description></item>
    /// <item><description>Every segment has its own literal hyphens escaped to <c>\\-</c> first (lines
    /// 1225-1229), so a hyphen inside a dimension code is not mistaken for the segment separator.</description></item>
    /// <item><description><b>Customer</b> lines send account type <c>"Cust"</c>, not <c>"Customer"</c>
    /// (line 1238) -- a D365 naming quirk.</description></item>
    /// <item><description><b>Bank</b> lines escape hyphens in the raw account value (line 1240).</description></item>
    /// </list>
    /// </summary>
    internal static string BuildLinePayload(string companyName, string journalBatchNumber, JournalVoucherLine line)
    {
        string Escape(string value) => (value ?? "").Replace("-", "\\\\-");

        var accountType = line.LineNumberAccountType ?? "";
        string accountDisplayValue;
        var payloadAccountType = accountType;

        if (accountType == "Ledger")
        {
            accountDisplayValue = string.Concat(
                Escape(line.LedgerDimensionMainAccount), "-",
                Escape(line.LedgerDimensionBrand), "-",
                Escape(line.LedgerDimensionDepartment), "-",
                Escape(line.LedgerDimensionLocation), "-",
                Escape(line.LedgerDimensionEmployees), "---");
        }
        else
        {
            accountDisplayValue = line.LedgerDimensionMainAccount ?? "";

            if (accountType == JournalVoucherBuilder.CustomerAccountTypeName)
            {
                payloadAccountType = "Cust";
            }

            if (accountType == "Bank")
            {
                accountDisplayValue = Escape(accountDisplayValue);
            }
        }

        return JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["dataAreaId"] = companyName,
            ["JournalBatchNumber"] = journalBatchNumber,

            // Legacy line 1201: the D365 payload uses TransDate in ISO form, NOT the display-formatted
            // "Line number.Date" column.
            ["TransDate"] = line.TransDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["AccountType"] = payloadAccountType,
            ["AccountDisplayValue"] = accountDisplayValue,
            ["CurrencyCode"] = line.Currency ?? "",
            ["DebitAmount"] = line.Debit,
            ["CreditAmount"] = line.Credit,
            ["Text"] = line.Description ?? "",
            ["OffsetAccountDisplayValue"] = line.OffsetLedgerDimensionMainAccount ?? "",
            ["OffsetCompany"] = line.OffsetCompanyAccounts ?? "",
            ["Company"] = line.CompanyAccounts ?? ""
        });
    }

    /// <summary>
    /// Reads through <see cref="ID365FinanceConfigReader"/> rather than straight from
    /// <c>SysTenantConfig</c>, so that settings entered in the Integration Center are the ones actually
    /// used to post. Without this, the Integration Center's Dynamics card would save credentials that
    /// posting silently ignored.
    /// </summary>
    private async Task<string> RequireConfigAsync(string key)
    {
        var value = await _configReader.GetAsync(key);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new UserFriendlyException(L("FiPostingIntegrationNotConfigured", key));
        }

        return value;
    }
}
