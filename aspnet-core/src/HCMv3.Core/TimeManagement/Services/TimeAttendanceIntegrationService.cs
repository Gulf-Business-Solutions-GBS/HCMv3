using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Runtime.Session;
using ClosedXML.Excel;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSettings;
using HCMv3.SystemSettings.Services;
using MySqlConnector;
using Newtonsoft.Json;

namespace HCMv3.TimeManagement.Services;

/// <inheritdoc cref="ITimeAttendanceIntegrationService"/>
/// <remarks>
/// <para>
/// Ported from legacy XAF <c>HCM10.Module.BusinessObjects.Services.TimeAttendanceIntegrationService</c>
/// (1314 lines). Method-by-method port/defer decisions:
/// </para>
/// <para><b>PORTED</b> (this class): <c>TransferTransactionFromAPI</c> -&gt;
/// <see cref="TransferTransactionFromApiAsync"/>; <c>TransferTransactionFromAPIForMissingData</c> -&gt;
/// <see cref="TransferTransactionFromApiForMissingDataAsync"/>; <c>InsertTransactionFromAPI</c> -&gt;
/// <see cref="InsertTransactionFromApiAsync"/> (see its XML doc for the staging-table architecture
/// simplification); <c>GetToken</c> -&gt; <see cref="GetTokenAsync"/>; <c>GetCheckInOutDataList</c> -&gt;
/// <see cref="GetCheckInOutDataListAsync"/>; <c>GetCheckInOutData</c> -&gt; <see cref="GetCheckInOutDataAsync"/>;
/// <c>GetDevicesData</c> -&gt; <see cref="GetDevicesDataAsync"/>; <c>GetEmployeeData</c> -&gt;
/// <see cref="GetEmployeeDataAsync"/>.
/// </para>
/// <para><b>PORTED IN A FOLLOW-UP CORRECTION WAVE</b> (real external-infrastructure package dependencies
/// added; see each method's own XML doc for full detail): <c>DumpAttendanceData</c> -&gt;
/// <see cref="DumpAttendanceDataAsync"/> and <c>TransferTransaction</c>/<c>TransferTransactionByParam</c> -&gt;
/// <see cref="TransferTransactionAsync"/>/<see cref="TransferTransactionByParamAsync"/> and
/// <c>FetchMissingTimeAttendnaceData</c> -&gt; <see cref="FetchMissingTimeAttendanceDataAsync"/> (all three use
/// the new <c>System.Data.OleDb</c> package reference against a caller-supplied "AccessControl" connection
/// string -- none is stored/fabricated in this app); <c>ImportTransactions</c> -&gt;
/// <see cref="ImportTransactionsAsync"/> (uses the new <c>ClosedXML</c> package in place of the commercial
/// DevExpress Spreadsheet reader); <c>ImportTransactionFromMySql</c> -&gt;
/// <see cref="ImportTransactionFromMySqlAsync"/> (uses the new <c>MySqlConnector</c> package against a
/// caller-supplied MySQL connection string). The legacy <c>ExecuteNonQuery(connString, sql)</c> /
/// <c>GetData(connString, sql)</c> / <c>Aq(string)</c> raw-ADO.NET helpers are inlined into the relevant
/// methods above rather than ported as standalone members.
/// </para>
/// </remarks>
[UnitOfWork]
public class TimeAttendanceIntegrationService : HCMv3DomainServiceBase, ITimeAttendanceIntegrationService
{
    private readonly IRepository<TimeAttAPI> _timeAttApiRepository;
    private readonly IRepository<TimeEvent> _timeEventRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<TransType> _transTypeRepository;
    private readonly IRepository<AccessUnit> _accessUnitRepository;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAbpSession _abpSession;

    public TimeAttendanceIntegrationService(
        IRepository<TimeAttAPI> timeAttApiRepository,
        IRepository<TimeEvent> timeEventRepository,
        IRepository<Employee> employeeRepository,
        IRepository<TransType> transTypeRepository,
        IRepository<AccessUnit> accessUnitRepository,
        ISysSettingsAccessor sysSettingsAccessor,
        IHttpClientFactory httpClientFactory,
        IAbpSession abpSession)
    {
        _timeAttApiRepository = timeAttApiRepository;
        _timeEventRepository = timeEventRepository;
        _employeeRepository = employeeRepository;
        _transTypeRepository = transTypeRepository;
        _accessUnitRepository = accessUnitRepository;
        _sysSettingsAccessor = sysSettingsAccessor;
        _httpClientFactory = httpClientFactory;
        _abpSession = abpSession;
    }

