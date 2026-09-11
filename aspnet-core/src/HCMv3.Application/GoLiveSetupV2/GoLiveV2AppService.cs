using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;
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

    /// <summary>Month end: everything entered during the month is included, which is the least surprising.</summary>
    private const int MonthEndCutOff = 31;

    private readonly IPayrollEngineService _payrollEngine;
    private readonly IPayrollAreasAppService _payrollAreas;
    private readonly IPayrollControlRecordsAppService _controlRecords;
    private readonly IRepository<PayrollArea> _payrollAreaRepository;
    private readonly IRepository<PayrollControlRecord> _controlRecordRepository;
    private readonly IRepository<Currency> _currencyRepository;
    private readonly IRepository<PayrollPeriod> _payYearRepository;

    public GoLiveV2AppService(
        IPayrollEngineService payrollEngine,
        IPayrollAreasAppService payrollAreas,
        IPayrollControlRecordsAppService controlRecords,
        IRepository<PayrollArea> payrollAreaRepository,
        IRepository<PayrollControlRecord> controlRecordRepository,
        IRepository<Currency> currencyRepository,
        IRepository<PayrollPeriod> payYearRepository)
    {
        _payrollEngine = payrollEngine;
        _payrollAreas = payrollAreas;
        _controlRecords = controlRecords;
        _payrollAreaRepository = payrollAreaRepository;
        _controlRecordRepository = controlRecordRepository;
        _currencyRepository = currencyRepository;
        _payYearRepository = payYearRepository;
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_PayrollEngine_Run)]
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

        PayrollEngineRunResult run;
        try
        {
            run = await _payrollEngine.RunAsync(new PayrollEngineRunInput
            {
                PayrollRunType = PayrollRunType.Regular,
                PayInDate = MonthEnd(input.PayDate.Value),
                PayrollAreaName = areaName,
                RegisterTarget = PayrollRegisterTarget.Live,
            });
        }
        catch (UserFriendlyException ex)
        {
            // Every run-level refusal the engine makes is a UserFriendlyException with its own wording. Anything
            // else is a genuine fault and is allowed to surface as one.
            var stopped = Stopped(GoLiveV2StoppedAt.Engine, ex.Message);
            stopped.NextStep = await NextStepAsync(areaName);
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

        return output;
    }

    /// <summary>
    /// The V2 step for a refusal, decided by what is actually missing. Only the control record is answered so
    /// far; each later link adds its own check here.
    /// </summary>
    private async Task<GoLiveV2NextStep> NextStepAsync(string areaName)
    {
        var area = await _payrollAreaRepository.FirstOrDefaultAsync(a => a.Name == areaName);
        if (area == null || await _controlRecordRepository.FirstOrDefaultAsync(r => r.PayrollAreaId == area.Id) == null)
        {
            return GoLiveV2NextStep.SetUpPayrollMonth;
        }

        return GoLiveV2NextStep.None;
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
