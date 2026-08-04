using System;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.BackgroundJobs;
using Abp.Runtime.Session;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.EngineRuns;
using HCMv3.EngineRuns.Dto;
using HCMv3.TimeManagement.Dto;
using Microsoft.AspNetCore.Mvc;

namespace HCMv3.TimeManagement;

/// <inheritdoc cref="ITimeEngineAppService"/>
/// <remarks>
/// Ported from legacy's Time Engine screen <c>HCM10.Web.UserControls.PayrollManagement.uscTimeEngine</c>,
/// which collected employee code / start date / end date / payroll area and shelled out to the
/// <c>HCM10.TimeEngine</c> console app. The screen name is preserved verbatim on the <c>ExecutionLog</c> row
/// the engine writes (see <c>TimeEngineService.AddExecutionLogAsync</c>) so existing execution-log reports
/// keep working.
/// </remarks>
public class TimeEngineAppService : HCMv3AppServiceBase, ITimeEngineAppService
{
    private readonly IBackgroundJobManager _backgroundJobManager;
    private readonly EngineRunProgressReader _progressReader;

    public TimeEngineAppService(
        IBackgroundJobManager backgroundJobManager,
        EngineRunProgressReader progressReader)
    {
        _backgroundJobManager = backgroundJobManager;
        _progressReader = progressReader;
    }

    /// <summary>
    /// Queues the run. Deliberately <b>not</b> synchronous: a run covers every employee x every day in the
    /// window (legacy's own console app took minutes to hours), so executing it inline would hold an HTTP
    /// request open for the duration.
    ///
    /// <para><c>[HttpPost]</c> is required by this repo's NSwag rule for methods taking a custom input DTO --
    /// without it the input would be flattened into query-string parameters and
    /// <c>RunTimeEngineInput</c> would never be generated into the client proxies.</para>
    /// </summary>
    [HttpPost]
    [AbpAuthorize(AppPermissions.Pages_TimeManagement_TimeEngine_Run)]
    public async Task<RunTimeEngineOutput> RunTimeEngine(RunTimeEngineInput input)
    {
        // Legacy Main()'s single up-front validation (Program.cs ~line 175). Reproduced here so the caller
        // gets a 400 instead of a job that fails on the queue.
        if (input.EndDate == default(DateTime))
        {
            throw new UserFriendlyException(L("TimeEngineEndDateRequired"));
        }

        if (input.StartDate.HasValue && input.StartDate.Value.Date > input.EndDate.Date)
        {
            throw new UserFriendlyException(L("TimeEngineStartDateAfterEndDate"));
        }

        await _backgroundJobManager.EnqueueAsync<TimeEngineBackgroundJob, TimeEngineJobArgs>(
            new TimeEngineJobArgs
            {
                TenantId = AbpSession.GetTenantId(),
                EmployeeCodes = input.EmployeeCodes,
                StartDate = input.StartDate?.Date,
                EndDate = input.EndDate.Date,
                PayrollAreaName = input.PayrollAreaName,
                // Legacy args[4]: the console app was handed a user name by its caller; here it is the
                // authenticated user, which is strictly better provenance for the ExecutionLog row.
                CurrentUserName = (await GetCurrentUserAsync()).UserName,
                // Legacy args[5]. Runs arriving through this API are never payroll-engine-initiated; the
                // payroll engine calls ITimeEngineService directly with the flag set.
                IsRanFromPayrollEngine = false,
                EmployeeSubGroupIds = input.EmployeeSubGroupIds
            });

        return new RunTimeEngineOutput { Queued = true };
    }

    /// <summary>
    /// Live status poll. Deliberately parameterless so it stays an HTTP GET, matching
    /// <c>GetProjectCostEngineStatus</c> -- the repo's <c>[HttpPost]</c> rule applies only to
    /// <c>Get*</c> methods taking a custom input DTO, which this is not.
    ///
    /// <para>Gated on the VIEW permission, not Run: an operator who may watch a run need not be able to
    /// start one.</para>
    /// </summary>
    [AbpAuthorize(AppPermissions.Pages_TimeManagement_TimeEngine)]
    public async Task<EngineRunStatusDto> GetTimeEngineStatus()
    {
        return await _progressReader.GetStatusAsync(EngineType.TimeEngine);
    }

    /// <summary>
    /// Per-employee grid. <c>[HttpPost]</c> is mandatory here: <see cref="GetEngineRunEmployeesInput"/> is a
    /// custom input DTO, and without it NSwag flattens it into query-string parameters and never emits the
    /// class into the client proxies.
    /// </summary>
    [HttpPost]
    [AbpAuthorize(AppPermissions.Pages_TimeManagement_TimeEngine)]
    public async Task<PagedResultDto<EngineRunEmployeeDto>> GetTimeEngineRunEmployees(GetEngineRunEmployeesInput input)
    {
        return await _progressReader.GetEmployeesAsync(EngineType.TimeEngine, input);
    }

    /// <summary>
    /// Legacy's Stop button. Sets a flag the engine re-reads between employees on its own connection; the
    /// engine finishes the employee in hand and then stops, so nothing is left half-written.
    /// </summary>
    [AbpAuthorize(AppPermissions.Pages_TimeManagement_TimeEngine_Run)]
    public async Task StopTimeEngine()
    {
        await _progressReader.RequestStopAsync(EngineType.TimeEngine);
    }

    /// <summary>Legacy's Clear Status button.</summary>
    [AbpAuthorize(AppPermissions.Pages_TimeManagement_TimeEngine_Run)]
    public async Task ClearTimeEngineProgress()
    {
        await _progressReader.ClearAsync(EngineType.TimeEngine);
    }

    /// <inheritdoc/>
    [AbpAuthorize(AppPermissions.Pages_TimeManagement_TimeEngine_Run)]
    public async Task<int> AbandonStaleTimeEngineRuns()
    {
        return await _progressReader.AbandonStaleRunsAsync(EngineType.TimeEngine);
    }
}
