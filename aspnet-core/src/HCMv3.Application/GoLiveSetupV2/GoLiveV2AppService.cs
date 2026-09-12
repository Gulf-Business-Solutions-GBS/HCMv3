using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
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
using HCMv3.GoLiveSetupV2.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using HCMv3.PayrollManagement.Services;

namespace HCMv3.GoLiveSetupV2;

/// <inheritdoc cref="IGoLiveV2AppService"/>
/// <remarks>
/// Each action also needs the permission of the thing it does: running payroll writes real salary registers
/// and setting up a month creates payroll master data, so V2 must not become a way round either.
/// </remarks>
[AbpAuthorize(AppPermissions.Pages_GoLiveSetup)]
public class GoLiveV2AppService : HCMv3AppServiceBase, IGoLiveV2AppService
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
    private readonly GoLiveV2EmployeeDefaults _employeeDefaults;
    private readonly GoLiveV2PlaceholderLookups _placeholders;
    private readonly GoLiveV2EmployeeLoader _employeeLoader;
    private readonly GoLiveV2TimeEngineStep _timeEngineStep;
    private readonly GoLiveV2WorkingTime _workingTime;
    private readonly GoLiveV2LoadedEmployeesReader _loadedEmployees;
    private readonly GoLiveV2PayrollResultsReader _payrollResults;
    private readonly IReferenceDataSeeder _referenceData;
    private readonly IRepository<WageTypeLeave> _wageTypeLeaveRepository;
    private readonly IRepository<Bank> _bankRepository;
    private readonly GoLiveV2BankFileStep _bankFileStep;
    private readonly GoLiveV2GridReader _gridReader;
    private readonly IPayrollSheetReportAppService _payrollSheetReport;
    private readonly ITempFileCacheManager _tempFiles;

    public GoLiveV2AppService(
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
        GoLiveV2EmployeeDefaults employeeDefaults,
        GoLiveV2PlaceholderLookups placeholders,
        GoLiveV2EmployeeLoader employeeLoader,
        GoLiveV2TimeEngineStep timeEngineStep,
        GoLiveV2WorkingTime workingTime,
        GoLiveV2LoadedEmployeesReader loadedEmployees,
        GoLiveV2PayrollResultsReader payrollResults,
        IReferenceDataSeeder referenceData,
        IRepository<WageTypeLeave> wageTypeLeaveRepository,
        IRepository<Bank> bankRepository,
        GoLiveV2BankFileStep bankFileStep,
        GoLiveV2GridReader gridReader,
        IPayrollSheetReportAppService payrollSheetReport,
        ITempFileCacheManager tempFiles)
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
        _payrollSheetReport = payrollSheetReport;
        _tempFiles = tempFiles;
    }

    /// <remarks>Runs the time engine too, so it needs that engine's permission as well as the payroll engine's.</remarks>
    [AbpAuthorize(
        AppPermissions.Pages_PayrollManagement_PayrollEngine_Run,
        AppPermissions.Pages_TimeManagement_TimeEngine_Run,
        RequireAllPermissions = true)]
    public async Task<GoLiveV2RunPayrollOutput> RunPayroll(GoLiveV2RunPayrollInput input)
    {
        input ??= new GoLiveV2RunPayrollInput();

        // The same two checks, in the same words, as IPayrollEngineAppService.RunPayrollEngine makes before it
        // queues a run -- so V2 meets exactly the refusals production does, in the same order.
        if (input.PayDate == null)
        {
            return Stopped(GoLiveV2StoppedAt.Input, L("PayrollEnginePayInDateRequired"));
        }

        if (string.IsNullOrWhiteSpace(input.PayrollAreaName))
        {
            return Stopped(GoLiveV2StoppedAt.Input, L("PayrollEnginePayrollAreaRequired"));
        }

        var areaName = input.PayrollAreaName.Trim();
        var payInDate = MonthEnd(input.PayDate.Value);

        // The month first. A missing control record or pay calendar was the engine's third and fourth refusal,
        // and answering them needs nothing the customer has not already given -- so it is done here, once, and
        // reported, instead of being a separate button to press between two runs.
        GoLiveV2SetUpPayrollMonthOutput monthSetUp = null;
        if (await NextStepAsync(areaName, payInDate) == GoLiveV2NextStep.SetUpPayrollMonth)
        {
            try
            {
                monthSetUp = await SetUpPayrollMonth(new GoLiveV2SetUpPayrollMonthInput { PayDate = payInDate, PayrollAreaName = areaName });
            }
            catch (UserFriendlyException ex)
            {
                return Stopped(GoLiveV2StoppedAt.MonthSetUp, ex.Message);
            }
        }

        var leavePayLinksAdded = await EnsureLeavePayLinksAsync();

        var output = await RunEnginesAsync(areaName, payInDate);
        output.MonthSetUp = monthSetUp;
        output.LeavePayLinksAdded = leavePayLinksAdded;
        return output;
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
    private async Task<GoLiveV2RunPayrollOutput> RunEnginesAsync(string areaName, DateTime payInDate)
    {
        // The time engine first: the payroll pays days from the time registers it writes for the month. With
        // nobody in the area it has nothing to do, and the payroll engine says so in its own words.
        var timeEngineStopped = await _timeEngineStep.RunAsync(areaName, payInDate, (await GetCurrentUserAsync()).UserName);
        if (timeEngineStopped != null)
        {
            timeEngineStopped.PayInDate = payInDate;
            return timeEngineStopped;
        }

        PayrollEngineRunResult run;
        try
        {
            run = await _payrollEngine.RunAsync(new PayrollEngineRunInput
            {
                PayrollRunType = PayrollRunType.Regular,
                PayInDate = payInDate,
                PayrollAreaName = areaName,
                RegisterTarget = PayrollRegisterTarget.Live,
            });
        }
        catch (UserFriendlyException ex)
        {
            // Every run-level refusal the engine makes is a UserFriendlyException with its own wording. Anything
            // else is a genuine fault and is allowed to surface as one.
            var stopped = Stopped(GoLiveV2StoppedAt.Engine, ex.Message);
            stopped.NextStep = await NextStepAsync(areaName, payInDate);
            return stopped;
        }

        var output = new GoLiveV2RunPayrollOutput
        {
            PayInDate = run.PayInDate,
            Employees = run.EmployeeResults
                .Select(e => new GoLiveV2EmployeeResultDto
                {
                    EmployeeCode = e.EmployeeCode,
                    Succeeded = e.Succeeded,
                    Message = e.Message,
                })
                .ToList(),
        };

        var firstFailure = output.Employees.FirstOrDefault(e => !e.Succeeded);

        if (firstFailure != null)
        {
            // The first refusal leads, because it is the next thing to fix; the rest are in the list.
            output.StoppedAt = GoLiveV2StoppedAt.Employees;
            output.Message = firstFailure.Message;
        }
        else if (output.Employees.Count == 0)
        {
            // The engine treats an empty payroll area as a finished run. For go-live it is the opposite.
            output.StoppedAt = GoLiveV2StoppedAt.NoEmployees;
            output.NextStep = GoLiveV2NextStep.LoadEmployees;
        }
        else
        {
            output.Succeeded = !run.Aborted;
            output.StoppedAt = run.Aborted ? GoLiveV2StoppedAt.Engine : GoLiveV2StoppedAt.None;
        }

        return output;
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_PayrollControlRecords_Create)]
    public async Task<GoLiveV2SetUpPayrollMonthOutput> SetUpPayrollMonth(GoLiveV2SetUpPayrollMonthInput input)
    {
        var name = input.PayrollAreaName.Trim();
        var monthEnd = MonthEnd(input.PayDate.Value);

        var output = new GoLiveV2SetUpPayrollMonthOutput { PayrollAreaName = name };

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

        output.MonthStart = record.CalendarMonthStart;
        output.MonthEnd = record.CalendarMonthEnd;

        output.CalendarYear = monthEnd.Year;
        output.CalendarMonthsCreated = await CreateCalendarYearAsync(area.Id, monthEnd.Year);

        return output;
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_Employees_Create)]
    public async Task<GoLiveV2LoadEmployeesOutput> LoadEmployees(GoLiveV2LoadEmployeesInput input)
    {
        // The reference records every employee needs, checked before anything is created: refusing half-way
        // would leave an org unit and a grade behind with nobody in them.
        var contractType = await _contractTypeRepository.FirstOrDefaultAsync(c => c.NameEn == DefaultContractType)
                           ?? throw new UserFriendlyException(L("GoLiveV2ContractTypeMissing"));
        var currency = await _currencyRepository.FirstOrDefaultAsync(c => c.Code == DefaultCurrencyCode)
                       ?? throw new UserFriendlyException(L("GoLiveV2CurrencyMissing"));
        var salaryWageTypeIds = await ResolveSalaryWageTypesAsync();

        var areaName = input.PayrollAreaName.Trim();
        var unitName = input.OrganizationUnitName.Trim();

        var output = new GoLiveV2LoadEmployeesOutput
        {
            PayrollAreaName = areaName,
            OrganizationUnitName = unitName,
            EmployeeGroupName = GoLiveV2EmployeeDefaults.EmployeeGroupName,
            EmployeeSubGroupName = GoLiveV2EmployeeDefaults.EmployeeSubGroupName,
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

        var workingTime = await _workingTime.EnsureCompanyAsync();
        output.WorkingTimeCreated = workingTime.Created;
        output.EmployeeGroupCreated = grade.GroupCreated;
        output.EmployeeSubGroupCreated = grade.SubGroupCreated;
        output.GratuitySlabsCreated = grade.GratuitySlabsCreated;
        output.LeaveEntitlementsCreated = grade.LeaveEntitlementsCreated;
        output.SickLeaveRuleCreated = grade.SickLeaveRuleCreated;

        await _employeeLoader.LoadAsync(
            input.Employees ?? new List<GoLiveV2EmployeeRowDto>(),
            new GoLiveV2EmployeeTargets
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
        var codes = GoLiveV2EmployeeLoader.SalaryColumns.Select(c => c.WageCode).ToList();
        var found = await _wageTypeRepository.GetAll()
            .Where(w => codes.Contains(w.Code) && w.Status)
            .Select(w => new { w.Code, w.Id })
            .ToListAsync();

        var missing = codes.Where(c => found.All(w => w.Code != c)).ToList();
        if (missing.Count > 0)
        {
            throw new UserFriendlyException(L("GoLiveV2WageTypesMissing", string.Join(", ", missing)));
        }

        return found.GroupBy(w => w.Code).ToDictionary(g => g.Key, g => g.First().Id);
    }

    /// <remarks>Shows employee records, so it needs the employee screen's permission too.</remarks>
    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_Employees)]
    public Task<GoLiveV2LoadedEmployeesOutput> GetLoadedEmployees(string payrollAreaName) =>
        _loadedEmployees.ReadAsync(payrollAreaName);

    /// <remarks>Shows what people were paid, so it needs the salary register screen's permission.</remarks>
    [HttpPost]
    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_SalaryRegisters)]
    public Task<GoLiveV2PayrollResultsOutput> GetPayrollResults(GoLiveV2PayrollResultsInput input) =>
        _payrollResults.GetResultsAsync(input.PayrollAreaName, MonthEnd(input.PayDate.Value));

    [HttpPost]
    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_SalaryRegisters)]
    public Task<GoLiveV2SalaryRegisterDto> GetSalaryRegister(GoLiveV2SalaryRegisterInput input) =>
        _payrollResults.GetRegisterAsync(input.EmployeeId, MonthEnd(input.PayDate.Value));

    [HttpPost]
    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_SalaryRegisters)]
    public Task<FileDto> GetSalaryRegisterExcel(GoLiveV2PayrollResultsInput input) =>
        _payrollResults.ExportRegisterLinesAsync(input.PayrollAreaName, MonthEnd(input.PayDate.Value));

    /// <remarks>
    /// The report writes an Excel file; this reads that same file back as rows. Going through the file
    /// rather than the report's own DTO keeps the screen and the download identical -- one column list,
    /// one set of formats -- and the report's permission still applies, because it is its own service
    /// that produces the file.
    /// </remarks>
    [HttpPost]
    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_SalaryRegisters)]
    public async Task<GoLiveV2GridDto> GetPayrollSheetGrid(GoLiveV2PayrollResultsInput input)
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

        return _gridReader.Read(_tempFiles.GetFile(file.FileToken), L("GoLiveV2PayrollSheet"));
    }

    [HttpPost]
    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_SalaryRegisters)]
    public async Task<GoLiveV2GridDto> GetSalaryRegisterGrid(GoLiveV2PayrollResultsInput input)
    {
        var file = await GetSalaryRegisterExcel(input);
        return _gridReader.Read(_tempFiles.GetFile(file.FileToken), L("GoLiveV2SalaryRegister"));
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
    public Task<GoLiveV2BankFileSetupOutput> GetBankFileSetup(GoLiveV2BankFileSetupInput input) =>
        _bankFileStep.GetSetupAsync(input.PayrollAreaName.Trim(), MonthEnd(input.PayDate.Value));

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_BankFiles_Generate)]
    public Task<GoLiveV2GenerateBankFileOutput> GenerateBankFile(GoLiveV2GenerateBankFileInput input) =>
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
    /// The V2 step for a refusal, decided by what is actually missing -- the same records, looked up the same
    /// way, as the engine checks them. Each later link adds its own check here.
    /// </summary>
    private async Task<GoLiveV2NextStep> NextStepAsync(string areaName, DateTime payInDate)
    {
        var area = await _payrollAreaRepository.FirstOrDefaultAsync(a => a.Name == areaName);
        if (area == null || await _controlRecordRepository.FirstOrDefaultAsync(r => r.PayrollAreaId == area.Id) == null)
        {
            return GoLiveV2NextStep.SetUpPayrollMonth;
        }

        // The engine looks for the pay month in the calendar across the whole tenant, not per payroll area.
        if (!await _calendarRepository.GetAll().AnyAsync(c => c.PayrollMonth == payInDate.Month && c.PayrollYear == payInDate.Year))
        {
            return GoLiveV2NextStep.SetUpPayrollMonth;
        }

        return GoLiveV2NextStep.None;
    }

    /// <summary>
    /// The pay calendar for a whole year, January to December, one row per calendar month from the 1st to the
    /// last day -- the assumption V2 makes rather than asking. A whole year, not just the month being paid, so
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
                       ?? throw new UserFriendlyException(L("GoLiveV2CurrencyMissing"));

        var payYear = await _payYearRepository.FirstOrDefaultAsync(p => p.Code == DefaultPayYearCode)
                      ?? throw new UserFriendlyException(L("GoLiveV2PayYearMissing"));

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

    private static GoLiveV2RunPayrollOutput Stopped(GoLiveV2StoppedAt stoppedAt, string message) =>
        new() { StoppedAt = stoppedAt, Message = message };
}
