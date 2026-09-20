using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;
using System.Transactions;
using Abp.Application.Services.Dto;
using Abp.BackgroundJobs;
using Abp.Timing;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Runtime.Session;
using Abp.UI;
using HCMv3.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HCMv3.GeneralMasterInfo;
using HCMv3.Storage;
using HCMv3.Dto;
using HCMv3.GridReports.PayrollReport;
using HCMv3.GridReports.PayrollReport.Dto;
using HCMv3.GoLiveSetup.ReferenceData;
using HCMv3.GoLiveSetup.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using HCMv3.PayrollManagement.Services;

namespace HCMv3.GoLiveSetup;

/// <inheritdoc cref="IGoLiveSetupAppService"/>
/// <remarks>
/// Each action also needs the permission of the thing it does: running payroll writes real salary registers
/// and setting up a month creates payroll master data, so Go-Live Setup must not become a way round either.
/// </remarks>
[AbpAuthorize(AppPermissions.Pages_GoLiveSetup)]
public class GoLiveSetupAppService : HCMv3AppServiceBase, IGoLiveSetupAppService
{
    private const string DefaultCurrencyCode = "KWD";
    private const string DefaultPayYearCode = "Jan-Dec";

    /// <summary>The reference pack with the payroll catalogue: wage types, leave types and the links between them.</summary>
    private const string PayrollCataloguePack = "payroll";

    /// <summary>The Kuwait pack's open-ended contract type, matching the 31/12/9999 end date every contract gets.</summary>
    private const string DefaultContractType = "Unlimited";

    /// <summary>Month end: everything entered during the month is included, which is the least surprising.</summary>
    private const int MonthEndCutOff = 31;

    /// <summary>A go-live company is one payroll area; no page of it is worth splitting a report over.</summary>
    private const int MaxReportRows = 100000;

    private readonly IPayrollEngineService _payrollEngine;
    private readonly IPayrollAreasAppService _payrollAreas;
    private readonly IPayrollControlRecordsAppService _controlRecords;
    private readonly IPayrollCalendarsAppService _calendars;
    private readonly IRepository<PayrollArea> _payrollAreaRepository;
    private readonly IRepository<PayrollControlRecord> _controlRecordRepository;
    private readonly IRepository<PayrollCalendar> _calendarRepository;
    private readonly IRepository<Currency> _currencyRepository;
    private readonly IRepository<PayrollPeriod> _payYearRepository;
    private readonly IRepository<ContractType> _contractTypeRepository;
    private readonly IRepository<WageType> _wageTypeRepository;
    private readonly GoLiveEmployeeDefaults _employeeDefaults;
    private readonly GoLivePlaceholderLookups _placeholders;
    private readonly GoLiveEmployeeLoader _employeeLoader;
    private readonly GoLiveTimeEngineStep _timeEngineStep;
    private readonly GoLiveWorkingTime _workingTime;
    private readonly GoLiveLoadedEmployeesReader _loadedEmployees;
    private readonly GoLivePayrollResultsReader _payrollResults;
    private readonly IReferenceDataSeeder _referenceData;
    private readonly IRepository<WageTypeLeave> _wageTypeLeaveRepository;
    private readonly IRepository<Bank> _bankRepository;
    private readonly GoLiveBankFileStep _bankFileStep;
    private readonly GoLiveGridReader _gridReader;
    private readonly GoLiveCompanyClearer _companyClearer;
    private readonly GoLiveRunTracker _runTracker;
    private readonly GoLiveEngineBatches _batches;
    private readonly IBackgroundJobManager _backgroundJobs;
    private readonly IPayrollSheetReportAppService _payrollSheetReport;
    private readonly ITempFileCacheManager _tempFiles;
    private readonly GoLiveLegacyComparer _legacyComparer;
    private readonly GoLiveOpeningBalances _openingBalances;
    private readonly GoLiveExcelExports _excelExports;
    private readonly GoLiveLegacyPayments _legacyPayments;

