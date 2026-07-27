using System;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.BackgroundJobs;
using Abp.Runtime.Session;
using Abp.UI;
using HCMv3.Authorization;
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

    public TimeEngineAppService(IBackgroundJobManager backgroundJobManager)
    {
        _backgroundJobManager = backgroundJobManager;
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
}