    public async Task<int> TransferTransactionFromApiAsync()
    {
        var timeSettings = await _sysSettingsAccessor.GetTimeSettingsAsync();
        if (timeSettings == null || !timeSettings.EnableTimeAttIntegrationAPI)
        {
            return 0;
        }

        var lastTransactionDate = _timeEventRepository.GetAll()
            .OrderByDescending(e => e.TransTime)
            .Select(e => (DateTime?)e.TransTime)
            .FirstOrDefault() ?? timeSettings.EarliestEvaluationDate;

        return await InsertTransactionFromApiAsync(lastTransactionDate, DateTime.Now, "", "");
    }

    public async Task<int> TransferTransactionFromApiForMissingDataAsync(DateTime fromDate, DateTime toDate, string empCode, string terminalSn)
    {
        var timeSettings = await _sysSettingsAccessor.GetTimeSettingsAsync();
        if (timeSettings == null || !timeSettings.EnableTimeAttIntegrationAPI)
        {
            return 0;
        }

        if (fromDate < timeSettings.EarliestEvaluationDate)
        {
            fromDate = timeSettings.EarliestEvaluationDate;
        }

        var lastTransactionDate = _timeEventRepository.GetAll()
            .OrderByDescending(e => e.TransTime)
            .Select(e => (DateTime?)e.TransTime)
            .FirstOrDefault();

        if (!lastTransactionDate.HasValue || lastTransactionDate.Value == default)
        {
            // Legacy only ran the sync at all once at least one prior punch has been imported.
            return 0;
        }

        if (toDate > lastTransactionDate.Value || toDate.Date == lastTransactionDate.Value.Date)
        {
            toDate = lastTransactionDate.Value;
        }
        else if (toDate.Date < lastTransactionDate.Value.Date)
        {
            toDate = new DateTime(toDate.Year, toDate.Month, toDate.Day, 23, 59, 59);
        }

        return await InsertTransactionFromApiAsync(fromDate, toDate, empCode, terminalSn);
    }

    public async Task<int> InsertTransactionFromApiAsync(DateTime startTime, DateTime endTime, string empCode, string terminalSn)
    {
        empCode ??= "";
        terminalSn ??= "";

        var timeAttApi = _timeAttApiRepository.GetAll().FirstOrDefault();
        if (timeAttApi == null)
        {
            return 0;
        }

        var punchInList = (timeAttApi.PunchInCode ?? "").Split(',');
        var breakOutList = (timeAttApi.BreakOutCode ?? "").Split(',');
        var breakInList = (timeAttApi.BreakInCode ?? "").Split(',');
        var punchOutList = (timeAttApi.PunchOutCode ?? "").Split(',');

        var accessToken = await GetTokenAsync(timeAttApi);
        if (accessToken == null)
        {
            return 0;
        }

        var devicesList = new List<ApiDevicesPage>();
        var devices = await GetDevicesDataAsync(accessToken, timeAttApi.DevicesUrl);
        if (devices != null)
        {
            devicesList.Add(devices);
        }

        while (devices != null && devices.next != null)
        {
            var nextDevicesUrl = devices.next.ToString();
            devices = await GetDevicesDataAsync(accessToken, nextDevicesUrl);
            if (devices != null)
            {
                devicesList.Add(devices);
            }
        }

        var insertedCount = 0;
        var syncedRecords = 0;

        while (startTime <= endTime)
        {
            const string page = "1";
            var pageSize = timeAttApi.NoOfRecords.ToString();
            string startTimeParam;
            string endTimeParam;

            // If this is a missing-data (gap-fill) sync, only take data up to the last sync date.
            if (startTime.Date == endTime.Date && (empCode != "" || terminalSn != ""))
            {
                startTimeParam = startTime.ToString("yyyy-MM-dd") + " 00:00:00";
                endTimeParam = endTime.ToString("yyyy-MM-dd HH:mm:ss");
            }
            else
            {
                startTimeParam = startTime.ToString("yyyy-MM-dd HH:mm:ss");
                endTimeParam = startTime.ToString("yyyy-MM-dd") + " 23:59:59";
            }

            var parameters = new StringBuilder();
            parameters.Append("page=").Append(page);
            parameters.Append("&page_size=").Append(pageSize);

            if (startTime != default)
            {
                parameters.Append("&start_time=").Append(startTimeParam);
            }

            parameters.Append("&end_time=").Append(endTimeParam);

            if (empCode != "")
            {
                parameters.Append("&emp_code=").Append(empCode);
            }

            if (terminalSn != "")
            {
                parameters.Append("&terminal_sn=").Append(terminalSn);
            }

            var requestUrl = timeAttApi.TransactionsUrl + parameters;

            var headerList = await GetCheckInOutDataListAsync(accessToken, requestUrl);
            if (headerList == null)
            {
                break;
            }

            foreach (var header in headerList)
            {
                foreach (var record in header.data.OrderBy(r => r.punch_time))
                {
                    var inserted = await TryInsertTimeEventFromApiRecordAsync(
                        record, timeAttApi, devicesList, punchInList, punchOutList, breakOutList, breakInList);
                    if (inserted)
                    {
                        insertedCount++;
                    }
                }
            }

            syncedRecords += headerList.Sum(h => h.data?.Count ?? 0);
            if (syncedRecords < timeAttApi.NoOfRecords)
            {
                startTime = startTime.Date.AddDays(1);
            }
            else
            {
                break;
            }
        }

        return insertedCount;
    }

