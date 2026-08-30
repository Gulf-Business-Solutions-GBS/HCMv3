using System;
using System.Linq;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Notifications;
using Abp.Threading;
using Abp.Threading.BackgroundWorkers;
using Abp.Threading.Timers;
using HCMv3.MultiTenancy;
using HCMv3.Notifications;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow.Services;

namespace HCMv3.Workflow;

/// <summary>
/// Runs the time-based half of the workflow engine: escalating stalled requests and reminding approvers who
/// have not acted.
///
/// <para>
/// Until this existed, <c>ShouldEscalate</c>, <c>EscalateAfterDays</c> and <c>ReminderDays</c> were columns
/// an administrator could set and nothing would ever read. That is a worse failure than the feature being
/// absent: the configuration screen says escalation is on, so nobody goes looking for why a request sat for
/// three weeks.
/// </para>
///
/// <para>
/// AN ABP PERIODIC WORKER, not a Hangfire recurring job -- Hangfire is referenced here but DISABLED
/// (<c>WebConsts.HangfireDashboardEnabled</c> is false), so a recurring job would register against storage
/// nothing polls and silently never fire. Same reasoning, and same mechanism, as the report schedule
/// dispatcher and the nightly Time Engine run.
/// </para>
/// </summary>
public class WorkflowEscalationWorker : PeriodicBackgroundWorkerBase, ISingletonDependency
{
    /// <summary>
    /// Hourly. Both rules are expressed in whole days, so a tighter poll buys nothing; but a daily poll
    /// would make the exact firing time depend on when the host last restarted, and an escalation that
    /// happens at an unpredictable hour is hard to tell apart from one that did not happen.
    /// </summary>
    private const int CheckPeriodAsMilliseconds = 60 * 60 * 1000;

    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IRepository<Tenant> _tenantRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IWorkflowEscalationService _escalationService;
    private readonly IAppNotifier _appNotifier;

    public WorkflowEscalationWorker(
        AbpTimer timer,
        IUnitOfWorkManager unitOfWorkManager,
        IRepository<Tenant> tenantRepository,
        IRepository<Employee> employeeRepository,
        IWorkflowEscalationService escalationService,
        IAppNotifier appNotifier)
        : base(timer)
    {
        _unitOfWorkManager = unitOfWorkManager;
        _tenantRepository = tenantRepository;
        _employeeRepository = employeeRepository;
        _escalationService = escalationService;
        _appNotifier = appNotifier;

        Timer.Period = CheckPeriodAsMilliseconds;

        // Not on start-up. A deployment or app-pool recycle would otherwise escalate every overdue request
        // at once and mail a burst of reminders -- and a restart loop would do it repeatedly.
        Timer.RunOnStart = false;

        // Lets the base class resolve L("...") against this application's own resources, as the Time Engine
        // worker does.
        LocalizationSourceName = HCMv3Consts.LocalizationSourceName;
    }

    protected override void DoWork()
    {
        var tenantIds = _unitOfWorkManager.WithUnitOfWork(() =>
            _tenantRepository.GetAll().Where(t => t.IsActive).Select(t => t.Id).ToList());

        foreach (var tenantId in tenantIds)
        {
            try
            {
                RunForTenant(tenantId);
            }
            catch (Exception ex)
            {
                // One tenant's failure must not stop the others -- but it is logged, never swallowed.
                Logger.Error($"Workflow escalation run failed for tenant {tenantId}: {ex.Message}", ex);
            }
        }
    }

    private void RunForTenant(int tenantId)
    {
        _unitOfWorkManager.WithUnitOfWork(() =>
        {
            using (_unitOfWorkManager.Current.SetTenantId(tenantId))
            {
                var escalated = AsyncHelper.RunSync(() => _escalationService.EscalateStalledRequestsAsync());

                foreach (var result in escalated)
                {
                    Logger.Info(
                        result.MovedToNextLevel
                            ? $"Workflow request {result.RequestNumber} escalated from step {result.FromStep} to step {result.ToStep}."
                            : $"Workflow request {result.RequestNumber} escalated sideways at step {result.FromStep}.");
                }
            }
        });

        SendRemindersForTenant(tenantId);
    }

    /// <summary>
    /// Reminders go out in their own unit of work, after escalation has committed.
    ///
    /// <para>
    /// Separating them matters: a request escalated a moment ago has a fresh action log entry, so it is no
    /// longer stalled and must not also be chased. Reading the reminders inside the escalation transaction
    /// would see the pre-escalation state and send a nudge about a request that had just moved.
    /// </para>
    /// </summary>
    private void SendRemindersForTenant(int tenantId)
    {
        _unitOfWorkManager.WithUnitOfWork(() =>
        {
            using (_unitOfWorkManager.Current.SetTenantId(tenantId))
            {
                var due = AsyncHelper.RunSync(() => _escalationService.GetRemindersDueAsync());

                foreach (var reminder in due)
                {
                    try
                    {
                        NotifyApprovers(tenantId, reminder);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(
                            $"Could not send workflow reminder for request {reminder.RequestNumber}: {ex.Message}",
                            ex);
                    }
                }
            }
        });
    }

    private void NotifyApprovers(int tenantId, WorkflowReminderDue reminder)
    {
        // Approvers are employees; notifications go to users. An approver with no linked user account simply
        // cannot be reached this way, which is a reason to link them rather than to fail the whole run.
        var userIds = _employeeRepository.GetAll()
            .Where(e => reminder.ApproverEmployeeIds.Contains(e.Id) && e.UserId.HasValue)
            .Select(e => e.UserId.Value)
            .Distinct()
            .ToList();

        if (userIds.Count == 0)
        {
            return;
        }

        var message = string.Format(
            L("WorkflowRequestHasBeenAwaitingYourApprovalForDays"),
            reminder.RequestNumber,
            reminder.SummaryEn,
            reminder.PendingDays);

        foreach (var userId in userIds)
        {
            AsyncHelper.RunSync(() => _appNotifier.SendMessageAsync(
                new Abp.UserIdentifier(tenantId, userId),
                message,
                NotificationSeverity.Warn));
        }
    }
}
