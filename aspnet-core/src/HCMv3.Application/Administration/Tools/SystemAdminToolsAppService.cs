using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Timing;
using Abp.UI;
using HCMv3.Administration.Tools.Dto;
using HCMv3.Authorization;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSettings;
using HCMv3.SystemSettings.Services;
using HCMv3.TimeManagement.Services;
using Microsoft.AspNetCore.Mvc;

namespace HCMv3.Administration.Tools;

/// <inheritdoc cref="ISystemAdminToolsAppService"/>
/// <remarks>
/// <para><b>DELIBERATE DEVIATION — permission instead of a username check</b> (legacy
/// <c>CheckIfAdminUser()</c>, uscSystemConfigPanel.ascx.cs line 59).</para>
///
/// <para><b>DELIBERATE DEVIATION — password tools moved.</b> Legacy's credential-minting callbacks on this
/// panel live in <see cref="UserCredentialResetAppService"/> (Reset User Passwords), behind its own
/// <c>Pages_Administration_Tools_ResetUserCredentials</c> permission.</para>
///
/// <para><b>NOT PORTED — "Reset Model Navigation Items Nodes" (legacy line 83) and "Migrate Attachments"
/// (legacy line 309).</b> Justified on <see cref="ISystemAdminToolsAppService"/>.</para>
/// </remarks>
[AbpAuthorize(AppPermissions.Pages_Administration_Tools)]
public class SystemAdminToolsAppService : HCMv3AppServiceBase, ISystemAdminToolsAppService
{
    /// <summary>The access-control database connection saved in the Integration Center, if any.</summary>
    public HCMv3.Integrations.Connectors.AccessControlDbConnector AccessControlDbConnector { get; set; }

    private readonly IRepository<Employee> _employeeRepository;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;
    private readonly ITimeAttendanceIntegrationService _timeAttendanceIntegrationService;

    public SystemAdminToolsAppService(
        IRepository<Employee> employeeRepository,
        ISysSettingsAccessor sysSettingsAccessor,
        ITimeAttendanceIntegrationService timeAttendanceIntegrationService)
    {
        _employeeRepository = employeeRepository;
        _sysSettingsAccessor = sysSettingsAccessor;
        _timeAttendanceIntegrationService = timeAttendanceIntegrationService;
    }

    #region Recalculate ProbationDays (legacy line 795)

    [HttpPost]
    [AbpAuthorize(AppPermissions.Pages_Administration_Tools_RecalculateProbation)]
    public async Task<AdminToolResultDto> RecalculateProbationEndDates()
    {
        var result = new AdminToolResultDto();

        var personnelSettings = await _sysSettingsAccessor.GetPersonnelSettingsAsync();
        if (personnelSettings == null)
        {
            throw new UserFriendlyException(L("PersonnelSettingsNotConfigured"));
        }

        // Legacy Employee.GetProbationEndDate (HCM10.Module Employee.cs line 2159) has two branches. Only
        // the CalendarDays branch is available in this port: Employee.ProbationEndDate's own XML docs record
        // that the WorkingDays branch (which walks DutyPeriod/HolidayCalendar/unpaid EmployeeAbsence rows,
        // legacy lines 2169-2230) was not ported. Failing loudly beats silently writing calendar-day dates
        // over every employee of a working-days tenant.
        if (personnelSettings.ProbationDaysCalculation != ProbationDaysCalculation.CalendarDays)
        {
            throw new UserFriendlyException(L("ProbationRecalculationSupportsCalendarDaysOnly"));
        }

        var employees = _employeeRepository.GetAll()
            .Where(r => r.Status == EmployeeStatus.Active)
            .OrderBy(r => r.Code)
            .ToList();

        foreach (var employee in employees)
        {
            var newProbationEndDate = employee.JoiningDate.AddDays(employee.ProbationDays);
            if (employee.ProbationEndDate == newProbationEndDate)
            {
                result.SkippedCount++;
                continue;
            }

            employee.ProbationEndDate = newProbationEndDate;
            await _employeeRepository.UpdateAsync(employee);

            result.AffectedCount++;
            result.Messages.Add($"{employee.Code}: {newProbationEndDate:dd/MM/yyyy}");
        }

        return result;
    }

    #endregion

    #region Time attendance re-sync (legacy lines 844 and 899)