    /// <summary>
    /// Resolves a single vendor-API punch record into a <see cref="TimeEvent"/> and inserts it, mirroring the
    /// legacy TransType/AccessUnit resolution and duplicate-skip rules -- see
    /// <see cref="ITimeAttendanceIntegrationService.InsertTransactionFromApiAsync"/> XML doc for the
    /// staging-table-collapse architecture note.
    /// </summary>
    private async Task<bool> TryInsertTimeEventFromApiRecordAsync(
        CheckInOutRecord record,
        TimeAttAPI timeAttApi,
        List<ApiDevicesPage> devicesList,
        string[] punchInList,
        string[] punchOutList,
        string[] breakOutList,
        string[] breakInList)
    {
        var recordSource = record.id;
        var pin = record.emp_code;

        var punchDateTime = DateTime.ParseExact(record.punch_time, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        var transDate = punchDateTime.Date;
        var transTime = punchDateTime;

        var accessUnitAlias = Convert.ToString(record.terminal_alias);
        var ipAddress = record.terminal_sn;

        foreach (var page in devicesList)
        {
            var deviceInfo = page.data?.FirstOrDefault(d => d.sn == record.terminal_sn);
            if (deviceInfo != null)
            {
                ipAddress = deviceInfo.ip_address;
                accessUnitAlias = deviceInfo.alias;
                break;
            }
        }

        string transTypeCode;
        if (!string.IsNullOrEmpty(timeAttApi.PunchInCode) && punchInList.Contains(record.punch_state))
        {
            transTypeCode = "I";
        }
        else if (!string.IsNullOrEmpty(timeAttApi.PunchOutCode) && punchOutList.Contains(record.punch_state))
        {
            transTypeCode = "O";
        }
        else if (!string.IsNullOrEmpty(timeAttApi.BreakOutCode) && breakOutList.Contains(record.punch_state))
        {
            transTypeCode = "BO";
        }
        else if (!string.IsNullOrEmpty(timeAttApi.BreakInCode) && breakInList.Contains(record.punch_state))
        {
            transTypeCode = "BI";
        }
        else
        {
            // Legacy fallback (both PunchInCode and PunchOutCode blank): first punch of the day for this
            // PIN is "I" (In), every subsequent punch that day is "O" (Out). Originally expressed as a raw
            // SQL CASE/EXISTS fragment against the CHECKINOUT staging table; re-expressed here as an
            // equivalent EF Core query against TimeEvent (the new single sink -- see class remarks).
            var earlierPunchExistsToday = _timeEventRepository.GetAll()
                .Any(e => e.PIN == pin && e.TransDate == transDate && e.TransTime < transTime);
            transTypeCode = earlierPunchExistsToday ? "O" : "I";
        }

        var employee = _employeeRepository.GetAll().FirstOrDefault(e => e.AccessID == pin);
        var transType = _transTypeRepository.GetAll().FirstOrDefault(t => t.Code == transTypeCode);
        if (employee == null || transType == null)
        {
            return false;
        }

        var duplicateRecordSource = _timeEventRepository.GetAll()
            .Any(e => e.RecordSource == recordSource && e.TransSource == TransSource.Machine);
        if (duplicateRecordSource)
        {
            return false;
        }

        var accessUnit = await ResolveOrCreateAccessUnitAsync(ipAddress, accessUnitAlias);

        var timeEvent = new TimeEvent
        {
            TenantId = _abpSession.TenantId ?? 0,
            EmployeeId = employee.Id,
            PIN = pin,
            ActualTransTypeId = transType.Id,
            TransTypeId = transType.Id,
            TransDate = transDate,
            TransTime = transTime,
            // Set via navigation (not AccessUnitId) so EF Core's change-tracker relationship fixup wires up
            // the FK correctly even when accessUnit is a brand-new, not-yet-saved entity in this same
            // ambient unit of work (its identity Id isn't known until SaveChanges runs).
            AccessUnit = accessUnit,
            IPAddress = ipAddress,
            TransSource = TransSource.Machine,
            RecordSource = recordSource
        };

        await _timeEventRepository.InsertAsync(timeEvent);

        return true;
    }

    /// <summary>
    /// Ported from the <c>&lt;&lt; AccessUnit &gt;&gt;</c> region of legacy <c>TransferTransactionByParam</c>
    /// (the same auto-create-if-missing-by-IP logic, reused here since this port collapses the API punch
    /// import to write directly into <see cref="TimeEvent"/> -- see class remarks).
    /// </summary>
    private async Task<AccessUnit> ResolveOrCreateAccessUnitAsync(string ipAddress, string alias)
    {
        if (string.IsNullOrEmpty(ipAddress))
        {
            return null;
        }

        var accessUnit = _accessUnitRepository.GetAll().FirstOrDefault(a => a.IPAddress == ipAddress);
        if (accessUnit != null)
        {
            return accessUnit;
        }

        // If the same alias already exists under a different IP, disambiguate the new one's name (legacy parity).
        var nameCollision = _accessUnitRepository.GetAll().Any(a => a.NameEn == alias && a.IPAddress != ipAddress);
        var name = string.IsNullOrEmpty(alias) ? ipAddress : alias;
        if (nameCollision)
        {
            name = $"{name} ({ipAddress})";
        }

        accessUnit = new AccessUnit
        {
            TenantId = _abpSession.TenantId ?? 0,
            NameEn = name,
            NameAr = name,
            IPAddress = ipAddress
        };

        return await _accessUnitRepository.InsertAsync(accessUnit);
    }

    public async Task<string> GetTokenAsync(TimeAttAPI timeAttAPI)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, timeAttAPI.TokenUrl);

