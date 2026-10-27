using System;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.BackgroundJobs;
using Abp.Domain.Repositories;
using Abp.Runtime.Session;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.EngineRuns;
using HCMv3.EngineRuns.Dto;
using HCMv3.PayrollManagement.Dto;
using HCMv3.PayrollManagement.Services;
using HCMv3.SystemSettings.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PayrollManagement;

/// <inheritdoc cref="IPayrollEngineAppService"/>
/// <remarks>
/// Ported from legacy's Payroll Engine screen
/// <c>HCM10.Web.UserControls.PayrollManagement.uscPayrollEngine</c>, which collected run type / employee code
/// / pay-in date / payroll area / off-cycle payment date / off-cycle reason and shelled out to the
/// <c>HCM10.PayrollEngine</c> console app. The screen name is preserved verbatim on the <c>ExecutionLog</c>
/// row the engine writes (see <c>PayrollEngineService.AddExecutionLogAsync</c>) so existing execution-log
/// reports keep working.
/// </remarks>
public class PayrollEngineAppService : HCMv3AppServiceBase, IPayrollEngineAppService
{
    private readonly IBackgroundJobManager _backgroundJobManager;
    private readonly EngineRunProgressReader _progressReader;
    private readonly HCMv3.EngineRuns.Services.IEngineRunProgressWriter _progressWriter;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;
    private readonly IRepository<PayrollArea> _payrollAreaRepository;
    private readonly IRepository<PayrollControlRecord> _payrollControlRecordRepository;

    public PayrollEngineAppService(
        IBackgroundJobManager backgroundJobManager,
        EngineRunProgressReader progressReader,
        HCMv3.EngineRuns.Services.IEngineRunProgressWriter progressWriter,
        ISysSettingsAccessor sysSettingsAccessor,
        IRepository<PayrollArea> payrollAreaRepository,
        IRepository<PayrollControlRecord> payrollControlRecordRepository)
    {
        _backgroundJobManager = backgroundJobManager;
        _progressReader = progressReader;
        _progressWriter = progressWriter;
        _sysSettingsAccessor = sysSettingsAccessor;
        _payrollAreaRepository = payrollAreaRepository;
        _payrollControlRecordRepository = payrollControlRecordRepository;
    }

