using System;
using System.Linq;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Threading;
using Abp.Threading.BackgroundWorkers;
using Abp.Threading.Timers;
using Abp.Timing;
using HCMv3.MultiTenancy;
using HCMv3.ReportManagement.Services;

namespace HCMv3.ReportManagement;

/// <summary>
/// Fires report schedules whose next occurrence has arrived.
///
/// WHY A PERIODIC WORKER AND NOT A HANGFIRE RECURRING JOB. Hangfire is present as a package but
/// DISABLED in this application -- <c>WebConsts.HangfireDashboardEnabled</c> is false, so
/// <c>AddHangfire</c>/<c>AddHangfireServer</c> never run and the app uses ABP's default background job
/// manager. A <c>RecurringJob.AddOrUpdate</c> would register against storage nothing is polling and
/// would simply never fire -- the worst possible failure for a distribution module, because the UI
/// would look correct and no report would ever arrive. This uses ABP's own periodic worker, which is
/// the mechanism already used for the nightly Time Engine autorun, and keeps working unchanged if
/// Hangfire is switched on later.
///
/// The worker only DECIDES what is due. Execution goes through the same
/// <see cref="IReportScheduleExecutor"/> that the Run Now button uses, so a scheduled run and a manual
/// run cannot drift apart in behaviour.
/// </summary>
public class ReportScheduleDispatcherWorker : PeriodicBackgroundWorkerBase, ISingletonDependency
{
    /// <summary>
    /// Five minutes. The occurrence patterns are daily at coarsest, so a tighter poll buys nothing but
    /// database churn; a looser one makes "first occurrence" times visibly inaccurate.
    /// </summary>
    private const int CheckPeriodAsMilliseconds = 5 * 60 * 1000;

    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IRepository<ReportSchedule> _scheduleRepository;
    private readonly IRepository<Tenant> _tenantRepository;
    private readonly IReportScheduleExecutor _executor;

    public ReportScheduleDispatcherWorker(
        AbpTimer timer,
        IUnitOfWorkManager unitOfWorkManager,
        IRepository<ReportSchedule> scheduleRepository,
        IRepository<Tenant> tenantRepository,
        IReportScheduleExecutor executor)
        : base(timer)
    {
        _unitOfWorkManager = unitOfWorkManager;
        _scheduleRepository = scheduleRepository;
        _tenantRepository = tenantRepository;
        _executor = executor;

        Timer.Period = CheckPeriodAsMilliseconds;

        // Not on start-up: a deployment or an app-pool recycle would otherwise fire every overdue
        // schedule at once, mailing a burst of duplicates to everybody.
        Timer.RunOnStart = false;
    }

    protected override void DoWork()
    {
        var tenantIds = _unitOfWorkManager.WithUnitOfWork(() =>
            _tenantRepository.GetAll().Where(t => t.IsActive).Select(t => t.Id).ToList());

        foreach (var tenantId in tenantIds)
        {
            try
            {
                DispatchForTenant(tenantId);
            }
            catch (Exception ex)
            {
                // One tenant's failure must not stop the others -- but it is logged, never swallowed.
                Logger.Error($"Report schedule dispatch failed for tenant {tenantId}: {ex.Message}", ex);
            }
        }
    }

    private void DispatchForTenant(int tenantId)
    {
        var now = Clock.Now;

        var dueScheduleIds = _unitOfWorkManager.WithUnitOfWork(() =>
        {
            using (_unitOfWorkManager.Current.SetTenantId(tenantId))
            {
                // Candidates are narrowed in SQL, then IsDue decides -- the occurrence rules involve
                // anchor arithmetic that does not translate to a query.
                return _scheduleRepository.GetAll()
                    .Where(s => s.IsActive)
                    .ToList()
                    .Where(s => ReportScheduleOccurrenceCalculator.IsDue(s, now))
                    .Select(s => s.Id)
                    .ToList();
            }
        });

        foreach (var scheduleId in dueScheduleIds)
        {
            try
            {
                _unitOfWorkManager.WithUnitOfWork(() =>
                {
                    using (_unitOfWorkManager.Current.SetTenantId(tenantId))
                    {
                        // Advance the schedule BEFORE running it. If the run throws or the process dies
                        // mid-way, the schedule has already moved on, so the next tick does not re-fire
                        // the same occurrence and mail everybody a second copy. A missed run is
                        // recoverable with Run Now; duplicate payroll e-mails are not recallable.
                        AdvanceNextRunTime(scheduleId, now);
                    }
                });

                _unitOfWorkManager.WithUnitOfWork(() =>
                {
                    using (_unitOfWorkManager.Current.SetTenantId(tenantId))
                    {
                        // Same executor as the Run Now button: a scheduled run and a manual run must not
                        // be able to behave differently.
                        AsyncHelper.RunSync(() => _executor.ExecuteAsync(scheduleId, isManual: false));
                    }
                });
            }
            catch (Exception ex)
            {
                // The executor already records per-slice failures against the run; this catches a
                // failure of the dispatch itself so one bad schedule cannot stop the rest.
                Logger.Error(
                    $"Report schedule {scheduleId} (tenant {tenantId}) failed to dispatch: {ex.Message}",
                    ex);
            }
        }
    }

    private void AdvanceNextRunTime(int scheduleId, DateTime now)
    {
        var schedule = _scheduleRepository.Get(scheduleId);

        schedule.NextRunTime = ReportScheduleOccurrenceCalculator.GetNextRunTime(schedule, now);

        // A one-off has no next occurrence: deactivate it so it stops being a candidate every tick and
        // so the list screen shows plainly that it is finished rather than merely quiet.
        if (schedule.OccurrenceType == ReportScheduleOccurrenceType.Once)
        {
            schedule.IsActive = false;
        }

        _scheduleRepository.Update(schedule);
    }
}
