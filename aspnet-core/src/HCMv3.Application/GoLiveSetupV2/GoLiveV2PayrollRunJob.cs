using System;
using System.Threading.Tasks;
using Abp.BackgroundJobs;
using Abp.Dependency;
using Abp.Domain.Uow;
using Abp.Runtime.Session;
using Abp.Timing;
using HCMv3.GoLiveSetupV2.Dto;

namespace HCMv3.GoLiveSetupV2;

/// <summary>The run a Start payroll queued: which company, month and payroll area, and who asked.</summary>
[Serializable]
public class GoLiveV2PayrollRunJobArgs
{
    public int TenantId { get; set; }

    public long UserId { get; set; }

    public string PayrollAreaName { get; set; }

    public DateTime PayInDate { get; set; }
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
public class GoLiveV2PayrollRunJob : AsyncBackgroundJob<GoLiveV2PayrollRunJobArgs>, ITransientDependency
{
    private readonly IAbpSession _session;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IIocResolver _iocResolver;
    private readonly GoLiveV2RunTracker _tracker;

    public GoLiveV2PayrollRunJob(
        IAbpSession session,
        IUnitOfWorkManager unitOfWorkManager,
        IIocResolver iocResolver,
        GoLiveV2RunTracker tracker)
    {
        _session = session;
        _unitOfWorkManager = unitOfWorkManager;
        _iocResolver = iocResolver;
        _tracker = tracker;
    }

    public override async Task ExecuteAsync(GoLiveV2PayrollRunJobArgs args)
    {
        using (_session.Use(args.TenantId, args.UserId))
        {
            try
            {
                await _tracker.UpdateAsync(s => s.Status = GoLiveV2RunStatus.Running);

                GoLiveV2RunPayrollOutput result;
                using (var goLive = _iocResolver.ResolveAsDisposable<IGoLiveV2AppService>())
                using (var uow = _unitOfWorkManager.Begin())
                {
                    result = await goLive.Object.RunPayroll(new GoLiveV2RunPayrollInput
                    {
                        PayDate = args.PayInDate,
                        PayrollAreaName = args.PayrollAreaName,
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
                    state.Status = result.StoppedAt == GoLiveV2StoppedAt.ByUser
                        ? GoLiveV2RunStatus.Stopped
                        : GoLiveV2RunStatus.Completed;
                });
            }
            catch (Exception ex)
            {
                Logger.Error("Go-Live Setup V2: the background payroll run failed.", ex);
                await _tracker.UpdateAsync(state =>
                {
                    state.Result = new GoLiveV2RunPayrollOutput
                    {
                        StoppedAt = GoLiveV2StoppedAt.Engine,
                        Message = ex.Message,
                        PayInDate = args.PayInDate,
                    };
                    state.FinishedAt = Clock.Now;
                    state.Status = GoLiveV2RunStatus.Failed;
                });
            }
        }
    }
}
