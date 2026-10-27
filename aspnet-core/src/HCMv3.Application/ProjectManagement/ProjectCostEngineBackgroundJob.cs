using System;
using System.Threading.Tasks;
using Abp.BackgroundJobs;
using Abp.Dependency;
using Abp.Domain.Uow;
using Abp.Runtime.Session;
using HCMv3.EngineRuns;
using HCMv3.EngineRuns.Services;
using HCMv3.ProjectManagement.Services;

namespace HCMv3.ProjectManagement;

/// <summary>
/// The ONLY place a Project Cost Engine run is actually executed -- the same single-execution-path arrangement as
/// <c>PayrollEngineBackgroundJob</c>/<c>TimeEngineBackgroundJob</c>. Replaces legacy's
/// <c>ThreadPool.QueueUserWorkItem(new WaitCallback(ExecuteLongOperation), this)</c>
/// (<c>uscProjectCosting.ascx.cs</c> line 1521).
///
/// <para><b>Saved in batches.</b> The run is handed <see cref="ProjectCostEngineBatches.Size"/>, so the engine saves
/// and commits ten employees at a time, each batch on its own (Go-Live Setup's IT11 fix). The unit of work this job
/// opens around the run is therefore NOT transactional: what the run writes outside the batches (clearing the log,
/// the execution log, the run-level failure row) is saved as it happens, so no batch waits on a lock this job holds
/// and a late failure does not roll back the batches already done.</para>
///
/// <para><b>It never throws.</b> A background job that throws is retried by ABP's job manager, and a retried run is a
/// second run nobody asked for. A failure is logged and put on the run's progress row as Failed with its reason, where
/// the screen shows it. (Legacy's <c>Main</c> catch, lines 266-282, likewise wrote a failed <c>ProjectLog</c> row and
/// swallowed the exception.)</para>
///
/// <para><b>It runs as the person who started it</b> (their tenant and user, from the job arguments), because the
/// batches' units of work and the progress writer read the tenant from the session, and a job has none of its own.</para>
/// </summary>
public class ProjectCostEngineBackgroundJob : AsyncBackgroundJob<ProjectCostEngineJobArgs>, ITransientDependency
{
    private readonly IProjectCostEngineService _projectCostEngineService;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IAbpSession _session;
    private readonly IEngineRunProgressWriter _progressWriter;

    public ProjectCostEngineBackgroundJob(
        IProjectCostEngineService projectCostEngineService,
        IUnitOfWorkManager unitOfWorkManager,
        IAbpSession session,
        IEngineRunProgressWriter progressWriter)
    {
        _projectCostEngineService = projectCostEngineService;
        _unitOfWorkManager = unitOfWorkManager;
        _session = session;
        _progressWriter = progressWriter;
    }

    public override async Task ExecuteAsync(ProjectCostEngineJobArgs args)
    {
        var input = new ProjectCostEngineRunInput
        {
            PayrollRunType = args.PayrollRunType,
            EmployeeCodes = args.EmployeeCodes,
            PayInDate = args.PayInDate,
            PayrollAreaName = args.PayrollAreaName,
            OffCyclePaymentDate = args.OffCyclePaymentDate,
            OffCycleReason = args.OffCycleReason,
            CurrentUserName = args.CurrentUserName,
            CommitBatchSize = ProjectCostEngineBatches.Size
        };

        using (_session.Use(args.TenantId, args.UserId))
        {
            try
            {
                // Background jobs start with no tenant scope; every entity involved is IMustHaveTenant, so the
                // scope is also set on the unit of work explicitly.
                using (var uow = _unitOfWorkManager.Begin(new UnitOfWorkOptions { IsTransactional = false }))
                using (_unitOfWorkManager.Current.SetTenantId(args.TenantId))
                {
                    var result = await _projectCostEngineService.RunAsync(input);

                    Logger.Info(string.Format(
                        "ProjectCostEngine run finished. PayInDate={0:dd/MM/yyyy} Employees={1} Succeeded={2} Failed={3} Aborted={4}",
                        result.PayInDate,
                        result.EmployeeResults.Count,
                        result.EmployeeResults.FindAll(r => r.Succeeded).Count,
                        result.EmployeeResults.FindAll(r => !r.Succeeded).Count,
                        result.Aborted));

                    // CompleteAsync MUST stay INSIDE the SetTenantId scope: if the scope is already disposed, every
                    // IMustHaveTenant entity still pending is flushed with TenantId 0 and ABP throws "Can not set
                    // TenantId to 0 for IMustHaveTenant entities!".
                    await uow.CompleteAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(string.Format(
                    "ProjectCostEngine run failed and will NOT be retried (a retry would be a second run). Area={0} PayInDate={1:dd/MM/yyyy}",
                    args.PayrollAreaName, args.PayInDate), ex);

                await RecordFailureAsync(input, ex);
            }
        }
    }

    /// <summary>
    /// Puts the failure on a progress row the screen can show. A failure during the run is already there (the engine
    /// closes its own row as Failed); one the engine raised before beginning -- a refused pay date, a missing control
    /// record or calendar, an off-cycle run -- gets a row of its own, opened and closed as Failed with the reason. Not
    /// when another run is going: a row for the refusal would become the "latest run" and hide that run's progress.
    /// </summary>
    private async Task RecordFailureAsync(ProjectCostEngineRunInput input, Exception ex)
    {
        try
        {
            if (ex.Data.Contains(ProjectCostEngineService.RunFailureRecordedKey))
            {
                return;
            }

            if (await _progressWriter.GetActiveRunAsync(EngineType.ProjectCostEngine, ProjectCostEngineService.RunKey) != null)
            {
                return;
            }

            var engineRunId = await _progressWriter.BeginRunAsync(new EngineRunStartInfo
            {
                EngineType = EngineType.ProjectCostEngine,
                RunKey = ProjectCostEngineService.RunKey,
                StartedByUserName = input.CurrentUserName,
                Parameters = ProjectCostEngineService.BuildExecutionLogParameters(input, input.PayInDate)
            });

            await _progressWriter.CompleteRunAsync(engineRunId, EngineRunStatus.Failed, null, ex.Message);
        }
        catch (Exception recordFailure)
        {
            // Nothing more can be done; the original failure is already in the log.
            Logger.Warn("ProjectCostEngine: could not record the run failure on a progress row.", recordFailure);
        }
    }
}
