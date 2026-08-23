using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.BackgroundJobs;
using Abp.Dependency;
using Abp.Domain.Uow;
using HCMv3.TimeManagement.Services;

namespace HCMv3.TimeManagement;

/// <summary>
/// The ONLY place a Time Engine run is actually executed. Both entry points --
/// <see cref="TimeEngineAppService.RunTimeEngine"/> (on demand) and
/// <see cref="TimeEngineAutorunBackgroundWorker"/> (nightly) -- enqueue this job; neither calls
/// <see cref="ITimeEngineService"/> directly. That keeps the two callers to a single difference: who fills in
/// <see cref="TimeEngineJobArgs"/> and how the start date was resolved.
///
/// <para>Replaces legacy's <c>Program.Main(args)</c> process launch
/// (<c>HCM10.TimeEngine/Program.cs</c> lines ~130-210, and <c>TimeAttendanceService.RunTimeEngine</c>
/// line 59, which shelled into the same <c>Main</c>).</para>
///
/// <para><b>Deviations from legacy's Main:</b>
/// (1) the <c>dd/MM/yyyy</c> string parsing of <c>args[]</c> is gone -- the arguments arrive typed;
/// (2) the connection-string / XAF type-registration plumbing is gone -- ABP owns it;
/// (3) legacy's outermost <c>catch</c> wrote a failed <c>TimeLog</c> row with no employee and swallowed the
/// exception. Reproduced in spirit but NOT verbatim: this job lets the exception surface so ABP's background
/// job manager records and (per its own policy) retries it, since the engine already writes a
/// <c>TimeLog</c> row for every per-employee and per-day failure itself and the outer catch could only ever
/// fire for a whole-run failure -- which a silently-swallowed log row would hide.</para>
/// </summary>
public class TimeEngineBackgroundJob : AsyncBackgroundJob<TimeEngineJobArgs>, ITransientDependency
{
    private readonly ITimeEngineService _timeEngineService;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public TimeEngineBackgroundJob(
        ITimeEngineService timeEngineService,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _timeEngineService = timeEngineService;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public override async Task ExecuteAsync(TimeEngineJobArgs args)
    {
        // Background jobs start with no tenant scope; every Time Engine entity is IMustHaveTenant, so the
        // scope has to be re-established explicitly or the run would see (and write) nothing.
        using (var uow = _unitOfWorkManager.Begin())
        {
            using (_unitOfWorkManager.Current.SetTenantId(args.TenantId))
            {
                var result = await _timeEngineService.CreateTimeRegisterAsync(new TimeEngineRunInput
                {
                    EmployeeCodes = args.EmployeeCodes,
                    StartDate = args.StartDate,
                    EndDate = args.EndDate,
                    PayrollAreaName = args.PayrollAreaName,
                    CurrentUserName = args.CurrentUserName,
                    IsRanFromPayrollEngine = args.IsRanFromPayrollEngine,
                    EmployeeSubGroupIds = args.EmployeeSubGroupIds ?? new Dictionary<int, int>()
                });

                Logger.Info(string.Format(
                    "TimeEngine run finished. ExecutionLogId={0} Status={1} Total={2} Success={3} Failed={4} Aborted={5}",
                    result.ExecutionLogId, result.ExecutionStatus, result.TotalRecords,
                    result.Successfull, result.Failed, result.Aborted));

                // CompleteAsync MUST stay INSIDE the SetTenantId scope. SaveChanges happens here, not during
                // the run, so if the scope is already disposed every IMustHaveTenant entity the run tracked is
                // flushed with TenantId 0 and ABP throws "Can not set TenantId to 0 for IMustHaveTenant
                // entities!", rolling back the whole run. That made every engine run queued from the UI fail
                // after doing all its work: nothing was written and the progress panel stayed empty.
                await uow.CompleteAsync();
            }
        }
    }
}
