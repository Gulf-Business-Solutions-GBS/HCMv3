using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.BackgroundJobs;
using Abp.Dependency;
using Abp.Domain.Uow;
using Abp.Runtime.Session;
using HCMv3.EngineRuns;
using HCMv3.EngineRuns.Services;
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
/// <para><b>Saved in batches.</b> The run is handed <see cref="TimeEngineBatches.Size"/>, so the engine saves and
/// commits ten employees at a time, each batch on its own -- legacy committed per employee and every 500 changes
/// (Program.cs lines 1599-1602, 1705-1711). The unit of work this job opens around the run is therefore NOT
/// transactional: the little the run writes outside the batches (the execution log, a missing settings row) is
/// saved as it happens, so no batch ever waits on a lock this job is holding. Same arrangement as
/// <c>PayrollEngineBackgroundJob</c>.</para>
///
/// <para><b>It never throws.</b> A background job that throws is retried by ABP's job manager, and a retried run is
/// a second run of the same area nobody asked for. A failure is logged and written onto the run's progress row as
/// Failed with its reason, where the Time Engine screen shows it. Legacy's <c>Main</c> did the same in its own
/// way: its outermost <c>catch</c> (lines 185-199) wrote a failed <c>TimeLog</c> row and swallowed the
/// exception.</para>
///
/// <para><b>It runs as the person who started it</b> (their tenant and user, from the job arguments).</para>
/// </summary>
public class TimeEngineBackgroundJob : AsyncBackgroundJob<TimeEngineJobArgs>, ITransientDependency
{
    private readonly ITimeEngineService _timeEngineService;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IAbpSession _session;
    private readonly IEngineRunProgressWriter _progressWriter;

    public TimeEngineBackgroundJob(
        ITimeEngineService timeEngineService,
        IUnitOfWorkManager unitOfWorkManager,
        IAbpSession session,
        IEngineRunProgressWriter progressWriter)
    {
        _timeEngineService = timeEngineService;
        _unitOfWorkManager = unitOfWorkManager;
        _session = session;
        _progressWriter = progressWriter;
    }

    public override async Task ExecuteAsync(TimeEngineJobArgs args)
    {
        var input = new TimeEngineRunInput
        {
            EmployeeCodes = args.EmployeeCodes,
            StartDate = args.StartDate,
            EndDate = args.EndDate,
            PayrollAreaName = args.PayrollAreaName,
            CurrentUserName = args.CurrentUserName,
            IsRanFromPayrollEngine = args.IsRanFromPayrollEngine,
            EmployeeSubGroupIds = args.EmployeeSubGroupIds ?? new Dictionary<int, int>(),
            CommitBatchSize = TimeEngineBatches.Size
        };

        using (_session.Use(args.TenantId, args.UserId))
        {
            try
            {
                // Background jobs start with no tenant scope; every Time Engine entity is IMustHaveTenant, so the
                // scope is also set on the unit of work explicitly.
                using (var uow = _unitOfWorkManager.Begin(new UnitOfWorkOptions { IsTransactional = false }))
                using (_unitOfWorkManager.Current.SetTenantId(args.TenantId))
                {
                    var result = await _timeEngineService.CreateTimeRegisterAsync(input);

                    Logger.Info(string.Format(
                        "TimeEngine run finished. ExecutionLogId={0} Status={1} Total={2} Success={3} Failed={4} Aborted={5}",
                        result.ExecutionLogId, result.ExecutionStatus, result.TotalRecords,
                        result.Successfull, result.Failed, result.Aborted));

                    // CompleteAsync MUST stay INSIDE the SetTenantId scope: if the scope is already disposed,
                    // every IMustHaveTenant entity still pending is flushed with TenantId 0 and ABP throws
                    // "Can not set TenantId to 0 for IMustHaveTenant entities!".
                    await uow.CompleteAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(string.Format(
                    "TimeEngine run failed and will NOT be retried (a retry would be a second run). Area={0} Codes={1} Start={2:dd/MM/yyyy} End={3:dd/MM/yyyy}",
                    args.PayrollAreaName, args.EmployeeCodes, args.StartDate, args.EndDate), ex);

                await RecordFailureAsync(input, ex);
            }
        }
    }

    /// <summary>
    /// Puts the failure on a progress row the screen can show. A failure during the run is already there (the
    /// engine closes its own row as Failed); one raised before the run began -- a missing end date, another run of
    /// the same area going -- gets a row of its own, opened and closed as Failed with the reason. Not when another
    /// run of the area is going: a row for the refusal would become that area's "latest run" and hide the run's
    /// progress.
    /// </summary>
    private async Task RecordFailureAsync(TimeEngineRunInput input, Exception ex)
    {
        try
        {
            if (ex.Data.Contains(TimeEngineService.RunFailureRecordedKey))
            {
                return;
            }

            var runKey = TimeEngineRunKeys.ForRun(input, input.PayrollAreaName);

            if (runKey != null && await _progressWriter.GetActiveRunAsync(EngineType.TimeEngine, runKey) != null)
            {
                return;
            }

            var engineRunId = await _progressWriter.BeginRunAsync(new EngineRunStartInfo
            {
                EngineType = EngineType.TimeEngine,
                RunKey = runKey,
                StartedByUserName = input.CurrentUserName,
                Parameters = TimeEngineService.DescribeRun(input)
            });

            await _progressWriter.CompleteRunAsync(engineRunId, EngineRunStatus.Failed, null, ex.Message);
        }
        catch (Exception recordFailure)
        {
            // Nothing more can be done; the original failure is already in the log.
            Logger.Warn("TimeEngine: could not record the run failure on a progress row.", recordFailure);
        }
    }
}
