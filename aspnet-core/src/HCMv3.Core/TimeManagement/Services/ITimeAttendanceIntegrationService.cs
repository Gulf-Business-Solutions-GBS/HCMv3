using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Domain.Services;

namespace HCMv3.TimeManagement.Services;

/// <summary>
/// Ported from legacy XAF <c>HCM10.Module.BusinessObjects.Services.TimeAttendanceIntegrationService</c>
/// (the vendor cloud/device time-attendance API integration methods only -- see the implementation's class
/// remarks for the full method-by-method port/defer decision list).
///
/// This service integrates with a per-tenant, externally-hosted "Biotime"-style vendor cloud API
/// (base URLs/credentials/etc. all come from the ported <see cref="TimeAttAPI"/> configuration entity) --
/// it CANNOT function until a tenant has configured a <see cref="TimeAttAPI"/> row with real, live vendor
/// endpoint/credential values. No placeholder URLs/credentials are fabricated here.
/// </summary>
public interface ITimeAttendanceIntegrationService : IDomainService
{
    /// <summary>
    /// Ported from legacy <c>TransferTransactionFromAPI()</c>: syncs from the latest already-imported
    /// <see cref="TimeEvent"/> punch (or <c>ManageTimeSettings.EarliestEvaluationDate</c> if none exist yet)
    /// through "now". No-ops (returns 0) when <c>ManageTimeSettings.EnableTimeAttIntegrationAPI</c> is off or
    /// no <see cref="TimeAttAPI"/> row is configured.
    /// </summary>
    /// <returns>Number of new <see cref="TimeEvent"/> rows inserted.</returns>
    Task<int> TransferTransactionFromApiAsync();

    /// <summary>
    /// Ported from legacy <c>TransferTransactionFromAPIForMissingData(fromDate, toDate, emp_Code, terminal_sn)</c>:
    /// gap-fill sync for a specific date range (and, optionally, employee/terminal), clamped to
    /// [EarliestEvaluationDate, latest already-imported punch date].
    /// </summary>
    /// <returns>Number of new <see cref="TimeEvent"/> rows inserted.</returns>
    Task<int> TransferTransactionFromApiForMissingDataAsync(DateTime fromDate, DateTime toDate, string empCode, string terminalSn);

    /// <summary>
    /// Ported from legacy <c>InsertTransactionFromAPI(starttime, endtime, empcode, terminal_sn)</c>.
    ///
    /// ARCHITECTURE SIMPLIFICATION (documented judgment call): the legacy method wrote into an intermediate
    /// OleDb "CHECKINOUT" staging table (in an external SQL Server "AccessControl" database that also hosts
    /// the raw device-punch <c>v_timeevents</c> view used by <c>TransferTransactionByParam</c>) which some
    /// other, never-ported process later drained into <c>TimeEvent</c>. That staging table/database was never
    /// itself part of this port's schema (no entity, no connection string, no ADO.NET provider referenced in
    /// this solution) and adding one is out of this task's scope (no new entities/migrations allowed). This
    /// port therefore collapses the two hops into one: punches fetched from the vendor API are written
    /// directly into the already-ported <see cref="TimeEvent"/> sink (matching <c>TimeEventsAppService</c>'s
    /// validation: known Employee + TransType required, duplicate-RecordSource-among-Machine-rows skipped).
    /// </summary>
    /// <returns>Number of new <see cref="TimeEvent"/> rows inserted.</returns>
    Task<int> InsertTransactionFromApiAsync(DateTime startTime, DateTime endTime, string empCode, string terminalSn);

    /// <summary>Ported from legacy <c>GetToken(TimeAttAPI)</c>: acquires a JWT bearer token from the vendor's auth endpoint.</summary>
    /// <returns>The access token, or <c>null</c> on any failure (matching legacy swallow-and-return-null behavior).</returns>
    Task<string> GetTokenAsync(TimeAttAPI timeAttAPI);

    /// <summary>
    /// Ported from legacy <c>GetCheckInOutDataList(accessToken, requestUrl)</c>: follows the vendor API's
    /// <c>next</c> pagination link until exhausted, returning every page fetched.
    /// </summary>
    Task<List<CheckInOutHeader>> GetCheckInOutDataListAsync(string accessToken, string requestUrl);

