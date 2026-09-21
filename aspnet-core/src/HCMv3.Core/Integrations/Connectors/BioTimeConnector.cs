using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Runtime.Session;
using Abp.UI;
using HCMv3.Integrations.Services;
using HCMv3.SystemSettings;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Services;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Integrations.Connectors;

/// <summary>
/// BioTime-style time-attendance device/cloud API.
///
/// STORAGE: stays in the existing <see cref="TimeAttAPI"/> entity rather than moving to
/// <see cref="IntegrationSetting"/>. That row is read as a unit by
/// <see cref="ITimeAttendanceIntegrationService"/> and edited by the existing Time Attendance API screen;
/// splitting five of its columns into a second table to centralise one password would mean two reads, two
/// editing paths for one logical record, and a partially migrated entity -- for no gain the Integration
/// Center does not already get from a uniform connector interface.
///
/// SCOPE: only the connection fields appear here. <c>PunchInCode</c> and its siblings resolve to
/// <c>TransType</c> records and are domain mapping, not credentials, so they stay on the dedicated screen
/// that <see cref="ExternalManageRoute"/> links to.
///
/// ENABLING writes <c>ManageTimeSettings.EnableTimeAttIntegrationAPI</c>, which is the flag
/// <see cref="ITimeAttendanceIntegrationService.TransferTransactionFromApiAsync"/> actually checks before
/// syncing -- so switching the card off really does stop the sync.
/// </summary>
public class BioTimeConnector : HCMv3DomainServiceBase, IIntegrationConnector
{
    public const string FieldTokenUrl = "TokenUrl";
    public const string FieldUsername = "Username";
    public const string FieldPassword = "Password";
    public const string FieldTransactionsUrl = "TransactionsUrl";
    public const string FieldDevicesUrl = "DevicesUrl";
    public const string FieldNoOfRecords = "NoOfRecords";

    private readonly IRepository<TimeAttAPI> _timeAttApiRepository;
    private readonly IRepository<ManageTimeSettings> _timeSettingsRepository;
    private readonly ITimeAttendanceIntegrationService _integrationService;
    private readonly IAbpSession _abpSession;

    public BioTimeConnector(
        IRepository<TimeAttAPI> timeAttApiRepository,
        IRepository<ManageTimeSettings> timeSettingsRepository,
        ITimeAttendanceIntegrationService integrationService,
        IAbpSession abpSession)
    {
        _timeAttApiRepository = timeAttApiRepository;
        _timeSettingsRepository = timeSettingsRepository;
        _integrationService = integrationService;
        _abpSession = abpSession;
    }

    public string Key => IntegrationKeys.BioTime;

    public IntegrationCategory Category => IntegrationCategory.TimeAttendance;

    public string NameKey => "Integration_BioTime";

    public string DescriptionKey => "Integration_BioTime_Description";

    public bool SupportsTest => true;

    public string ExclusiveGroup => null;

    public bool SupportsEnableToggle => true;

    public string ExternalManageRoute => "/app/admin/time-att-apis";

    public IReadOnlyList<IntegrationFieldDefinition> Fields { get; } = new List<IntegrationFieldDefinition>
    {
        new(FieldTokenUrl, "Integration_BioTime_TokenUrl", IntegrationFieldType.Url, true, false,
            "Integration_BioTime_TokenUrl_Help"),
        new(FieldUsername, "Integration_BioTime_Username", IntegrationFieldType.Text, true, false),
        new(FieldPassword, "Integration_BioTime_Password", IntegrationFieldType.Password, true, true),
        new(FieldTransactionsUrl, "Integration_BioTime_TransactionsUrl", IntegrationFieldType.Url, true, false),
        new(FieldDevicesUrl, "Integration_BioTime_DevicesUrl", IntegrationFieldType.Url, false, false),
        new(FieldNoOfRecords, "Integration_BioTime_NoOfRecords", IntegrationFieldType.Number, false, false,
            "Integration_BioTime_NoOfRecords_Help", "100")
    };

    public async Task<IntegrationConnectorState> GetStateAsync()
    {
        var api = await GetApiRowAsync();
        var settings = await GetTimeSettingsAsync();

        var isConfigured = api != null
                           && !string.IsNullOrWhiteSpace(api.TokenUrl)
                           && !string.IsNullOrWhiteSpace(api.Username)
                           && !string.IsNullOrWhiteSpace(api.Password)
                           && !string.IsNullOrWhiteSpace(api.TransactionsUrl);

        return new IntegrationConnectorState
        {
            IsConfigured = isConfigured,
            IsEnabled = isConfigured && settings != null && settings.EnableTimeAttIntegrationAPI
        };
    }

    public async Task<IReadOnlyList<IntegrationFieldValue>> GetValuesAsync()
    {
        var api = await GetApiRowAsync();

        return new List<IntegrationFieldValue>
        {
            Value(FieldTokenUrl, api?.TokenUrl),
            Value(FieldUsername, api?.Username),

            // The password is withheld here even though the older Time Attendance API screen shows it.
            new(FieldPassword, null, !string.IsNullOrWhiteSpace(api?.Password)),

            Value(FieldTransactionsUrl, api?.TransactionsUrl),
            Value(FieldDevicesUrl, api?.DevicesUrl),
            Value(FieldNoOfRecords, api == null ? null : api.NoOfRecords.ToString(CultureInfo.InvariantCulture))
        };
    }

