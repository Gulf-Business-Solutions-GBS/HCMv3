using System;
using System.Threading.Tasks;
using Abp.BackgroundJobs;
using Abp.Dependency;
using Abp.Domain.Uow;
using Abp.Runtime.Session;
using Abp.Timing;
using HCMv3.GoLiveSetup.Dto;

namespace HCMv3.GoLiveSetup;

/// <summary>The run a Start payroll queued: which company, month and payroll area, and who asked.</summary>
[Serializable]
public class GoLivePayrollRunJobArgs
{
    public int TenantId { get; set; }

    public long UserId { get; set; }

    public string PayrollAreaName { get; set; }

    public DateTime PayInDate { get; set; }

    /// <summary>The employees picked to run; null runs everyone in the payroll area.</summary>
    public System.Collections.Generic.List<string> EmployeeCodes { get; set; }
}

/// <summary>
/// Runs a company's payroll in the background: the same Run payroll the screen used to wait on, now outside
/// any browser request, so a locked screen or a closed tab no longer takes the run with it.
///
/// <para>It runs as the person who started it -- their tenant and their permissions -- because both engines
/// and the batches' own units of work read the tenant from the session, and a job has none of its own.</para>
///
/// <para>It never throws. A background job that throws is retried, and a retried payroll run would be a
/// second payroll nobody asked for; a failure is written into the run's state instead, where the screen
/// reads it.</para>
/// </summary>
public class GoLivePayrollRunJob : AsyncBackgroundJob<GoLivePayrollRunJobArgs>, ITransientDependency
{
    private readonly IAbpSession _session;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IIocResolver _iocResolver;
    private readonly GoLiveRunTracker _tracker;

    public GoLivePayrollRunJob(
        IAbpSession session,
        IUnitOfWorkManager unitOfWorkManager,
        IIocResolver iocResolver,
        GoLiveRunTracker tracker)
    {
        _session = session;
        _unitOfWorkManager = unitOfWorkManager;
        _iocResolver = iocResolver;
        _tracker = tracker;
    }

    public override async Task ExecuteAsync(GoLivePayrollRunJobArgs args)
    {
        using (_session.Use(args.TenantId, args.UserId))
        {
            try
            {
                await _tracker.UpdateAsync(s => s.Status = GoLiveRunStatus.Running);

                GoLiveRunPayrollOutput result;
                using (var goLive = _iocResolver.ResolveAsDisposable<IGoLiveSetupAppService>())
                using (var uow = _unitOfWorkManager.Begin())
                {
                    result = await goLive.Object.RunPayroll(new GoLiveRunPayrollInput
                    {
                        PayDate = args.PayInDate,
                        PayrollAreaName = args.PayrollAreaName,
                        EmployeeCodes = args.EmployeeCodes,
                    });
                    await uow.CompleteAsync();
                }

                await _tracker.UpdateAsync(state =>
                {
                    // Start set up the month and added any missing leave pay links before queuing; the run
                    // found both already done, so the result carries what Start did.
                    result.MonthSetUp ??= state.MonthSetUp;
                    result.LeavePayLinksAdded = Math.Max(result.LeavePayLinksAdded, state.LeavePayLinksAdded);

                    state.Result = result;
                    state.FinishedAt = Clock.Now;
                    state.Status = result.StoppedAt == GoLiveStoppedAt.ByUser
                        ? GoLiveRunStatus.Stopped
                        : GoLiveRunStatus.Completed;
                });
            }
            catch (Exception ex)
            {
                Logger.Error("Go-Live Setup: the background payroll run failed.", ex);
                await _tracker.UpdateAsync(state =>
                {
                    state.Result = new GoLiveRunPayrollOutput
                    {
                        StoppedAt = GoLiveStoppedAt.Engine,
                        Message = ex.Message,
                        PayInDate = args.PayInDate,
                    };
                    state.FinishedAt = Clock.Now;
                    state.Status = GoLiveRunStatus.Failed;
                });
            }
        }
    }
}