    [HttpPost]
    [AbpAuthorize(AppPermissions.Pages_Administration_Tools_TimeAttendanceResync)]
    public async Task<AdminToolResultDto> FetchMissingTimeAttendanceData(FetchMissingTimeAttendanceDataInput input)
    {
        var result = new AdminToolResultDto();

        // A connection string typed on the screen wins; otherwise the one saved in the Integration Center.
        var connectionString = input.AccessControlConnectionString;
        if (connectionString.IsNullOrWhiteSpace() && AccessControlDbConnector != null)
        {
            connectionString = await AccessControlDbConnector.GetConnectionStringAsync();
        }

        if (connectionString.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException(L("AccessControlConnectionStringIsRequired"));
        }

        var (fromDate, toDate, employees) = await ResolveResyncScopeAsync(input);
        if (employees.Count == 0)
        {
            return result;
        }

        // Legacy lines 878-887 built a SQL-ready quoted, comma-separated AccessID list and passed it
        // straight through. FetchMissingTimeAttendanceDataAsync's ported contract expects the same shape.
        var accessIdList = string.Join(",", employees.Select(r => "'" + r.AccessID + "'"));

        await _timeAttendanceIntegrationService.FetchMissingTimeAttendanceDataAsync(
            connectionString, fromDate, toDate, accessIdList, string.Empty);

        result.AffectedCount = employees.Count;
        return result;
    }

    [HttpPost]
    [AbpAuthorize(AppPermissions.Pages_Administration_Tools_TimeAttendanceResync)]
    public async Task<AdminToolResultDto> SyncMissingTimeAttendanceApiData(FetchMissingTimeAttendanceDataInput input)
    {
        var result = new AdminToolResultDto();

        var (fromDate, toDate, employees) = await ResolveResyncScopeAsync(input);

        // Legacy lines 933-945: when explicit employee codes were supplied it loops one call per employee
        // passing that employee's AccessID; otherwise it makes ONE call with a blank employee filter (which
        // means "everyone" to the vendor API), ignoring the employee list entirely.
        var hasEmployeeCodes = input.EmployeeCodes != null && input.EmployeeCodes.Length > 0;

        if (hasEmployeeCodes)
        {
            foreach (var employee in employees)
            {
                var transferred = await _timeAttendanceIntegrationService
                    .TransferTransactionFromApiForMissingDataAsync(fromDate, toDate, employee.AccessID, string.Empty);

                result.AffectedCount += transferred;
                result.Messages.Add($"{employee.Code}: {transferred}");
            }
        }
        else
        {
            result.AffectedCount = await _timeAttendanceIntegrationService
                .TransferTransactionFromApiForMissingDataAsync(fromDate, toDate, string.Empty, string.Empty);
        }

        return result;
    }

    /// <summary>
    /// Shared translation of the filter block legacy duplicated at lines 856-876 and 911-931.
    /// </summary>
    private async Task<(DateTime FromDate, DateTime ToDate, System.Collections.Generic.List<Employee> Employees)>
        ResolveResyncScopeAsync(FetchMissingTimeAttendanceDataInput input)
    {
        var timeSettings = await _sysSettingsAccessor.GetTimeSettingsAsync();
        if (timeSettings == null)
        {
            throw new UserFriendlyException(L("TimeSettingsNotConfigured"));
        }

        var fromDate = input.FromDate ?? default;
        if (fromDate == default || fromDate < timeSettings.EarliestEvaluationDate)
        {
            fromDate = timeSettings.EarliestEvaluationDate;
        }

        // Legacy line 875 / 930: the upper bound is always "now".
        var toDate = Clock.Now;

        var hasEmployeeCodes = input.EmployeeCodes != null && input.EmployeeCodes.Length > 0;

        // Legacy lines 860-864: supplying codes replaces the whole predicate, dropping both the
        // Status==Active and the "has an AccessID" filters. Ported faithfully, but rows without an AccessID
        // are then useless to the integration call, so they are filtered out here.
        var employees = (hasEmployeeCodes
                ? _employeeRepository.GetAll().Where(r => input.EmployeeCodes.Contains(r.Code))
                : _employeeRepository.GetAll().Where(r => r.Status == EmployeeStatus.Active
                                                          && r.AccessID != null
                                                          && r.AccessID != ""))
            .OrderBy(r => r.Code)
            .ToList()
            .Where(r => !r.AccessID.IsNullOrWhiteSpace())
            .ToList();

        return (fromDate, toDate, employees);
    }

    #endregion
}
