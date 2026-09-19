using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Abp;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Timing;
using HCMv3.GoLiveSetup.Dto;
using HCMv3.PayrollManagement.Services;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSettings;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Services;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GoLiveSetup;

/// <summary>
/// The time engine, run inline for one payroll area and one month, just before the payroll engine -- which pays
/// days from the time registers this writes. Its refusals come back in its own words, like the payroll
/// engine's, as the next thing Go-Live Setup has to answer.
///
/// <para><b>Two things the engine does not do for a caller, done here.</b> It reads four settings records
/// without checking they exist, so a new company's first run would end in a null reference; they are checked
/// first and named instead. And it records a failed day only in its log, still counting the employee as
/// processed; those log rows are read back, so a day it could not work out is not mistaken for a day
/// that went through.</para>
/// </summary>
public class GoLiveTimeEngineStep : AbpServiceBase, ITransientDependency
{
    /// <summary>The time engine's log status for a failure.</summary>
    private const int FailedStatus = 1;

    private readonly ITimeEngineService _timeEngine;
    private readonly IEmployeePayrollAreaResolver _payrollAreaResolver;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<TimeLog> _timeLogRepository;
    private readonly IRepository<ManageTimeSettings> _timeSettings;
    private readonly IRepository<ManagePayrollSettings> _payrollSettings;
    private readonly IRepository<ManagePersonnelSettings> _personnelSettings;
    private readonly IRepository<ManageSecuritySettings> _securitySettings;
    private readonly GoLiveEngineBatches _batches;
    private readonly GoLiveRunTracker _runTracker;

    public GoLiveTimeEngineStep(
        ITimeEngineService timeEngine,
        IEmployeePayrollAreaResolver payrollAreaResolver,
        IRepository<Employee> employeeRepository,
        IRepository<TimeLog> timeLogRepository,
        IRepository<ManageTimeSettings> timeSettings,
        IRepository<ManagePayrollSettings> payrollSettings,
        IRepository<ManagePersonnelSettings> personnelSettings,
        IRepository<ManageSecuritySettings> securitySettings,
        GoLiveEngineBatches batches,
        GoLiveRunTracker runTracker)
    {
        LocalizationSourceName = HCMv3Consts.LocalizationSourceName;

        _timeEngine = timeEngine;
        _payrollAreaResolver = payrollAreaResolver;
        _employeeRepository = employeeRepository;
        _timeLogRepository = timeLogRepository;
        _timeSettings = timeSettings;
        _payrollSettings = payrollSettings;
        _personnelSettings = personnelSettings;
        _securitySettings = securitySettings;
        _batches = batches;
        _runTracker = runTracker;
    }

    /// <summary>
    /// The codes of the area's active employees on the pay date, in code order -- the people both engines run
    /// for, and the batches they run them in.
    /// </summary>
    public async Task<List<string>> EmployeeCodesAsync(string payrollAreaName, DateTime payInDate)
    {
        var ids = await _payrollAreaResolver.GetEmployeeIdsInPayrollAreaAsync(payrollAreaName, payInDate);
        return await _employeeRepository.GetAll()
            .Where(e => ids.Contains(e.Id) && e.Status == EmployeeStatus.Active)
            .OrderBy(e => e.Code)
            .Select(e => e.Code)
            .ToListAsync();
    }