    public GoLiveSetupAppService(
        IPayrollEngineService payrollEngine,
        IPayrollAreasAppService payrollAreas,
        IPayrollControlRecordsAppService controlRecords,
        IPayrollCalendarsAppService calendars,
        IRepository<PayrollArea> payrollAreaRepository,
        IRepository<PayrollControlRecord> controlRecordRepository,
        IRepository<PayrollCalendar> calendarRepository,
        IRepository<Currency> currencyRepository,
        IRepository<PayrollPeriod> payYearRepository,
        IRepository<ContractType> contractTypeRepository,
        IRepository<WageType> wageTypeRepository,
        GoLiveEmployeeDefaults employeeDefaults,
        GoLivePlaceholderLookups placeholders,
        GoLiveEmployeeLoader employeeLoader,
        GoLiveTimeEngineStep timeEngineStep,
        GoLiveWorkingTime workingTime,
        GoLiveLoadedEmployeesReader loadedEmployees,
        GoLivePayrollResultsReader payrollResults,
        IReferenceDataSeeder referenceData,
        IRepository<WageTypeLeave> wageTypeLeaveRepository,
        IRepository<Bank> bankRepository,
        GoLiveBankFileStep bankFileStep,
        GoLiveGridReader gridReader,
        GoLiveCompanyClearer companyClearer,
        GoLiveRunTracker runTracker,
        GoLiveEngineBatches batches,
        IBackgroundJobManager backgroundJobs,
        IPayrollSheetReportAppService payrollSheetReport,
        ITempFileCacheManager tempFiles,
        GoLiveLegacyComparer legacyComparer,
        GoLiveOpeningBalances openingBalances,
        GoLiveExcelExports excelExports,
        GoLiveLegacyPayments legacyPayments)
    {
        _payrollEngine = payrollEngine;
        _payrollAreas = payrollAreas;
        _controlRecords = controlRecords;
        _calendars = calendars;
        _payrollAreaRepository = payrollAreaRepository;
        _controlRecordRepository = controlRecordRepository;
        _calendarRepository = calendarRepository;
        _currencyRepository = currencyRepository;
        _payYearRepository = payYearRepository;
        _contractTypeRepository = contractTypeRepository;
        _wageTypeRepository = wageTypeRepository;
        _employeeDefaults = employeeDefaults;
        _placeholders = placeholders;
        _employeeLoader = employeeLoader;
        _timeEngineStep = timeEngineStep;
        _workingTime = workingTime;
        _loadedEmployees = loadedEmployees;
        _payrollResults = payrollResults;
        _referenceData = referenceData;
        _wageTypeLeaveRepository = wageTypeLeaveRepository;
        _bankRepository = bankRepository;
        _bankFileStep = bankFileStep;
        _gridReader = gridReader;
        _companyClearer = companyClearer;
        _runTracker = runTracker;
        _batches = batches;
        _backgroundJobs = backgroundJobs;
        _payrollSheetReport = payrollSheetReport;
        _tempFiles = tempFiles;
        _legacyComparer = legacyComparer;
        _openingBalances = openingBalances;
        _excelExports = excelExports;
        _legacyPayments = legacyPayments;
    }

    /// <remarks>Runs the time engine too, so it needs that engine's permission as well as the payroll engine's.</remarks>
    [AbpAuthorize(
        AppPermissions.Pages_PayrollManagement_PayrollEngine_Run,
        AppPermissions.Pages_TimeManagement_TimeEngine_Run,
        RequireAllPermissions = true)]
    public async Task<GoLiveRunPayrollOutput> RunPayroll(GoLiveRunPayrollInput input)
    {
        var prepared = await PrepareRunAsync(input);
        if (prepared.Refusal != null)
        {
            return prepared.Refusal;
        }

        var output = await RunEnginesAsync(prepared.AreaName, prepared.PayInDate, prepared.EmployeeCodes);
        output.MonthSetUp = prepared.MonthSetUp;
        output.LeavePayLinksAdded = prepared.LeavePayLinksAdded;
        return output;
    }

