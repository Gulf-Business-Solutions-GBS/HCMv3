using System;
using System.Threading.Tasks;
using Abp.BackgroundJobs;
using Abp.Dependency;
using Abp.Domain.Uow;
using Abp.Runtime.Session;
using HCMv3.EngineRuns;
using HCMv3.EngineRuns.Services;
using HCMv3.PayrollManagement.Services;

namespace HCMv3.PayrollManagement;

/// <summary>
/// The ONLY place a Payroll Engine run is actually executed. <see cref="PayrollEngineAppService"/> enqueues
/// this job and never calls <see cref="IPayrollEngineService"/> directly -- the same single-execution-path
/// arrangement as <c>TimeEngineBackgroundJob</c>.
///
/// <para>Replaces legacy's <c>Program.Main(args)</c> / <c>SimulationPayrollHelper.Main(args)</c> process launch
/// (<c>HCM10.PayrollEngine/Program.cs</c> lines 154-215; <c>uscPayrollEngine.ascx.cs</c> lines 214-217).</para>
///
/// <para><b>Saved in batches.</b> The run is handed <see cref="PayrollEngineBatches.Size"/>, so the engine saves
/// and commits ten employees at a time, each batch on its own (Go-Live Setup's IT11 fix). The unit of work this
/// job opens around the run is therefore NOT transactional: the little the run writes outside the batches (the
/// execution log, a missing settings row, a simulation's clearing of its old registers) is saved as it happens,
/// so no batch ever waits on a lock this job is holding.</para>
///
/// <para><b>It never throws.</b> A background job that throws is retried by ABP's job manager, and a retried
/// payroll run is a SECOND payroll run nobody asked for -- the same reason <c>GoLivePayrollRunJob</c> never
/// throws. A failure is logged and written onto the run's progress row as Failed with its reason, where the
/// Payroll Engine screen shows it. (Legacy's outermost <c>catch</c>, lines 352-369, likewise wrote a failed
/// <c>PayrollLog</c> row and swallowed the exception.)</para>
///
/// <para><b>It runs as the person who started it</b> (their tenant and user, from the job arguments), because
/// the batches' units of work and the progress writer read the tenant from the session, and a job has none of
/// its own.</para>
/// </summary>
public class PayrollEngineBackgroundJob : AsyncBackgroundJob<PayrollEngineJobArgs>, ITransientDependency
{
    private readonly IPayrollEngineService _payrollEngineService;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IAbpSession _session;
    private readonly IEngineRunProgressWriter _progressWriter;

    public PayrollEngineBackgroundJob(
        IPayrollEngineService payrollEngineService,
        IUnitOfWorkManager unitOfWorkManager,
        IAbpSession session,
        IEngineRunProgressWriter progressWriter)
    {
        _payrollEngineService = payrollEngineService;
        _unitOfWorkManager = unitOfWorkManager;
        _session = session;
        _progressWriter = progressWriter;
    }

    public override async Task ExecuteAsync(PayrollEngineJobArgs args)
    {
        var input = new PayrollEngineRunInput
        {
            PayrollRunType = args.PayrollRunType,
            EmployeeCodes = args.EmployeeCodes,
            PayInDate = args.PayInDate,
            PayrollAreaName = args.PayrollAreaName,
            OffCyclePaymentDate = args.OffCyclePaymentDate,
            OffCycleReason = args.OffCycleReason,
            RegisterTarget = args.RegisterTarget,
            CommitBatchSize = PayrollEngineBatches.Size
        };

        using (_session.Use(args.TenantId, args.UserId))
        {
            try
            {
                // Background jobs start with no tenant scope; every payroll entity is IMustHaveTenant, so the
                // scope is also set on the unit of work explicitly.
                using (var uow = _unitOfWorkManager.Begin(new UnitOfWorkOptions { IsTransactional = false }))
                using (_unitOfWorkManager.Current.SetTenantId(args.TenantId))
                {
                    var result = await _payrollEngineService.RunAsync(input);

                    Logger.Info(string.Format(
                        "PayrollEngine run finished. Target={0} PayInDate={1:dd/MM/yyyy} Employees={2} Succeeded={3} Failed={4} Aborted={5}",
                        input.RegisterTarget,
                        result.PayInDate,
                        result.EmployeeResults.Count,
                        result.EmployeeResults.FindAll(r => r.Succeeded).Count,
                        result.EmployeeResults.FindAll(r => !r.Succeeded).Count,
                        result.Aborted));

                    // CompleteAsync MUST stay INSIDE the SetTenantId scope: if the scope is already disposed,
                    // every IMustHaveTenant entity still pending is flushed with TenantId 0 and ABP throws
                    // "Can not set TenantId to 0 for IMustHaveTenant entities!".
                    await uow.CompleteAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(string.Format(
                    "PayrollEngine run failed and will NOT be retried (a retry would be a second payroll run). Area={0} PayInDate={1:dd/MM/yyyy} Target={2}",
                    args.PayrollAreaName, args.PayInDate, args.RegisterTarget), ex);

                await RecordFailureAsync(input, ex);
            }
        }
    }

    /// <summary>
    /// Puts the failure on a progress row the screen can show. A failure during the run is already there (the
    /// engine closes its own row as Failed); one the engine raised before beginning -- a refused pay date, a
    /// missing control record or calendar, a simulation the settings do not allow -- gets a row of its own,
    /// opened and closed as Failed with the reason. Not when another run of the area is going: a row for the
    /// refusal would become the "latest run" and hide that run's progress.
    /// </summary>
    private async Task RecordFailureAsync(PayrollEngineRunInput input, Exception ex)
    {
        try
        {
            if (ex.Data.Contains(PayrollEngineService.RunFailureRecordedKey))
            {
                return;
            }

            var runKey = PayrollEngineService.BuildRunKey(input.PayrollAreaName);

            if (await _progressWriter.GetActiveRunAsync(EngineType.PayrollEngine, runKey) != null)
            {
                return;
            }

            var engineRunId = await _progressWriter.BeginRunAsync(new EngineRunStartInfo
            {
                EngineType = EngineType.PayrollEngine,
                RunKey = runKey,
                Parameters = PayrollEngineService.DescribeRun(input)
            });

            await _progressWriter.CompleteRunAsync(engineRunId, EngineRunStatus.Failed, null, ex.Message);
        }
        catch (Exception recordFailure)
        {
            // Nothing more can be done; the original failure is already in the log.
            Logger.Warn("PayrollEngine: could not record the run failure on a progress row.", recordFailure);
        }
    }
}