    /// <summary>
    /// Runs the month for the area's active employees. Returns null when the time engine went through -- or had
    /// nobody to run, which the payroll engine then reports in its own way -- and the refusal otherwise.
    /// </summary>
    public async Task<GoLiveRunPayrollOutput> RunAsync(
        string payrollAreaName,
        DateTime payInDate,
        string currentUserName,
        IReadOnlyCollection<string> onlyCodes = null)
    {
        // The engine's own payroll-area filter does not filter -- it takes every active employee -- so the area's
        // employees are found the way the payroll engine finds them, and named.
        var ids = await _payrollAreaResolver.GetEmployeeIdsInPayrollAreaAsync(payrollAreaName, payInDate);
        var employees = await _employeeRepository.GetAll()
            .Where(e => ids.Contains(e.Id) && e.Status == EmployeeStatus.Active)
            .Select(e => new { e.Id, e.Code })
            .ToListAsync();

        // Only the employees picked to run, when some were.
        if (onlyCodes != null)
        {
            var picked = new HashSet<string>(onlyCodes, StringComparer.OrdinalIgnoreCase);
            employees = employees.Where(e => picked.Contains(e.Code)).ToList();
        }

        if (employees.Count == 0)
        {
            return null;
        }

        var missing = await MissingSettingsAsync();
        if (missing.Count > 0)
        {
            return Stopped(L("GoLiveTimeSettingsMissing", string.Join(", ", missing)));
        }

        var runStart = Clock.Now;

        try
        {
            // A batch at a time, each saved on its own, so the tenth batch is as quick as the first.
            foreach (var batch in employees.Select(e => e.Code).Chunk(Math.Max(1, _batches.Size)))
            {
                if (await _runTracker.IsStopRequestedAsync())
                {
                    return new GoLiveRunPayrollOutput
                    {
                        StoppedAt = GoLiveStoppedAt.ByUser,
                        Message = L("GoLiveRunStoppedByUser"),
                    };
                }

                using var uow = UnitOfWorkManager.Begin(TransactionScopeOption.RequiresNew);
                await _timeEngine.CreateTimeRegisterAsync(new TimeEngineRunInput
                {
                    StartDate = new DateTime(payInDate.Year, payInDate.Month, 1),
                    EndDate = payInDate,
                    PayrollAreaName = payrollAreaName,
                    EmployeeCodes = string.Join(",", batch),
                    CurrentUserName = currentUserName,
                    // As the payroll engine would run it: someone joining after the month end is skipped, not an error.
                    IsRanFromPayrollEngine = true,
                });
                await uow.CompleteAsync();
            }
        }
        catch (Exception ex)
        {
            // The engine's run-level refusals are plain exceptions, not UserFriendlyException, so none is let
            // through here -- but each is logged in full, since a crash and a refusal look alike from the screen.
            Logger.Error("Go-Live Setup: the time engine stopped.", ex);
            return Stopped(ex.Message);
        }

        await CurrentUnitOfWork.SaveChangesAsync();

        var failures = await _timeLogRepository.GetAll()
            .Where(l => l.Status == FailedStatus && l.RunTimeStamp >= runStart)
            .OrderBy(l => l.DayDate)
            .Select(l => new { l.EmployeeId, l.Message })
            .ToListAsync();

        if (failures.Count == 0)
        {
            return null;
        }

        var codes = employees.ToDictionary(e => e.Id, e => e.Code);
        var output = Stopped(failures[0].Message);

        // One line per employee: their first failed day, and how many more there were. The same refusal on thirty
        // days is one thing to fix, not thirty.
        output.Employees = failures
            .GroupBy(f => f.EmployeeId)
            .Select(g => new GoLiveEmployeeResultDto
            {
                EmployeeCode = g.Key.HasValue && codes.TryGetValue(g.Key.Value, out var code) ? code : string.Empty,
                Succeeded = false,
                Message = g.Count() > 1 ? L("GoLiveTimeEngineMoreDays", g.First().Message, g.Count() - 1) : g.First().Message,
            })
            .ToList();

        return output;
    }

    private async Task<List<string>> MissingSettingsAsync()
    {
        var missing = new List<string>();

        if (!await _timeSettings.GetAll().AnyAsync())
        {
            missing.Add(L("GoLiveSettingsTime"));
        }

        if (!await _payrollSettings.GetAll().AnyAsync())
        {
            missing.Add(L("GoLiveSettingsPayroll"));
        }

        if (!await _personnelSettings.GetAll().AnyAsync())
        {
            missing.Add(L("GoLiveSettingsPersonnel"));
        }

        if (!await _securitySettings.GetAll().AnyAsync())
        {
            missing.Add(L("GoLiveSettingsSecurity"));
        }

        return missing;
    }

    private static GoLiveRunPayrollOutput Stopped(string message) =>
        new() { StoppedAt = GoLiveStoppedAt.TimeEngine, Message = message };
}
