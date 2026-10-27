using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Abp;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.UI;
using HCMv3.OrganizationManagement;
using HCMv3.PayrollManagement.Costing;
using HCMv3.Integrations;
using HCMv3.Integrations.Services;

namespace HCMv3.PayrollManagement;

/// <inheritdoc cref="IFinancePostingClient"/>
/// <remarks>
/// <para><b>Not an application service.</b> This class used to derive from <c>HCMv3AppServiceBase</c>, which
/// made ABP publish it as <c>/api/services/app/D365FinancePostingClient/PostJournal</c> -- an endpoint with no
/// permission check that sends journals to the tenant's D365. It is now a plain service
/// (<see cref="AbpServiceBase"/> keeps <c>L()</c> and the logger) reachable only from the Post JV run.</para>
/// </remarks>
public class D365FinancePostingClient : AbpServiceBase, IFinancePostingClient, ITransientDependency
{
    /// <summary>Legacy hardcoded journal name (line 1159).</summary>
    public const string JournalName = "General";

    /// <summary>Legacy separator between a failed batch's error and the lines it carried (line 1483).</summary>
    public const string JsonSeparator = "------------------------JSon Starts From Here-----------------------------";

    /// <summary>
    /// A changeset reply inside a <c>multipart/mixed</c> batch response: <c>HTTP/1.1 400 Bad Request</c> and
    /// the like. OData answers the OUTER request 200 even when a changeset inside it failed, so the outer
    /// status alone (all that legacy looked at) can report a rejected batch as posted.
    /// </summary>
    private static readonly Regex FailedChangesetStatus =
        new(@"^HTTP/1\.[01]\s+[45]\d\d", RegexOptions.Multiline | RegexOptions.CultureInvariant);

    private readonly ID365FinanceConfigReader _configReader;
    private readonly ID365FinancePostingSwitch _postingSwitch;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IRepository<CostElement> _costElementRepository;

    public D365FinancePostingClient(
        ID365FinanceConfigReader configReader,
        ID365FinancePostingSwitch postingSwitch,
        IHttpClientFactory httpClientFactory,
        IRepository<CostElement> costElementRepository)
    {
        _configReader = configReader;
        _postingSwitch = postingSwitch;
        _httpClientFactory = httpClientFactory;
        _costElementRepository = costElementRepository;
        LocalizationSourceName = HCMv3Consts.LocalizationSourceName;
    }

    public async Task<FinanceJournalPostResult> PostJournalAsync(PayrollArea payrollArea, List<JournalVoucherLine> lines,
                                                                 string journalHeaderName, IFinancePostingObserver observer)
    {
        // The Integration Center switch is checked before anything is read or sent.
        if (!await _postingSwitch.IsPostingAllowedAsync())
        {
            throw new UserFriendlyException(L("D365FinancePostingSwitchedOff"));
        }

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

        var companyName = payrollArea?.FICompanyId == null
            ? ""
            : _costElementRepository.GetAll()
                  .Where(c => c.Id == payrollArea.FICompanyId.Value)
                  .Select(c => c.Code)
                  .FirstOrDefault() ?? "";

        var accessToken = await AcquireAccessTokenAsync(requestUrl, clientId, grantType, resource, clientSecret);

        var journalBatchNumber = await CreateJournalHeaderAsync(
            journalHeaderUrl, odataETag, accessToken, companyName, journalHeaderName, observer);

        var result = new FinanceJournalPostResult { JournalBatchNumber = journalBatchNumber };

        await PostJournalLinesAsync(journalLineUrl, accessToken, companyName, journalBatchNumber,
                                    lines ?? new List<JournalVoucherLine>(), changesetLimit, observer, result);

        return result;
    }