    /// <summary>
    /// Queues the run. Deliberately <b>not</b> synchronous: a run covers every employee x every retro period
    /// x every wage type, so executing it inline would hold an HTTP request open for the duration.
    ///
    /// <para>Only the validations legacy performs BEFORE it touches the database are reproduced here, so the
    /// caller gets a 400 instead of a job that fails on the queue. Every other guard (control record found /
    /// locked / closed, payroll calendar present, per-employee checks) stays in the domain service where
    /// legacy has it.</para>
    ///
    /// <para><c>[HttpPost]</c> is required by this repo's NSwag rule for methods taking a custom input DTO --
    /// without it the input would be flattened into query-string parameters and
    /// <c>RunPayrollEngineInput</c> would never be generated into the client proxies.</para>
    /// </summary>
    [HttpPost]
    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_PayrollEngine_Run)]
    public async Task<RunPayrollEngineOutput> RunPayrollEngine(RunPayrollEngineInput input)
    {
        var isOtherPeriodSimulation = input.IsSimulation && input.SimulationOtherPeriod;

        // Legacy Program.cs lines 218-224. (A simulation of another period gets its date worked out below.)
        if (!isOtherPeriodSimulation && input.PayInDate == default(DateTime))
        {
            throw new UserFriendlyException(L("PayrollEnginePayInDateRequired"));
        }

        // Legacy line 239 requires a payroll area to find the control record at all.
        if (string.IsNullOrWhiteSpace(input.PayrollAreaName))
        {
            throw new UserFriendlyException(L("PayrollEnginePayrollAreaRequired"));
        }

        var payInDate = input.PayInDate;

        if (input.IsSimulation)
        {
            // Legacy shows the Simulation box only when the setting is on (uscPayrollEngine.ascx.cs line 88).
            var payrollSettings = await _sysSettingsAccessor.GetPayrollSettingsAsync();
            if (payrollSettings == null || !payrollSettings.AllowSimulationRun)
            {
                throw new UserFriendlyException(L("PayrollEngineSimulationNotAllowed"));
            }

            if (input.SimulationOtherPeriod)
            {
                payInDate = await ResolveSimulationOtherPeriodPayInDateAsync(input);
            }
        }

        // The other period's date, when legacy would have had none.
        if (payInDate == default(DateTime))
        {
            throw new UserFriendlyException(L("PayrollEnginePayInDateRequired"));
        }

        // Legacy lines 234-237.
        if (input.PayrollRunType == PayrollRunType.OffCycle && string.IsNullOrWhiteSpace(input.OffCycleReason))
        {
            throw new UserFriendlyException(L("PayrollEngineOffCycleReasonRequired"));
        }

        // ONE PAYROLL RUN PER PAYROLL AREA. Checked here so the operator is told straight away instead of
        // finding out from a background job that failed minutes later; the engine re-checks it to catch the race
        // between two jobs queued at once. Different payroll areas are independent and may run together.
        //
        // Not a port: legacy had no such guard -- its Run button truncated the whole PayrollLog table and queued
        // another thread, so a second run wrecked the first one's progress log and their counters merged.
        var activeRun = await _progressWriter.GetActiveRunAsync(
            EngineType.PayrollEngine,
            PayrollEngineService.BuildRunKey(input.PayrollAreaName));

        if (activeRun != null)
        {
            throw new UserFriendlyException(L(
                "PayrollEngineAlreadyRunningForPayrollArea",
                input.PayrollAreaName,
                string.IsNullOrWhiteSpace(activeRun.StartedByUserName) ? L("AnotherUser") : activeRun.StartedByUserName,
                activeRun.StartedAt.ToString("dd/MM/yyyy HH:mm")));
        }

        await _backgroundJobManager.EnqueueAsync<PayrollEngineBackgroundJob, PayrollEngineJobArgs>(
            new PayrollEngineJobArgs
            {
                TenantId = AbpSession.GetTenantId(),
                UserId = AbpSession.UserId,
                PayrollRunType = input.PayrollRunType,
                EmployeeCodes = input.EmployeeCodes,
                PayInDate = payInDate.Date,
                PayrollAreaName = input.PayrollAreaName,
                OffCyclePaymentDate = input.OffCyclePaymentDate?.Date ?? default(DateTime),
                OffCycleReason = input.OffCycleReason,
                // Legacy args[3]: the screen called SimulationPayrollHelper.Main instead of Program.Main.
                RegisterTarget = input.IsSimulation ? PayrollRegisterTarget.Simulation : PayrollRegisterTarget.Live
            });

        return new RunPayrollEngineOutput { Queued = true };
    }

    /// <summary>
    /// The simulation's "Other" period (legacy <c>uscPayrollEngine.SetDates</c>, lines 323-331): the pay date is
    /// the calendar month end that payroll month/year maps to under the payroll area's payroll year
    /// (<c>PayrollControlRecord.GetStartEndDateFromPayrollPeriod</c>, ported as
    /// <see cref="PayrollPeriodShiftCalculator"/>). Legacy works it out only for an area that has a control
    /// record, and gives no date otherwise -- which the engine then refused as "no pay date"; the same refusal is
    /// given here.
    /// </summary>
    private async Task<DateTime> ResolveSimulationOtherPeriodPayInDateAsync(RunPayrollEngineInput input)
    {
        if (input.SimulationMonth == null || input.SimulationYear == null)
        {
            throw new UserFriendlyException(L("PayrollEngineSimulationPeriodRequired"));
        }

        var area = await _payrollAreaRepository.GetAll()
            .Include(a => a.PayrollPeriod)
            .FirstOrDefaultAsync(a => a.Name == input.PayrollAreaName);

        if (area == null || !await _payrollControlRecordRepository.GetAll().AnyAsync(r => r.PayrollAreaId == area.Id))
        {
            return default(DateTime);
        }

        if (area.PayrollPeriod == null)
        {
            throw new UserFriendlyException(L("IncorrectPayrollPeriod"));
        }

        try
        {
            return PayrollPeriodShiftCalculator
                .GetCalendarMonthRange(area.PayrollPeriod.Code, input.SimulationMonth.Value, input.SimulationYear.Value)
                .CalendarMonthEnd;
        }
        catch (ArgumentException)
        {
            throw new UserFriendlyException(L("IncorrectPayrollPeriod"));
        }
    }

    /// <summary>
    /// Live status poll. Parameterless on purpose so it stays an HTTP GET, matching
    /// <c>GetProjectCostEngineStatus</c>; the repo's <c>[HttpPost]</c> rule applies to <c>Get*</c> methods
    /// with a custom input DTO, which this is not.
    ///
    /// <para>Gated on the VIEW permission, not Run.</para>
    /// </summary>
    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_PayrollEngine)]
    public async Task<EngineRunStatusDto> GetPayrollEngineStatus()
    {
        return await _progressReader.GetStatusAsync(EngineType.PayrollEngine);
    }

    /// <summary>
    /// Per-employee grid. <c>[HttpPost]</c> is mandatory: <see cref="GetEngineRunEmployeesInput"/> is a
    /// custom input DTO and would otherwise be flattened into query-string parameters by NSwag and never
    /// generated into the client proxies.
    /// </summary>
    [HttpPost]
    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_PayrollEngine)]
    public async Task<PagedResultDto<EngineRunEmployeeDto>> GetPayrollEngineRunEmployees(GetEngineRunEmployeesInput input)
    {
        return await _progressReader.GetEmployeesAsync(EngineType.PayrollEngine, input);
    }

    /// <summary>
    /// Legacy's Stop button. Sets a flag the engine re-reads between employees on its own connection, so the
    /// current employee completes and the run then stops cleanly.
    /// </summary>
    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_PayrollEngine_Run)]
    public async Task StopPayrollEngine()
    {
        await _progressReader.RequestStopAsync(EngineType.PayrollEngine);
    }

    /// <summary>Legacy's Clear Status button.</summary>
    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_PayrollEngine_Run)]
    public async Task ClearPayrollEngineProgress()
    {
        await _progressReader.ClearAsync(EngineType.PayrollEngine);
    }

    /// <inheritdoc/>
    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_PayrollEngine_Run)]
    public async Task<int> AbandonStalePayrollEngineRuns()
    {
        return await _progressReader.AbandonStaleRunsAsync(EngineType.PayrollEngine);
    }
}