            var payload = JsonConvert.SerializeObject(new
            {
                username = timeAttAPI.Username,
                email = timeAttAPI.Email,
                password = timeAttAPI.Password,
                company = timeAttAPI.Company
            });
            request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

            using var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(jsonResponse);

            return tokenResponse?.token;
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<CheckInOutHeader>> GetCheckInOutDataListAsync(string accessToken, string requestUrl)
    {
        var pages = new List<CheckInOutHeader>();
        try
        {
            var client = _httpClientFactory.CreateClient();

            while (!string.IsNullOrEmpty(requestUrl))
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.Add("Authorization", "JWT " + accessToken);

                using var response = await client.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var header = JsonConvert.DeserializeObject<CheckInOutHeader>(responseBody);
                    pages.Add(header);
                    requestUrl = header?.next;
                }
                else if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    // Matches legacy: parse the error (for potential future logging) and stop paging.
                    JsonConvert.DeserializeObject<ApiErrorResponse>(responseBody);
                    requestUrl = "";
                }
                else
                {
                    response.EnsureSuccessStatusCode();
                }
            }

            return pages;
        }
        catch
        {
            return null;
        }
    }

    public async Task<CheckInOutHeader> GetCheckInOutDataAsync(string accessToken, DateTime startTime, TimeAttAPI timeAttAPI)
    {
        try
        {
            var pageSize = timeAttAPI.NoOfRecords.ToString();
            var startTimeParam = startTime.ToString("yyyy-MM-dd HH:mm:ss");
            var endTimeParam = DateTime.Now.Date.ToString("yyyy-MM-dd") + " 23:59:59";

            var parameters = new StringBuilder();
            parameters.Append("page=1");
            parameters.Append("&page_size=").Append(pageSize);
            if (startTime != default)
            {
                parameters.Append("&start_time=").Append(startTimeParam);
            }

            parameters.Append("&end_time=").Append(endTimeParam);

            var requestUrl = timeAttAPI.TransactionsUrl + parameters;

            var client = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Add("Authorization", "JWT " + accessToken);

            using var response = await client.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return JsonConvert.DeserializeObject<CheckInOutHeader>(responseBody);
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                JsonConvert.DeserializeObject<ApiErrorResponse>(responseBody);
                return null;
            }

            response.EnsureSuccessStatusCode();
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<ApiDevicesPage> GetDevicesDataAsync(string accessToken, string devicesUrl)
    {
        if (string.IsNullOrEmpty(devicesUrl))
        {
            return null;
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, devicesUrl);
            request.Headers.Add("Authorization", "JWT " + accessToken);

            using var response = await client.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return JsonConvert.DeserializeObject<ApiDevicesPage>(responseBody);
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                JsonConvert.DeserializeObject<ApiErrorResponse>(responseBody);
                return null;
            }

            response.EnsureSuccessStatusCode();
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<ApiEmployeeHeader> GetEmployeeDataAsync(string accessToken, TimeAttAPI timeAttAPI)
    {
        try
        {
            // See ITimeAttendanceIntegrationService.GetEmployeeDataAsync XML doc: legacy hardcoded a debug IP
            // here; this port instead derives the employees endpoint from the configured DevicesUrl's host.
            var devicesUri = new Uri(timeAttAPI.DevicesUrl);
            var requestUrl = $"{devicesUri.Scheme}://{devicesUri.Authority}/personnel/api/employees/?page_size=1000";

            var client = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Add("Authorization", "JWT " + accessToken);

            using var response = await client.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return JsonConvert.DeserializeObject<ApiEmployeeHeader>(responseBody);
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                JsonConvert.DeserializeObject<ApiErrorResponse>(responseBody);
                return null;
            }

            response.EnsureSuccessStatusCode();
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task DumpAttendanceDataAsync(string accessControlConnectionString)
    {
        if (string.IsNullOrWhiteSpace(accessControlConnectionString))
        {
            throw new ArgumentException(
                "A real AccessControl database connection string must be supplied (see XML doc remarks).",
                nameof(accessControlConnectionString));
        }

        var timeSettings = await _sysSettingsAccessor.GetTimeSettingsAsync();
        if (timeSettings == null || !timeSettings.EnableMiddleTierTimeEvents)
        {
            return;
        }

        await using var connection = new OleDbConnection(accessControlConnectionString);
        await connection.OpenAsync();

        await using var command = new OleDbCommand("sp_DumpAttendaceData", connection)
        {
            CommandType = CommandType.StoredProcedure
        };
        await command.ExecuteNonQueryAsync();
    }

    public Task<int> TransferTransactionAsync(string accessControlConnectionString)
    {
        return TransferTransactionByParamAsync(accessControlConnectionString, "", "");
    }

    public async Task<int> TransferTransactionByParamAsync(string accessControlConnectionString, string accessId, string accessIpAddress)
    {
        if (string.IsNullOrWhiteSpace(accessControlConnectionString))
        {
            throw new ArgumentException(
                "A real AccessControl database connection string must be supplied (see XML doc remarks).",
                nameof(accessControlConnectionString));
        }

        accessId ??= "";
        accessIpAddress ??= "";

        var timeSettings = await _sysSettingsAccessor.GetTimeSettingsAsync();
        if (timeSettings == null || !timeSettings.EnableTimeAttendanceIntegration)
        {
            return 0;
        }

        // Take a limited number of records at a time so as not to slow down the server (legacy parity).
        var timeEventSyncBatchCount = timeSettings.TimeEventSyncBatchCount == 0 ? 350 : timeSettings.TimeEventSyncBatchCount;

        var queryString = new StringBuilder();
        queryString.Append("SELECT TOP ").Append(timeEventSyncBatchCount).AppendLine();
        queryString.AppendLine(" pin ");
        queryString.AppendLine(" ,transtype ");
        queryString.AppendLine(" ,CONVERT(VARCHAR,transdate,120) AS transdate ");
        queryString.AppendLine(" ,CONVERT(VARCHAR,transtime,120) AS transtime ");
        queryString.AppendLine(" ,accessunit,ipaddress,transfered,recordsource ");
        queryString.AppendLine(" FROM v_timeevents  ");
        queryString.AppendLine(" WHERE ISNULL(transfered, 0) = 0 ");
        queryString.AppendLine(" AND ISNULL(transtype, '') != '' ");

        if (accessId != "")
        {
            queryString.AppendLine($" AND PIN IN ('{accessId}') ");
        }

        if (accessIpAddress != "")
        {
            queryString.AppendLine($" AND ipaddress IN ('{accessIpAddress}') ");
        }

        queryString.AppendLine(" ORDER BY transdate, transtime ");

        var transactionsTable = new DataTable();
        await using (var connection = new OleDbConnection(accessControlConnectionString))
        {
            await connection.OpenAsync();
            using var adapter = new OleDbDataAdapter(queryString.ToString(), connection);
            adapter.Fill(transactionsTable);
        }

        var transTypeList = _transTypeRepository.GetAllList();
        var insertedCount = 0;
        var successRecordSources = new List<long>();

        foreach (DataRow row in transactionsTable.Rows)
        {
            var recordSource = long.Parse(row["recordsource"].ToString());
            var pin = row["pin"].ToString();
            var transTypeCode = row["transtype"].ToString();
            var transDate = DateTime.ParseExact(row["transdate"].ToString(), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            var transTime = DateTime.ParseExact(row["transtime"].ToString(), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            var accessUnitAlias = row["accessunit"].ToString();
            var ipAddress = row["ipaddress"].ToString();

            // Legacy checks only Machine-sourced rows for the same RecordSource (parity with
            // InsertTransactionFromApiAsync's duplicate-RecordSource check).
            var alreadyImported = _timeEventRepository.GetAll()
                .Any(e => e.RecordSource == recordSource && e.TransSource == TransSource.Machine);

            var employee = !alreadyImported ? _employeeRepository.GetAll().FirstOrDefault(e => e.AccessID == pin) : null;
            var transType = !alreadyImported ? transTypeList.FirstOrDefault(t => t.Code == transTypeCode) : null;

            if (!alreadyImported && employee != null && transType != null)
            {
                var duplicate = _timeEventRepository.GetAll()
                    .Any(e => e.EmployeeId == employee.Id
                        && e.TransDate == transDate.Date
                        && e.TransTime == transTime
                        && e.TransTypeId == transType.Id);

                if (!duplicate)
                {
                    var accessUnit = await ResolveOrCreateAccessUnitAsync(ipAddress, accessUnitAlias);

                    var timeEvent = new TimeEvent
                    {
                        TenantId = _abpSession.TenantId ?? 0,
                        EmployeeId = employee.Id,
                        PIN = pin,
                        ActualTransTypeId = transType.Id,
                        TransTypeId = transType.Id,
                        TransDate = transDate.Date,
                        TransTime = transTime,
                        AccessUnit = accessUnit,
                        IPAddress = ipAddress,
                        TransSource = TransSource.Machine,
                        RecordSource = recordSource
                    };

                    await _timeEventRepository.InsertAsync(timeEvent);
                    insertedCount++;
                }
            }

            // Legacy marks EVERY row (imported, duplicate-skipped, or unresolvable Employee/TransType) as
            // successfully "transfered" -- only a hard exception during the loop would leave a row untouched.
            successRecordSources.Add(recordSource);
        }

        if (successRecordSources.Count > 0)
        {
            var updateSql = $"update v_timeevents set transfered = 1 where recordsource in ({string.Join(",", successRecordSources)})";
            await using var connection = new OleDbConnection(accessControlConnectionString);
            await connection.OpenAsync();
            await using var command = new OleDbCommand(updateSql, connection);
            await command.ExecuteNonQueryAsync();
        }

        return insertedCount;
    }

    public async Task FetchMissingTimeAttendanceDataAsync(string accessControlConnectionString, DateTime fromDate, DateTime toDate, string accessIdList, string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(accessControlConnectionString))
        {
            throw new ArgumentException(
                "A real AccessControl database connection string must be supplied (see XML doc remarks).",
                nameof(accessControlConnectionString));
        }

        accessIdList ??= "";
        ipAddress ??= "";

        var timeSettings = await _sysSettingsAccessor.GetTimeSettingsAsync();
        if (timeSettings == null || !timeSettings.EnableTimeAttendanceIntegration)
        {
            return;
        }

        if (accessIdList == "" && ipAddress == "")
        {
            return;
        }

        var updateSql = new StringBuilder();
        updateSql.AppendLine(" UPDATE v_timeevents ");
        updateSql.AppendLine(" SET transfered = 0 ");
        updateSql.AppendLine(" FROM v_timeevents  ");
        updateSql.AppendLine(" WHERE transfered = 1 ");

        if (accessIdList != "")
        {
            updateSql.AppendLine($" AND PIN IN ({accessIdList}) ");
        }

        if (ipAddress != "")
        {
            updateSql.AppendLine($" AND ipaddress IN ('{ipAddress}') ");
        }

        if (fromDate != default)
        {
            updateSql.AppendLine($" AND CONVERT(Datetime, CONVERT(VARCHAR, transdate, 120)) >= '{fromDate:yyyy-MM-dd} 00:00:00'");
        }

        if (toDate != default)
        {
            updateSql.AppendLine($" AND CONVERT(Datetime, CONVERT(VARCHAR, transdate, 120)) <= '{toDate:yyyy-MM-dd} 00:00:00'");
        }

        await using var connection = new OleDbConnection(accessControlConnectionString);
        await connection.OpenAsync();
        await using var command = new OleDbCommand(updateSql.ToString(), connection);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<int> ImportTransactionsAsync()
    {
        var timeSettings = await _sysSettingsAccessor.GetTimeSettingsAsync();
        var filePath = timeSettings?.TimeEventImportPath;
        if (string.IsNullOrWhiteSpace(filePath) || !Directory.Exists(filePath))
        {
            return 0;
        }

        var transTypeList = _transTypeRepository.GetAllList();
        var insertedCount = 0;

        foreach (var file in new DirectoryInfo(filePath).GetFiles("*.xls*"))
        {
            using var workbook = new XLWorkbook(file.FullName);
            var worksheet = workbook.Worksheets.Contains("Sheet1") ? workbook.Worksheet("Sheet1") : workbook.Worksheets.First();

            var lastRowUsed = worksheet.LastRowUsed()?.RowNumber() ?? 0;

            // Legacy: `for (int i = 1; i <= rows.LastUsedIndex; i++)` over 0-based DevExpress rows (row 0 is
            // the header). ClosedXML rows are 1-based, so header row 1 is skipped and data starts at row 2.
            for (var excelRow = 2; excelRow <= lastRowUsed; excelRow++)
            {
                try
                {
                    // DevExpress column indices (0-based) -> ClosedXML columns (1-based) = devIndex + 1.
                    var pin = worksheet.Cell(excelRow, 3).GetString(); // devIndex 2: PIN
                    var transDateRaw = worksheet.Cell(excelRow, 6).GetString(); // devIndex 5: TransDate
                    var transTimeRaw = worksheet.Cell(excelRow, 7).GetString(); // devIndex 6: TransTime

                    if (string.IsNullOrEmpty(pin) || string.IsNullOrEmpty(transDateRaw) || string.IsNullOrEmpty(transTimeRaw))
                    {
                        continue;
                    }

                    var transTypeText = worksheet.Cell(excelRow, 4).GetString(); // devIndex 3: TransType text ("IN"/other)
                    var transTypeCode = string.Equals(transTypeText, "IN", StringComparison.OrdinalIgnoreCase) ? "1" : "2";
                    // devIndex 4 (ClosedXML col 5) held a legacy "ActTransType" value that was computed but
                    // never actually applied to the created record in the original code (dead code) -- not
                    // carried forward here.

                    var recordSourceRaw = worksheet.Cell(excelRow, 8).GetString(); // devIndex 7: RecordSource

                    var transDate = DateTime.Parse(transDateRaw, CultureInfo.InvariantCulture);
                    var transTime = DateTime.Parse(transTimeRaw, CultureInfo.InvariantCulture);
                    var recordSource = long.Parse(recordSourceRaw, CultureInfo.InvariantCulture);

                    var employee = _employeeRepository.GetAll().FirstOrDefault(e => e.AccessID == pin);
                    var transType = transTypeList.FirstOrDefault(t => t.Code == transTypeCode);

                    if (employee == null || transType == null)
                    {
                        continue;
                    }

                    var alreadyImported = _timeEventRepository.GetAll().Any(e => e.RecordSource == recordSource);
                    if (alreadyImported)
                    {
                        continue;
                    }

                    var duplicate = _timeEventRepository.GetAll()
                        .Any(e => e.EmployeeId == employee.Id
                            && e.TransDate == transDate.Date
                            && e.TransTime == transTime
                            && e.TransTypeId == transType.Id);
                    if (duplicate)
                    {
                        continue;
                    }

                    var timeEvent = new TimeEvent
                    {
                        TenantId = _abpSession.TenantId ?? 0,
                        EmployeeId = employee.Id,
                        PIN = pin,
                        ActualTransTypeId = transType.Id,
                        TransTypeId = transType.Id,
                        TransDate = transDate.Date,
                        TransTime = transTime,
                        IPAddress = "",
                        TransSource = TransSource.System,
                        RecordSource = recordSource
                    };

                    await _timeEventRepository.InsertAsync(timeEvent);
                    insertedCount++;
                }
                catch
                {
                    // Matches legacy's per-row try/catch-and-continue for malformed rows.
                }
            }
        }

        return insertedCount;
    }

    public async Task<int> ImportTransactionFromMySqlAsync(string mySqlConnectionString)
    {
        if (string.IsNullOrWhiteSpace(mySqlConnectionString))
        {
            throw new ArgumentException(
                "A real vendor MySQL 'attendance' database connection string must be supplied (see XML doc remarks).",
                nameof(mySqlConnectionString));
        }

        var timeSettings = await _sysSettingsAccessor.GetTimeSettingsAsync();
        if (timeSettings == null || !timeSettings.EnableMySqlIntegration)
        {
            return 0;
        }

        var insertedCount = 0;
        insertedCount += await ImportTransactionFromMySqlByPunchTypeAsync(mySqlConnectionString, timeSettings, isInPunch: true);
        insertedCount += await ImportTransactionFromMySqlByPunchTypeAsync(mySqlConnectionString, timeSettings, isInPunch: false);
        return insertedCount;
    }

    /// <summary>
    /// Handles one of the two legacy IN/OUT queries in <c>ImportTransactionFromMySql()</c> -- see
    /// <see cref="ITimeAttendanceIntegrationService.ImportTransactionFromMySqlAsync"/> XML doc for the
    /// staging-table-collapse architecture note.
    /// </summary>
    private async Task<int> ImportTransactionFromMySqlByPunchTypeAsync(string mySqlConnectionString, ManageTimeSettings timeSettings, bool isInPunch)
    {
        var transTypeCode = isInPunch ? "I" : "O";
        var transType = _transTypeRepository.GetAll().FirstOrDefault(t => t.Code == transTypeCode);
        if (transType == null)
        {
            // Cannot classify punches without a matching TransType -- nothing to import for this half.
            return 0;
        }

        // Legacy: last CHECKINOUT row of this type's TransactionDate, minus one day (else a hardcoded
        // "2023-04-01" fallback). Since this port collapses straight into TimeEvent (no CHECKINOUT staging --
        // see class remarks), the equivalent "since" watermark is the latest already-imported ExternalAPI
        // TimeEvent of this TransType, minus one day, falling back to ManageTimeSettings.EarliestEvaluationDate.
        var lastTransDate = _timeEventRepository.GetAll()
            .Where(e => e.TransSource == TransSource.ExternalAPI && e.TransTypeId == transType.Id)
            .OrderByDescending(e => e.TransDate)
            .Select(e => (DateTime?)e.TransDate)
            .FirstOrDefault();

        var sinceDate = lastTransDate.HasValue ? lastTransDate.Value.AddDays(-1) : timeSettings.EarliestEvaluationDate;

        var query = isInPunch
            ? "SELECT DATE, idattendance_sheet, userid, CASE WHEN att_in IS NOT NULL THEN att_in ELSE '' END AS TIME, 'IN' as Type " +
              "FROM attendance WHERE DATE >= @date AND DATE <= CURRENT_TIMESTAMP() and att_in IS NOT NULL"
            : "SELECT DATE, -1*idattendance_sheet as idattendance_sheet, userid, CASE WHEN att_out IS NOT NULL THEN att_out ELSE '' END AS TIME, 'OUT' as Type " +
              "FROM attendance WHERE DATE >= @date AND DATE <= CURRENT_TIMESTAMP() and att_out IS NOT NULL";

        var insertedCount = 0;

        await using var connection = new MySqlConnection(mySqlConnectionString);
        await connection.OpenAsync();

        await using var command = new MySqlCommand(query, connection) { CommandType = CommandType.Text };
        command.Parameters.AddWithValue("@date", sinceDate.ToString("yyyy-MM-dd"));

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            try
            {
                var dateRaw = reader.GetString(reader.GetOrdinal("DATE"));
                var recordIdRaw = reader.GetString(reader.GetOrdinal("idattendance_sheet"));
                var userId = reader.GetString(reader.GetOrdinal("userid"));
                var timeRaw = reader.GetString(reader.GetOrdinal("TIME"));

                if (string.IsNullOrEmpty(recordIdRaw) || string.IsNullOrEmpty(timeRaw))
                {
                    continue;
                }

                var recordSource = long.Parse(recordIdRaw, CultureInfo.InvariantCulture);

                // Same collision-avoidance convention as legacy: OUT rows negate idattendance_sheet (done in
                // the SQL above) so an IN and OUT punch sharing the same underlying id never collide here.
                var alreadyImported = _timeEventRepository.GetAll()
                    .Any(e => e.RecordSource == recordSource && e.TransSource == TransSource.ExternalAPI);
                if (alreadyImported)
                {
                    continue;
                }

                var employee = _employeeRepository.GetAll().FirstOrDefault(e => e.AccessID == userId);
                if (employee == null)
                {
                    continue;
                }

                var transDate = DateTime.Parse(dateRaw, CultureInfo.InvariantCulture).Date;
                var transTime = DateTime.Parse($"{transDate:yyyy-MM-dd} {timeRaw}", CultureInfo.InvariantCulture);

                var duplicate = _timeEventRepository.GetAll()
                    .Any(e => e.EmployeeId == employee.Id
                        && e.TransDate == transDate
                        && e.TransTime == transTime
                        && e.TransTypeId == transType.Id);
                if (duplicate)
                {
                    continue;
                }

                var timeEvent = new TimeEvent
                {
                    TenantId = _abpSession.TenantId ?? 0,
                    EmployeeId = employee.Id,
                    PIN = userId,
                    ActualTransTypeId = transType.Id,
                    TransTypeId = transType.Id,
                    TransDate = transDate,
                    TransTime = transTime,
                    IPAddress = "",
                    TransSource = TransSource.ExternalAPI,
                    RecordSource = recordSource
                };

                await _timeEventRepository.InsertAsync(timeEvent);
                insertedCount++;
            }
            catch
            {
                // Matches legacy's row-level try/catch-and-continue for malformed rows.
            }
        }

        return insertedCount;
    }
}