    /// <summary>Ported from legacy <c>GetCheckInOutData(accessToken, starttime, timeAttAPI)</c>: fetches a single page of punches.</summary>
    Task<CheckInOutHeader> GetCheckInOutDataAsync(string accessToken, DateTime startTime, TimeAttAPI timeAttAPI);

    /// <summary>Ported from legacy <c>GetDevicesData(accessToken, DevicesUrl)</c>: fetches a single page of the vendor's device list.</summary>
    Task<ApiDevicesPage> GetDevicesDataAsync(string accessToken, string devicesUrl);

    /// <summary>
    /// Ported from legacy <c>GetEmployeeData(accessToken, timeAttAPI)</c>.
    ///
    /// NOTE: the legacy method ignored <c>timeAttAPI.DevicesUrl</c>/any configured base URL and hardcoded a
    /// literal vendor host (<c>http://78.89.157.63:8080/personnel/api/employees/...</c>) -- almost certainly
    /// a debugging leftover. Per the "do not fabricate endpoints" instruction, this port does NOT carry that
    /// hardcoded IP forward; instead it derives the employees endpoint from <see cref="TimeAttAPI.DevicesUrl"/>'s
    /// host (same vendor API base as every other call in this service), swapping in the standard
    /// "personnel/api/employees/" path. This requires no new configuration field, but if the real vendor
    /// deployment uses a materially different path, <see cref="TimeAttAPI"/> will need a dedicated
    /// EmployeesUrl field added in a follow-up.
    /// </summary>
    Task<ApiEmployeeHeader> GetEmployeeDataAsync(string accessToken, TimeAttAPI timeAttAPI);

    /// <summary>
    /// Ported from legacy <c>DumpAttendanceData()</c>: runs the external "AccessControl" database's
    /// <c>sp_DumpAttendaceData</c> stored procedure, which (per the legacy vendor's own middleware design)
    /// copies raw device punches into that database's <c>v_timeevents</c> view/table so
    /// <see cref="TransferTransactionByParamAsync"/> can subsequently pick them up. No-ops when
    /// <c>ManageTimeSettings.EnableMiddleTierTimeEvents</c> is off.
    /// </summary>
    /// <param name="accessControlConnectionString">
    /// Connection string to the external vendor "AccessControl" SQL Server database. This database/schema is
    /// not part of this application -- callers must supply a real connection string pointing at their actual
    /// AccessControl database instance for this to function. No connection string is stored/fabricated here.
    /// </param>
    Task DumpAttendanceDataAsync(string accessControlConnectionString);

    /// <summary>Ported from legacy <c>TransferTransaction()</c>: convenience overload of <see cref="TransferTransactionByParamAsync"/> with no PIN/IP filter.</summary>
    /// <param name="accessControlConnectionString">See <see cref="DumpAttendanceDataAsync"/> remarks -- caller-supplied, not stored anywhere in this app.</param>
    /// <returns>Number of new <see cref="TimeEvent"/> rows inserted.</returns>
    Task<int> TransferTransactionAsync(string accessControlConnectionString);

    /// <summary>
    /// Ported from legacy <c>TransferTransactionByParam(accessId, accessIpAddress)</c>: reads not-yet-"transfered"
    /// rows off the external "AccessControl" database's <c>v_timeevents</c> view (optionally filtered by PIN
    /// and/or IP address), inserts a corresponding <see cref="TimeEvent"/> for each new/known
    /// (Employee, TransType) combination (skipping duplicates, matching
    /// <see cref="InsertTransactionFromApiAsync"/>'s Employee/TransType/AccessUnit resolution rules), then
    /// flags the successfully-imported source rows as "transfered = 1" back on that same external database.
    /// No-ops when <c>ManageTimeSettings.EnableTimeAttendanceIntegration</c> is off.
    /// </summary>
    /// <param name="accessControlConnectionString">See <see cref="DumpAttendanceDataAsync"/> remarks -- caller-supplied, not stored anywhere in this app.</param>
    /// <param name="accessId">Optional PIN filter (blank = no filter).</param>
    /// <param name="accessIpAddress">Optional IP-address filter (blank = no filter).</param>
    /// <returns>Number of new <see cref="TimeEvent"/> rows inserted.</returns>
    Task<int> TransferTransactionByParamAsync(string accessControlConnectionString, string accessId, string accessIpAddress);