    /// <remarks>The same permissions as a direct run: it is one, only not waited on.</remarks>
    [AbpAuthorize(
        AppPermissions.Pages_PayrollManagement_PayrollEngine_Run,
        AppPermissions.Pages_TimeManagement_TimeEngine_Run,
        RequireAllPermissions = true)]
    public async Task<GoLivePayrollRunProgressDto> StartPayrollRun(GoLiveRunPayrollInput input)
    {
        var current = await _runTracker.ProgressAsync();
        if (GoLiveRunTracker.IsGoing(current.Status))
        {
            return current;
        }

        var prepared = await PrepareRunAsync(input);
        var state = new GoLivePayrollRunState
        {
            PayrollAreaName = prepared.AreaName,
            PayInDate = prepared.PayInDate,
            StartedAt = Clock.Now,
            MonthSetUp = prepared.MonthSetUp,
            LeavePayLinksAdded = prepared.LeavePayLinksAdded,
        };

        if (prepared.Refusal != null)
        {
            prepared.Refusal.MonthSetUp ??= prepared.MonthSetUp;
            state.Status = GoLiveRunStatus.Completed;
            state.FinishedAt = Clock.Now;
            state.Result = prepared.Refusal;
            await _runTracker.SaveAsync(state);
            return await _runTracker.ProgressAsync();
        }

        state.Status = GoLiveRunStatus.Queued;
        state.TotalEmployees = prepared.EmployeeCodes?.Count
                               ?? (await _timeEngineStep.EmployeeCodesAsync(prepared.AreaName, prepared.PayInDate)).Count;
        state.BaselineEngineRunId = await _runTracker.LastEngineRunIdAsync();
        await _runTracker.SaveAsync(state);

        await _backgroundJobs.EnqueueAsync<GoLivePayrollRunJob, GoLivePayrollRunJobArgs>(new GoLivePayrollRunJobArgs
        {
            TenantId = AbpSession.GetTenantId(),
            UserId = AbpSession.GetUserId(),
            PayrollAreaName = prepared.AreaName,
            PayInDate = prepared.PayInDate,
            EmployeeCodes = prepared.EmployeeCodes?.ToList(),
        });

        return await _runTracker.ProgressAsync();
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_SalaryRegisters)]
    public Task<GoLivePayrollRunProgressDto> GetPayrollRunProgress(string payrollAreaName) =>
        _runTracker.ProgressAsync();

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_PayrollEngine_Run)]
    public Task StopPayrollRun() =>
        _runTracker.UpdateAsync(state =>
        {
            if (GoLiveRunTracker.IsGoing(state.Status))
            {
                state.StopRequested = true;
            }
        });

    /// <summary>
    /// Everything a run needs answered before an engine starts, in the order the engine would ask: the inputs,
    /// the month set up, the payroll month being the one asked for, the leave pay links. A refusal here costs
    /// nobody a wait.
    /// </summary>
    private async Task<PreparedRun> PrepareRunAsync(GoLiveRunPayrollInput input)
    {
        input ??= new GoLiveRunPayrollInput();

        // The same two checks, in the same words, as IPayrollEngineAppService.RunPayrollEngine makes before it
        // queues a run -- so Go-Live Setup meets exactly the refusals production does, in the same order.
        if (input.PayDate == null)
        {
            return PreparedRun.Refused(Stopped(GoLiveStoppedAt.Input, L("PayrollEnginePayInDateRequired")));
        }

        if (string.IsNullOrWhiteSpace(input.PayrollAreaName))
        {
            return PreparedRun.Refused(Stopped(GoLiveStoppedAt.Input, L("PayrollEnginePayrollAreaRequired")));
        }

        var areaName = input.PayrollAreaName.Trim();
        var payInDate = MonthEnd(input.PayDate.Value);

        // The month first. A missing control record or pay calendar was the engine's third and fourth refusal,
        // and answering them needs nothing the customer has not already given -- so it is done here, once, and
        // reported, instead of being a separate button to press between two runs.
        GoLiveSetUpPayrollMonthOutput monthSetUp = null;
        if (await NextStepAsync(areaName, payInDate) == GoLiveNextStep.SetUpPayrollMonth)
        {
            try
            {
                monthSetUp = await SetUpPayrollMonth(new GoLiveSetUpPayrollMonthInput { PayDate = payInDate, PayrollAreaName = areaName });
            }
            catch (UserFriendlyException ex)
            {
                return PreparedRun.Refused(Stopped(GoLiveStoppedAt.MonthSetUp, ex.Message));
            }
        }

        // The payroll engine refuses a month its control record is not for -- locked, and ending on the pay
        // date -- but only after the time engine has worked out every employee's days. Asked here instead.
        var area = await _payrollAreaRepository.FirstOrDefaultAsync(a => a.Name == areaName);
        var record = area == null ? null : await _controlRecordRepository.FirstOrDefaultAsync(r => r.PayrollAreaId == area.Id);
        if (record != null && (record.CalendarMonthEnd.Date != payInDate.Date || !record.PeriodLocked))
        {
            var refusal = Stopped(
                GoLiveStoppedAt.MonthSetUp,
                L("GoLiveControlRecordNotCurrent", MonthName(record.CalendarMonthEnd), MonthName(payInDate)));
            refusal.PayInDate = payInDate;
            return PreparedRun.Refused(refusal, areaName, payInDate, monthSetUp);
        }

        // The employees picked to run, if any, in the payroll area's own spelling. A code the area does not have
        // stops the run here, before either engine starts, and says which.
        IReadOnlyList<string> employeeCodes = null;
        var picked = (input.EmployeeCodes ?? new List<string>())
            .Select(c => c?.Trim())
            .Where(c => !string.IsNullOrEmpty(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (picked.Count > 0)
        {
            var inArea = (await _timeEngineStep.EmployeeCodesAsync(areaName, payInDate))
                .ToDictionary(c => c.Trim(), c => c, StringComparer.OrdinalIgnoreCase);
            var unknown = picked.Where(c => !inArea.ContainsKey(c)).ToList();
            if (unknown.Count > 0)
            {
                var refusal = Stopped(
                    GoLiveStoppedAt.Input,
                    L("GoLiveRunCodesNotInArea", unknown.Count, string.Join(", ", unknown)));
                refusal.PayInDate = payInDate;
                return PreparedRun.Refused(refusal, areaName, payInDate, monthSetUp);
            }

            employeeCodes = picked.Select(c => inArea[c]).ToList();
        }

        var leavePayLinksAdded = await EnsureLeavePayLinksAsync();

        // Opening balances count in the month they are dated: moved onto this month for anyone not yet paid.
        await _openingBalances.DateForRunAsync(areaName, payInDate);

        return new PreparedRun
        {
            AreaName = areaName,
            PayInDate = payInDate,
            MonthSetUp = monthSetUp,
            LeavePayLinksAdded = leavePayLinksAdded,
            EmployeeCodes = employeeCodes,
        };
    }

    private static string MonthName(DateTime date) => date.ToString("MMMM yyyy", CultureInfo.InvariantCulture);

    /// <summary>What preparing a run found: a refusal, or the area and month to run, and what it set up.</summary>
    private sealed class PreparedRun
    {
        public GoLiveRunPayrollOutput Refusal { get; init; }

        public string AreaName { get; init; }

        public DateTime PayInDate { get; init; }

        public GoLiveSetUpPayrollMonthOutput MonthSetUp { get; init; }

        public int LeavePayLinksAdded { get; init; }

        /// <summary>The employees picked to run; null runs everyone in the payroll area.</summary>
        public IReadOnlyList<string> EmployeeCodes { get; init; }

        public static PreparedRun Refused(
            GoLiveRunPayrollOutput refusal,
            string areaName = null,
            DateTime payInDate = default,
            GoLiveSetUpPayrollMonthOutput monthSetUp = null) =>
            new() { Refusal = refusal, AreaName = areaName, PayInDate = payInDate, MonthSetUp = monthSetUp };
    }

    /// <summary>
    /// A company with no "paid during leave" links at all -- one seeded before the catalogue carried them -- gets
    /// the legacy catalogue's, through the same seeder that seeds a new company; it adds only what is missing. A
    /// company with any links of its own has chosen them, and is left alone.
    /// </summary>
    private async Task<int> EnsureLeavePayLinksAsync()
    {
        if (await _wageTypeLeaveRepository.GetAll().AnyAsync())
        {
            return 0;
        }

        var result = await _referenceData.SeedPackAsync(PayrollCataloguePack, AbpSession.GetTenantId());
        await CurrentUnitOfWork.SaveChangesAsync();

        return result.Counts.Where(c => c.Lookup == nameof(WageTypeLeave)).Sum(c => c.Inserted);
    }

    /// <summary>The time engine, then the payroll engine, for a month that is set up.</summary>
    private async Task<GoLiveRunPayrollOutput> RunEnginesAsync(string areaName, DateTime payInDate, IReadOnlyList<string> employeeCodes = null)
    {
        // The time engine first: the payroll pays days from the time registers it writes for the month. With
        // nobody in the area it has nothing to do, and the payroll engine says so in its own words.
        var timeEngineStopped = await _timeEngineStep.RunAsync(areaName, payInDate, (await GetCurrentUserAsync()).UserName, employeeCodes);
        if (timeEngineStopped != null)
        {
            timeEngineStopped.PayInDate = payInDate;
            return timeEngineStopped;
        }

        var codes = employeeCodes?.ToList() ?? await _timeEngineStep.EmployeeCodesAsync(areaName, payInDate);
        var results = new List<GoLiveEmployeeResultDto>();
        var paidFor = payInDate;
        var aborted = false;

        // A batch at a time, each saved on its own: the engine keeps everything it writes for a call in memory
        // until the call ends, so one call for everyone gets slower with every employee. With nobody to pay,
        // one call as before -- the engine then says so in its own words.
        var batches = codes.Count == 0 ? new List<string[]> { null } : codes.Chunk(Math.Max(1, _batches.Size)).ToList();

        try
        {
            foreach (var batch in batches)
            {
                if (await _runTracker.IsStopRequestedAsync())
                {
                    return StoppedByUser(payInDate, results);
                }

                PayrollEngineRunResult batchRun;
                using (var uow = UnitOfWorkManager.Begin(TransactionScopeOption.RequiresNew))
                {
                    batchRun = await _payrollEngine.RunAsync(new PayrollEngineRunInput
                    {
                        PayrollRunType = PayrollRunType.Regular,
                        PayInDate = payInDate,
                        PayrollAreaName = areaName,
                        RegisterTarget = PayrollRegisterTarget.Live,
                        EmployeeCodes = batch == null ? null : string.Join(",", batch),
                    });
                    await uow.CompleteAsync();
                }

                paidFor = batchRun.PayInDate;
                aborted |= batchRun.Aborted;
                results.AddRange(batchRun.EmployeeResults.Select(e => new GoLiveEmployeeResultDto
                {
                    EmployeeCode = e.EmployeeCode,
                    Succeeded = e.Succeeded,
                    Message = e.Message,
                }));
            }
        }
        catch (UserFriendlyException ex)
        {
            // Every run-level refusal the engine makes is a UserFriendlyException with its own wording. Anything
            // else is a genuine fault and is allowed to surface as one.
            var stopped = Stopped(GoLiveStoppedAt.Engine, ex.Message);
            stopped.NextStep = await NextStepAsync(areaName, payInDate);
            return stopped;
        }

        var run = new { PayInDate = paidFor, Aborted = aborted };
        var output = new GoLiveRunPayrollOutput
        {
            PayInDate = run.PayInDate,
            Employees = results,
        };

        var firstFailure = output.Employees.FirstOrDefault(e => !e.Succeeded);

        if (firstFailure != null)
        {
            // The first refusal leads, because it is the next thing to fix; the rest are in the list.
            output.StoppedAt = GoLiveStoppedAt.Employees;
            output.Message = firstFailure.Message;
        }
        else if (output.Employees.Count == 0)
        {
            // The engine treats an empty payroll area as a finished run. For go-live it is the opposite.
            output.StoppedAt = GoLiveStoppedAt.NoEmployees;
            output.NextStep = GoLiveNextStep.LoadEmployees;
        }
        else
        {
            output.Succeeded = !run.Aborted;
            output.StoppedAt = run.Aborted ? GoLiveStoppedAt.Engine : GoLiveStoppedAt.None;
        }

        return output;
    }

    /// <summary>
    /// Puts every locked period of this company into "released for correction" before Go-Live changes data.
    ///
    /// <para>Go-Live loads salaries, org assignments and payments -- exactly the records the payroll lock freezes
    /// while a period is locked. Rather than leaving the user to unlock by hand, or quietly bypassing the lock,
    /// Go-Live records the release: the control record says the period was reopened to correct data, by whom and
    /// when. SetUpPayrollMonth locks it again before the next run.</para>
    /// </summary>
    private async Task ReleaseForCorrectionAsync()
    {
        var locked = await _controlRecordRepository.GetAllListAsync(r => r.PeriodLocked);

        foreach (var record in locked)
        {
            record.ApplyState(PayrollPeriodState.ReleasedForCorrection, AbpSession.UserId, Clock.Now);
            await _controlRecordRepository.UpdateAsync(record);
        }

        if (locked.Count > 0)
        {
            await CurrentUnitOfWork.SaveChangesAsync();
        }
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_PayrollControlRecords_Create)]
    public async Task<GoLiveSetUpPayrollMonthOutput> SetUpPayrollMonth(GoLiveSetUpPayrollMonthInput input)
    {
        var name = input.PayrollAreaName.Trim();
        var monthEnd = MonthEnd(input.PayDate.Value);

        var output = new GoLiveSetUpPayrollMonthOutput { PayrollAreaName = name };

        var area = await _payrollAreaRepository.FirstOrDefaultAsync(a => a.Name == name);
        if (area == null)
        {
            area = await CreatePayrollAreaAsync(name);
            output.PayrollAreaCreated = true;
        }

        var payYear = await _payYearRepository.GetAsync(area.PayrollPeriodId);
        var currency = await _currencyRepository.GetAsync(area.CurrencyId);
        output.CurrencyCode = currency.Code;
        output.PayYear = payYear.Code;
        output.CutOffDay = area.CutOffDay;

        // The engine allows one control record per payroll area. One that is already there is left alone: if
        // it is for the wrong month, the engine's next refusal will say so, and that is its own step.
        var record = await _controlRecordRepository.FirstOrDefaultAsync(r => r.PayrollAreaId == area.Id);
        if (record == null)
        {
            var (payYearMonth, payYearNumber) = FindPayYearMonth(payYear.Code, monthEnd);

            // Locked, because a regular run is refused unless the period is locked. The start and end dates are
            // not passed: the control record service works them out from the pay year, as its screen does.
            await _controlRecords.CreateOrEdit(new CreateOrEditPayrollControlRecordDto
            {
                PayrollAreaId = area.Id,
                PayrollMonth = payYearMonth,
                PayrollYear = payYearNumber,
                PeriodLocked = true,
                PeriodClosed = false,
            });

            await CurrentUnitOfWork.SaveChangesAsync();

            record = await _controlRecordRepository.FirstOrDefaultAsync(r => r.PayrollAreaId == area.Id);
            output.ControlRecordCreated = true;
        }

        // Locked for the run, whether the record was just created or released for correction earlier. A regular run
        // is refused unless the period is locked, and while it is locked the payroll lock freezes master data.
        if (record.PeriodState != PayrollPeriodState.ReleasedForPayroll)
        {
            record.ApplyState(PayrollPeriodState.ReleasedForPayroll, AbpSession.UserId, Clock.Now);
            await _controlRecordRepository.UpdateAsync(record);
        }

        output.MonthStart = record.CalendarMonthStart;
        output.MonthEnd = record.CalendarMonthEnd;

        output.CalendarYear = monthEnd.Year;
        output.CalendarMonthsCreated = await CreateCalendarYearAsync(area.Id, monthEnd.Year);

        return output;
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_Employees_Create)]
    public async Task<GoLiveLoadEmployeesOutput> LoadEmployees(GoLiveLoadEmployeesInput input)
    {
        await ReleaseForCorrectionAsync();

        // The reference records every employee needs, checked before anything is created: refusing half-way
        // would leave an org unit and a grade behind with nobody in them.
        var contractType = await _contractTypeRepository.FirstOrDefaultAsync(c => c.NameEn == DefaultContractType)
                           ?? throw new UserFriendlyException(L("GoLiveContractTypeMissing"));
        var currency = await _currencyRepository.FirstOrDefaultAsync(c => c.Code == DefaultCurrencyCode)
                       ?? throw new UserFriendlyException(L("GoLiveCurrencyMissing"));
        var salaryWageTypeIds = await ResolveSalaryWageTypesAsync();

        var areaName = input.PayrollAreaName.Trim();
        var unitName = input.OrganizationUnitName.Trim();

        var output = new GoLiveLoadEmployeesOutput
        {
            PayrollAreaName = areaName,
            OrganizationUnitName = unitName,
            EmployeeGroupName = GoLiveEmployeeDefaults.EmployeeGroupName,
            EmployeeSubGroupName = GoLiveEmployeeDefaults.EmployeeSubGroupName,
        };

        var area = await _payrollAreaRepository.FirstOrDefaultAsync(a => a.Name == areaName);
        if (area == null)
        {
            area = await CreatePayrollAreaAsync(areaName);
            output.PayrollAreaCreated = true;
        }

        var (unitId, unitCreated) = await _employeeDefaults.EnsureOrganizationUnitAsync(unitName, area.Id);
        output.OrganizationUnitCreated = unitCreated;

        var grade = await _employeeDefaults.EnsureGradeAsync();
        var placeholders = await _placeholders.EnsureAsync();
        output.PlaceholdersCreated = placeholders.Created;

        var workingTime = await _workingTime.EnsureCompanyAsync(input.SaturdayIsRestDay);
        output.WorkingTimeCreated = workingTime.Created;
        output.EmployeeGroupCreated = grade.GroupCreated;
        output.EmployeeSubGroupCreated = grade.SubGroupCreated;
        output.GratuitySlabsCreated = grade.GratuitySlabsCreated;
        output.LeaveEntitlementsCreated = grade.LeaveEntitlementsCreated;
        output.SickLeaveRuleCreated = grade.SickLeaveRuleCreated;

        await _employeeLoader.LoadAsync(
            input.Employees ?? new List<GoLiveEmployeeRowDto>(),
            new GoLiveEmployeeTargets
            {
                OrganizationUnitId = unitId,
                PayrollAreaId = area.Id,
                EmployeeGroupId = grade.GroupId,
                EmployeeSubGroupId = grade.SubGroupId,
                ContractTypeId = contractType.Id,
                CurrencyId = currency.Id,
                SalaryWageTypeIds = salaryWageTypeIds,
                Placeholders = placeholders,
                WorkingTime = workingTime,
                BankIdsByName = await BankIdsByNameAsync(),
            },
            output);

        return output;
    }

    /// <summary>
    /// The grid's salary columns' wage types, by catalogue code. All must exist and be active before anything is
    /// loaded: a salary line on a missing or inactive wage type is one the engine would silently never pay.
    /// </summary>
    private async Task<IReadOnlyDictionary<string, int>> ResolveSalaryWageTypesAsync()
    {
        var codes = GoLiveEmployeeLoader.SalaryColumns.Select(c => c.WageCode).ToList();
        var found = await _wageTypeRepository.GetAll()
            .Where(w => codes.Contains(w.Code) && w.Status)
            .Select(w => new { w.Code, w.Id })
            .ToListAsync();

        var missing = codes.Where(c => found.All(w => w.Code != c)).ToList();
        if (missing.Count > 0)
        {
            throw new UserFriendlyException(L("GoLiveWageTypesMissing", string.Join(", ", missing)));
        }

        return found.GroupBy(w => w.Code).ToDictionary(g => g.Key, g => g.First().Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_Employees)]
    public async Task<FileDto> ExportEmployeeSheet(GoLiveEmployeeSheetInput input) =>
        _excelExports.EmployeeSheet(input?.Employees ?? new List<GoLiveEmployeeRowDto>(), (await GetBankNames()).Items);

    /// <remarks>Creates payments, so it needs the Payment screen's own create permission.</remarks>
    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeePayments_Create)]
    public Task<GoLiveLegacyPaymentsOutput> AddLegacyPayments(GoLiveLegacyPaymentsInput input) =>
        _legacyPayments.AddAsync(input, MonthEnd(input.PayDate.Value));

    /// <remarks>The lines hold salaries, so it needs the salary register permission the comparison itself needs.</remarks>
    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_SalaryRegisters)]
    public Task<FileDto> ExportLegacyComparison(GoLiveLegacyCompareExportInput input) =>
        Task.FromResult(_excelExports.LegacyComparison(input?.Lines ?? new List<GoLiveLegacyCompareLineDto>(), input?.PayMonth));

    [HttpPost]
    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_SalaryRegisters)]
    public Task<GoLiveLegacyExportQueryDto> GetLegacySalaryExportQuery(GoLivePayrollResultsInput input) =>
        _legacyComparer.GetExportQueryAsync(input.PayrollAreaName, MonthEnd(input.PayDate.Value));

    /// <remarks>Reads salary records as well as registers, so it needs the salary register permission.</remarks>
    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_SalaryRegisters)]
    public Task<GoLiveLegacyCompareOutput> CompareWithLegacySalary(GoLiveLegacyCompareInput input) =>
        _legacyComparer.CompareAsync(input, MonthEnd(input.PayDate.Value));

    /// <remarks>Shows employee records, so it needs the employee screen's permission too.</remarks>
    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_Employees)]
    public Task<GoLiveLoadedEmployeesOutput> GetLoadedEmployees(string payrollAreaName) =>
        _loadedEmployees.ReadAsync(payrollAreaName);

    /// <remarks>Shows what people were paid, so it needs the salary register screen's permission.</remarks>
    [HttpPost]
    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_SalaryRegisters)]
    public Task<GoLivePayrollResultsOutput> GetPayrollResults(GoLivePayrollResultsInput input) =>
        _payrollResults.GetResultsAsync(input.PayrollAreaName, MonthEnd(input.PayDate.Value));

    [HttpPost]
    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_SalaryRegisters)]
    public Task<GoLiveSalaryRegisterDto> GetSalaryRegister(GoLiveSalaryRegisterInput input) =>
        _payrollResults.GetRegisterAsync(input.EmployeeId, MonthEnd(input.PayDate.Value));

    [HttpPost]
    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_SalaryRegisters)]
    public Task<FileDto> GetSalaryRegisterExcel(GoLivePayrollResultsInput input) =>
        _payrollResults.ExportRegisterLinesAsync(input.PayrollAreaName, MonthEnd(input.PayDate.Value));

    /// <remarks>
    /// The report writes an Excel file; this reads that same file back as rows. Going through the file
    /// rather than the report's own DTO keeps the screen and the download identical -- one column list,
    /// one set of formats -- and the report's permission still applies, because it is its own service
    /// that produces the file.
    /// </remarks>
    [HttpPost]
    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_SalaryRegisters)]
    public async Task<GoLiveGridDto> GetPayrollSheetGrid(GoLivePayrollResultsInput input)
    {
        var monthEnd = MonthEnd(input.PayDate.Value);
        var area = await _payrollAreaRepository.FirstOrDefaultAsync(a => a.Name == input.PayrollAreaName.Trim());

        var file = await _payrollSheetReport.GetPayrollSheetReportToExcel(new GetPayrollSheetReportInput
        {
            Month = monthEnd.Month,
            Year = monthEnd.Year,
            PayrollRunType = PayrollRunType.Regular,
            PayrollAreaId = area?.Id,
            SkipCount = 0,
            MaxResultCount = MaxReportRows,
        });

        return _gridReader.Read(_tempFiles.GetFile(file.FileToken), L("GoLivePayrollSheet"));
    }

    [HttpPost]
    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_SalaryRegisters)]
    public async Task<GoLiveGridDto> GetSalaryRegisterGrid(GoLivePayrollResultsInput input)
    {
        var file = await GetSalaryRegisterExcel(input);
        return _gridReader.Read(_tempFiles.GetFile(file.FileToken), L("GoLiveSalaryRegister"));
    }

    /// <remarks>Shows employee records, so it needs the employee screen's permission too.</remarks>
    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_Employees)]
    public Task<GoLiveClearCompanyDto> GetClearPreview(string payrollAreaName) =>
        _companyClearer.PreviewAsync(payrollAreaName);

    /// <remarks>
    /// Removes employees and a payroll month, so it needs both of those screens' delete permissions -- Go-Live Setup must
    /// not become a way round either.
    /// </remarks>
    [AbpAuthorize(
        AppPermissions.Pages_PersonnelAdministration_Employees_Delete,
        AppPermissions.Pages_PayrollManagement_PayrollControlRecords_Delete,
        RequireAllPermissions = true)]
    public async Task<GoLiveClearCompanyDto> ClearCompany(GoLiveClearCompanyInput input)
    {
        // A background run still writing registers would put back part of what Clear removes.
        if (GoLiveRunTracker.IsGoing((await _runTracker.ProgressAsync()).Status))
        {
            throw new UserFriendlyException(L("GoLiveClearWhileRunning"));
        }

        await ReleaseForCorrectionAsync();

        var cleared = await _companyClearer.ClearAsync(input);

        // The last run described employees that are gone now: nothing of it is shown again.
        await _runTracker.ForgetAsync();
        return cleared;
    }

    public async Task<ListResultDto<string>> GetBankNames()
    {
        var names = await _bankRepository.GetAll()
            .Where(b => b.NameEn != null && b.NameEn != "")
            .Select(b => b.NameEn)
            .Distinct()
            .OrderBy(n => n)
            .ToListAsync();

        return new ListResultDto<string>(names);
    }

    /// <remarks>Shows who is paid into which bank, so it needs the Bank File screen's permission.</remarks>
    [HttpPost]
    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_BankFiles)]
    public Task<GoLiveBankFileSetupOutput> GetBankFileSetup(GoLiveBankFileSetupInput input) =>
        _bankFileStep.GetSetupAsync(input.PayrollAreaName.Trim(), MonthEnd(input.PayDate.Value));

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_BankFiles_Generate)]
    public Task<GoLiveGenerateBankFileOutput> GenerateBankFile(GoLiveGenerateBankFileInput input) =>
        _bankFileStep.GenerateAsync(input, MonthEnd(input.PayDate.Value));

    /// <summary>By name ignoring case. Two banks with one name -- the seeder never makes them -- resolve to the first.</summary>
    private async Task<IReadOnlyDictionary<string, int>> BankIdsByNameAsync() =>
        (await _bankRepository.GetAll()
            .Where(b => b.NameEn != null && b.NameEn != "")
            .OrderBy(b => b.Id)
            .Select(b => new { b.Id, b.NameEn })
            .ToListAsync())
        .GroupBy(b => b.NameEn.Trim(), StringComparer.OrdinalIgnoreCase)
        .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

    public async Task<ListResultDto<string>> GetPayrollAreaNames()
    {
        var names = await _payrollAreaRepository.GetAll()
            .OrderBy(a => a.Name)
            .Select(a => a.Name)
            .ToListAsync();

        return new ListResultDto<string>(names);
    }

    /// <summary>
    /// The Go-Live Setup step for a refusal, decided by what is actually missing -- the same records, looked up the same
    /// way, as the engine checks them. Each later link adds its own check here.
    /// </summary>
    private async Task<GoLiveNextStep> NextStepAsync(string areaName, DateTime payInDate)
    {
        var area = await _payrollAreaRepository.FirstOrDefaultAsync(a => a.Name == areaName);
        if (area == null || await _controlRecordRepository.FirstOrDefaultAsync(r => r.PayrollAreaId == area.Id) == null)
        {
            return GoLiveNextStep.SetUpPayrollMonth;
        }

        // The engine looks for the pay month in the calendar across the whole tenant, not per payroll area.
        if (!await _calendarRepository.GetAll().AnyAsync(c => c.PayrollMonth == payInDate.Month && c.PayrollYear == payInDate.Year))
        {
            return GoLiveNextStep.SetUpPayrollMonth;
        }

        return GoLiveNextStep.None;
    }

    /// <summary>
    /// The pay calendar for a whole year, January to December, one row per calendar month from the 1st to the
    /// last day -- the assumption Go-Live Setup makes rather than asking. A whole year, not just the month being paid, so
    /// the next eleven runs do not stop on the same refusal.
    /// A month the area already has is left alone, and so is any month whose dates would overlap a row keyed in
    /// by hand: the calendar service refuses overlaps, and a hand-made calendar is the customer's to keep.
    /// </summary>
    private async Task<int> CreateCalendarYearAsync(int payrollAreaId, int year)
    {
        var existing = await _calendarRepository.GetAll()
            .Where(c => c.PayrollAreaId == payrollAreaId)
            .Select(c => new { c.PayrollMonth, c.PayrollYear, c.StartDate, c.EndDate })
            .ToListAsync();

        var created = 0;

        for (var month = 1; month <= 12; month++)
        {
            var start = new DateTime(year, month, 1);
            var end = new DateTime(year, month, DateTime.DaysInMonth(year, month));

            if (existing.Any(c => (c.PayrollMonth == month && c.PayrollYear == year)
                                  || (c.StartDate.Date <= end && c.EndDate.Date >= start)))
            {
                continue;
            }

            await _calendars.CreateOrEdit(new CreateOrEditPayrollCalendarDto
            {
                PayrollAreaId = payrollAreaId,
                PayrollMonth = month,
                PayrollYear = year,
                StartDate = start,
                EndDate = end,
            });

            // Saved one at a time: the service checks for overlaps against what is already in the database.
            await CurrentUnitOfWork.SaveChangesAsync();
            created++;
        }

        return created;
    }

    /// <summary>
    /// A new payroll area from just a name, with Kuwait defaults -- and only if the tenant really has them.
    /// Falling back to "the first currency" would pay a whole company in whatever happened to be seeded first.
    /// The code is left blank on purpose: the payroll area service assigns the next one, as its screen does.
    /// </summary>
    private async Task<PayrollArea> CreatePayrollAreaAsync(string name)
    {
        var currency = await _currencyRepository.FirstOrDefaultAsync(c => c.Code == DefaultCurrencyCode)
                       ?? throw new UserFriendlyException(L("GoLiveCurrencyMissing"));

        var payYear = await _payYearRepository.FirstOrDefaultAsync(p => p.Code == DefaultPayYearCode)
                      ?? throw new UserFriendlyException(L("GoLivePayYearMissing"));

        await _payrollAreas.CreateOrEdit(new CreateOrEditPayrollAreaDto
        {
            Name = name,
            CurrencyId = currency.Id,
            PayrollPeriodId = payYear.Id,
            CutOffDay = MonthEndCutOff,
        });

        await CurrentUnitOfWork.SaveChangesAsync();

        return await _payrollAreaRepository.FirstOrDefaultAsync(a => a.Name == name);
    }

    /// <summary>
    /// A control record's month is the month of the pay YEAR, not of the calendar: in an April to March year,
    /// September is month 6. Rather than re-derive that arithmetic, ask the same calculator the control record
    /// service uses which (month, year) ends on this date.
    /// </summary>
    private (int Month, int Year) FindPayYearMonth(string payYearCode, DateTime monthEnd)
    {
        for (var year = monthEnd.Year - 1; year <= monthEnd.Year + 1; year++)
        {
            for (var month = 1; month <= 12; month++)
            {
                try
                {
                    if (PayrollPeriodShiftCalculator.GetCalendarMonthRange(payYearCode, month, year).CalendarMonthEnd.Date == monthEnd)
                    {
                        return (month, year);
                    }
                }
                catch (ArgumentException)
                {
                    throw new UserFriendlyException(L("IncorrectPayrollPeriod"));
                }
            }
        }

        throw new UserFriendlyException(L("IncorrectPayrollPeriod"));
    }

    /// <summary>
    /// The engine only accepts the month's last day: its control-record check compares the pay date to the
    /// record's month end exactly, so the 12th is refused as "not locked or is not current". A customer thinks
    /// in months, so any day stands for its month. Found by this service's own tests.
    /// </summary>
    private static DateTime MonthEnd(DateTime payDate) =>
        new(payDate.Year, payDate.Month, DateTime.DaysInMonth(payDate.Year, payDate.Month));

    private static GoLiveRunPayrollOutput Stopped(GoLiveStoppedAt stoppedAt, string message) =>
        new() { StoppedAt = stoppedAt, Message = message };

    private GoLiveRunPayrollOutput StoppedByUser(DateTime payInDate, List<GoLiveEmployeeResultDto> soFar)
    {
        var stopped = Stopped(GoLiveStoppedAt.ByUser, L("GoLiveRunStoppedByUser"));
        stopped.PayInDate = payInDate;
        stopped.Employees = soFar ?? new List<GoLiveEmployeeResultDto>();
        return stopped;
    }
}