    /// <summary>Legacy lines 1105-1146: client-credentials token request as multipart form data.</summary>
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
            // Legacy lines 1124-1137 lifted error, error_description, timestamp, trace_id, correlation_id and
            // error_uri out of this body; the body itself carries all of them, so it is kept whole.
            throw new UserFriendlyException($"Finance token request failed ({(int)response.StatusCode}): {body}");
        }

        using var document = JsonDocument.Parse(body);
        if (!document.RootElement.TryGetProperty("access_token", out var token))
        {
            throw new UserFriendlyException("Finance token response did not contain an access_token.");
        }

        return token.GetString();
    }

    /// <summary>
    /// Legacy <c>PostJournalHeader</c> (lines 1293-1381), including its log rows: "Successfull : Journal
    /// Header Created (...)" on success (line 1346), the request and the reply on failure (lines 1360-1376).
    /// The success row now also carries the batch number, so the log alone shows what reached D365.
    /// </summary>
    private async Task<string> CreateJournalHeaderAsync(string journalHeaderUrl, string odataETag,
                                                        string accessToken, string companyName,
                                                        string description, IFinancePostingObserver observer)
    {
        var payload = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["@odata.etag"] = odataETag,
            ["dataAreaId"] = companyName,
            ["JournalName"] = JournalName,
            ["Description"] = description
        });

        string failure;
        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(5);

            using var request = new HttpRequestMessage(HttpMethod.Post, journalHeaderUrl)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                string batchNumber = null;
                try
                {
                    using var document = JsonDocument.Parse(body);
                    if (document.RootElement.TryGetProperty("JournalBatchNumber", out var number))
                    {
                        batchNumber = number.GetString();
                    }
                }
                catch (JsonException)
                {
                    // Reported below as a header failure with the reply that could not be read.
                }

                if (!string.IsNullOrWhiteSpace(batchNumber))
                {
                    await NotifyAsync(observer, true, batchNumber,
                        $"Successfull : Journal Header Created ({description}) : JournalBatchNumber : {batchNumber}");
                    return batchNumber;
                }

                failure = DescribeRequest(journalHeaderUrl, payload, response) + Environment.NewLine +
                          "Finance journal header response did not contain a JournalBatchNumber." +
                          Environment.NewLine + body;
            }
            else
            {
                failure = DescribeRequest(journalHeaderUrl, payload, response) + Environment.NewLine + body;
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            failure = DescribeRequest(journalHeaderUrl, payload, null) + Environment.NewLine + ex.Message +
                      (ex.InnerException != null ? Environment.NewLine + ex.InnerException.Message : "");
        }

        await NotifyAsync(observer, false, null, failure);
        throw new UserFriendlyException($"Finance journal header creation failed for '{description}'.", failure);
    }

    /// <summary>
    /// Legacy journal-line loop (lines 1160-1271) and <c>PostMultiJournalLines</c> (lines 1382-1495). Each
    /// line becomes one OData changeset inside a <c>multipart/mixed</c> batch; a batch is flushed every
    /// <c>CHANGESET_LIMIT</c> lines. Content ids run on across batches, as legacy's <c>ContentId</c> did, so
    /// a batch's "ChangeSet : a-b" in the log names its lines' positions in the whole journal.
    ///
    /// <para>A rejected batch is logged with D365's full reply and the payload, and the loop carries on with
    /// the next batch -- legacy's <c>PostMultiJournalLines</c> caught everything and returned <c>null</c>.</para>
    /// </summary>
    private async Task PostJournalLinesAsync(string journalLineUrl, string accessToken, string companyName,
                                             string journalBatchNumber, List<JournalVoucherLine> lines,
                                             int changesetLimit, IFinancePostingObserver observer,
                                             FinanceJournalPostResult result)
    {
        var contentId = 0;
        var srNo = 0;

        foreach (var chunk in lines.Select((line, index) => new { line, index })
                                   .GroupBy(x => x.index / changesetLimit)
                                   .Select(g => g.Select(x => x.line).ToList()))
        {
            srNo++;
            var changeSetStart = contentId + 1;
            var batchBoundary = "batch_" + Guid.NewGuid().ToString("N");
            var builder = new StringBuilder();

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

            var batchPayload = builder.ToString();
            var position = $"{journalBatchNumber}{Environment.NewLine},SrNo : {srNo}{Environment.NewLine}" +
                           $",ChangeSet : {changeSetStart}-{contentId}{Environment.NewLine}";

            result.BatchCount++;

            string failure;
            try
            {
                using var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromMinutes(5);

                using var request = new HttpRequestMessage(HttpMethod.Post, journalLineUrl)
                {
                    Content = new StringContent(batchPayload, Encoding.UTF8, "text/plain")
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Content.Headers.ContentType =
                    MediaTypeHeaderValue.Parse("multipart/mixed;boundary=" + batchBoundary);

                using var response = await client.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode && !FailedChangesetStatus.IsMatch(body ?? ""))
                {
                    // Legacy line 1450-1452, verbatim wording.
                    await NotifyAsync(observer, true, journalBatchNumber, "Successfull : JournalBatchNumber : " + position);
                    continue;
                }

                failure = DescribeRequest(journalLineUrl, null, response) + Environment.NewLine + body;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                failure = DescribeRequest(journalLineUrl, null, null) + Environment.NewLine + ex.Message +
                          (ex.InnerException != null ? Environment.NewLine + ex.InnerException.Message : "");
            }

            result.FailedBatchCount++;

            // Legacy lines 1478-1484: the request, which batch, then the JSON that was sent.
            await NotifyAsync(observer, false, journalBatchNumber,
                failure + Environment.NewLine +
                "Error in JournalBatchNumber : " + position +
                JsonSeparator + Environment.NewLine +
                batchPayload);
        }
    }

    /// <summary>
    /// Legacy "API Request Info" block (lines 1318-1329 / 1407-1417), without the emoji. The bearer token is
    /// never written out.
    /// </summary>
    private static string DescribeRequest(string url, string body, HttpResponseMessage response)
    {
        var text = new StringBuilder();
        text.AppendLine("REQUEST");
        text.AppendLine("URL     : " + url);
        text.AppendLine("Method  : POST");
        text.AppendLine("Headers : Authorization: Bearer [REDACTED]");
        if (body != null)
        {
            text.AppendLine("Body    : " + body);
        }

        text.AppendLine();
        text.AppendLine("RESPONSE");
        text.Append("Status Code : ");
        text.Append(response == null
            ? "(no response)"
            : $"{(int)response.StatusCode} ({response.StatusCode})");

        return text.ToString();
    }

    private static Task NotifyAsync(IFinancePostingObserver observer, bool succeeded, string journalBatchNumber, string message)
    {
        return observer == null
            ? Task.CompletedTask
            : observer.OnPostingEventAsync(new FinancePostingEvent
            {
                Succeeded = succeeded,
                JournalBatchNumber = journalBatchNumber,
                Message = message
            });
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