    /// <summary>
    /// Ported from legacy <c>FetchMissingTimeAttendnaceData(fromDate, toDate, accessIdList, ipAddress)</c>:
    /// resets the "transfered" flag back to 0 on the external "AccessControl" database's <c>v_timeevents</c>
    /// rows matching the given filters, so a subsequent <see cref="TransferTransactionByParamAsync"/> run
    /// re-pulls them. No-ops when <c>ManageTimeSettings.EnableTimeAttendanceIntegration</c> is off, or when
    /// both <paramref name="accessIdList"/> and <paramref name="ipAddress"/> are blank (matching legacy).
    /// </summary>
    /// <param name="accessControlConnectionString">See <see cref="DumpAttendanceDataAsync"/> remarks -- caller-supplied, not stored anywhere in this app.</param>
    Task FetchMissingTimeAttendanceDataAsync(string accessControlConnectionString, DateTime fromDate, DateTime toDate, string accessIdList, string ipAddress);

    /// <summary>
    /// Ported from legacy <c>ImportTransactions()</c>: scans <c>ManageTimeSettings.TimeEventImportPath</c> for
    /// <c>*.xls*</c> files and imports each one's "Sheet1" rows (columns, 0-based per legacy DevExpress
    /// indexing: [1]=EmployeeCode (unused for lookup, kept for parity), [2]=PIN, [3]=TransType text ("IN"/other),
    /// [4]=legacy "ActualTransType" text -- computed by legacy but never actually applied to the created record
    /// (dead code in the original; not carried forward), [5]=TransDate, [6]=TransTime, [7]=RecordSource) into
    /// <see cref="TimeEvent"/> rows (<see cref="TransSource.System"/>, matching legacy), skipping rows with a
    /// blank PIN/date/time, an unknown Employee/TransType, or a duplicate (Employee, TransType, TransDate,
    /// TransTime). Uses <c>ClosedXML</c> in place of the legacy commercial <c>DevExpress.Spreadsheet</c> reader.
    /// No-ops (returns 0) when <c>TimeEventImportPath</c> is blank.
    /// </summary>
    /// <returns>Number of new <see cref="TimeEvent"/> rows inserted.</returns>
    Task<int> ImportTransactionsAsync();

    /// <summary>
    /// Ported from legacy <c>ImportTransactionFromMySql()</c>: pulls "IN"/"OUT" punches recorded after the
    /// latest already-imported punch of each type (defaulting to <c>ManageTimeSettings.EarliestEvaluationDate</c>
    /// when none exist yet) from an external vendor MySQL <c>attendance</c> table, and inserts a corresponding
    /// <see cref="TimeEvent"/> for each new/known (Employee, TransType) combination.
    ///
    /// ARCHITECTURE SIMPLIFICATION (documented judgment call, matching the same collapse already applied to
    /// <see cref="InsertTransactionFromApiAsync"/>): the legacy method wrote into the same never-ported
    /// intermediate OleDb "CHECKINOUT" staging table referenced by <see cref="TransferTransactionByParamAsync"/>'s
    /// class remarks, rather than into <see cref="TimeEvent"/> directly. This port collapses that hop and
    /// writes straight into <see cref="TimeEvent"/> (using <see cref="TransSource.ExternalAPI"/> to distinguish
    /// these MySQL-sourced rows from the vendor-cloud-API rows inserted via <see cref="InsertTransactionFromApiAsync"/>
    /// (which use <see cref="TransSource.Machine"/>) and the OleDb-device rows inserted via
    /// <see cref="TransferTransactionByParamAsync"/> (which also use <see cref="TransSource.Machine"/>, matching
    /// legacy exactly)). <c>RecordSource</c> for "OUT" rows is negated (matching legacy's own
    /// <c>-1*idattendance_sheet</c> convention) so IN/OUT rows sharing the same underlying
    /// <c>idattendance_sheet</c> id never collide.
    /// </summary>
    /// <param name="mySqlConnectionString">
    /// Connection string to the external vendor MySQL "attendance" database. This database/schema is not part
    /// of this application -- callers must supply a real connection string pointing at their actual vendor
    /// MySQL instance for this to function. No connection string is stored/fabricated here.
    /// </param>
    /// <returns>Number of new <see cref="TimeEvent"/> rows inserted.</returns>
    Task<int> ImportTransactionFromMySqlAsync(string mySqlConnectionString);
}