    [UnitOfWork]
    public virtual async Task SaveValuesAsync(IReadOnlyDictionary<string, string> values)
    {
        var api = await GetApiRowAsync();
        var isNew = api == null;

        if (isNew)
        {
            api = new TimeAttAPI { TenantId = _abpSession.GetTenantId() };
        }

        string Submitted(string key) => values != null && values.TryGetValue(key, out var v) ? v : null;

        api.TokenUrl = Submitted(FieldTokenUrl);
        api.Username = Submitted(FieldUsername);
        api.TransactionsUrl = Submitted(FieldTransactionsUrl);
        api.DevicesUrl = Submitted(FieldDevicesUrl);

        var password = Submitted(FieldPassword);
        if (!string.IsNullOrWhiteSpace(password))
        {
            // Blank means "keep the stored password" -- the form never received it.
            api.Password = IntegrationSecret.Protect(password);
        }

        var noOfRecords = Submitted(FieldNoOfRecords);
        if (int.TryParse(noOfRecords, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) && parsed > 0)
        {
            api.NoOfRecords = parsed;
        }

        GuardRequired(api);

        if (isNew)
        {
            await _timeAttApiRepository.InsertAsync(api);
        }
        else
        {
            await _timeAttApiRepository.UpdateAsync(api);
        }
    }

    public async Task<IntegrationTestResult> TestConnectionAsync()
    {
        var stopwatch = Stopwatch.StartNew();

        var api = await GetApiRowAsync();
        if (api == null || string.IsNullOrWhiteSpace(api.TokenUrl))
        {
            return IntegrationTestResult.Failure(L("IntegrationNotConfiguredYet"));
        }

        try
        {
            // GetTokenAsync decrypts the stored password itself, so the entity is passed as stored.
            var token = await _integrationService.GetTokenAsync(api);

            if (string.IsNullOrWhiteSpace(token))
            {
                // GetTokenAsync swallows its own failures and returns null (ported legacy behaviour), so
                // there is no exception detail to pass on here.
                return Fail(stopwatch, L("Integration_BioTime_TokenFailed"));
            }

            stopwatch.Stop();

            return new IntegrationTestResult
            {
                Succeeded = true,
                Message = L("Integration_BioTime_TokenAcquired"),
                DurationMs = (int)stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            return Fail(stopwatch, ex.Message);
        }
    }

    [UnitOfWork]
    public virtual async Task SetEnabledAsync(bool enabled)
    {
        var settings = await GetTimeSettingsAsync();
        if (settings == null)
        {
            // Creating a ManageTimeSettings row here would invent defaults for a dozen unrelated
            // time-evaluation options. Better to send the administrator to the screen that owns them.
            throw new UserFriendlyException(L("Integration_BioTime_TimeSettingsMissing"));
        }

        if (enabled)
        {
            var state = await GetStateAsync();
            if (!state.IsConfigured)
            {
                throw new UserFriendlyException(L("IntegrationNotConfiguredYet"));
            }
        }

        settings.EnableTimeAttIntegrationAPI = enabled;
        await _timeSettingsRepository.UpdateAsync(settings);
    }

    private void GuardRequired(TimeAttAPI api)
    {
        if (string.IsNullOrWhiteSpace(api.TokenUrl))
        {
            throw new UserFriendlyException(L("IntegrationRequiredFieldMissing", L("Integration_BioTime_TokenUrl")));
        }

        if (string.IsNullOrWhiteSpace(api.Username))
        {
            throw new UserFriendlyException(L("IntegrationRequiredFieldMissing", L("Integration_BioTime_Username")));
        }

        if (string.IsNullOrWhiteSpace(api.Password))
        {
            throw new UserFriendlyException(L("IntegrationRequiredFieldMissing", L("Integration_BioTime_Password")));
        }

        if (string.IsNullOrWhiteSpace(api.TransactionsUrl))
        {
            throw new UserFriendlyException(L("IntegrationRequiredFieldMissing", L("Integration_BioTime_TransactionsUrl")));
        }
    }

    private static IntegrationFieldValue Value(string key, string value)
    {
        return new IntegrationFieldValue(key, value, !string.IsNullOrWhiteSpace(value));
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

    /// <summary>
    /// The tenant's time-attendance API row. The entity allows several; the sync service and this
    /// connector both take the first, which is how every deployment actually uses it.
    /// </summary>
    private async Task<TimeAttAPI> GetApiRowAsync()
    {
        return await _timeAttApiRepository.GetAll().OrderBy(a => a.Id).FirstOrDefaultAsync();
    }

    private async Task<ManageTimeSettings> GetTimeSettingsAsync()
    {
        return await _timeSettingsRepository.GetAll().OrderBy(s => s.Id).FirstOrDefaultAsync();
    }
}
