using System;
using System.Linq;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Threading;
using Abp.Threading.Timers;
using Abp.Timing;
using HCMv3.BackgroundJobs;
using HCMv3.HcmNotifications.Daily;
using HCMv3.HcmNotifications.Rules;
using HCMv3.MultiTenancy;
using HCMv3.PayrollManagement.Services;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.SystemSecurity.Services;

namespace HCMv3.Notifications;

/// <summary>
/// Runs the notifications that are due because of the DATE rather than because somebody did something:
/// birthdays, service anniversaries, leave pay owed before somebody travels, a return from leave that never
/// came, and a punch recorded while an employee was supposed to be away.
///
/// <para>
/// Every one of these was ported, and tested, and then never ran: <see cref="INotificationDispatchService"/>
/// had no caller outside its own tests. That is the same failure the escalation worker was written to fix --
/// the configuration screen offers the notification, an administrator switches it on, and nothing happens. A
/// notification nobody receives is harder to notice missing than one that was never built.
/// </para>
///
/// <para>
/// An ABP periodic worker rather than a Hangfire recurring job, for the reason given on
/// <c>WorkflowEscalationWorker</c>: Hangfire is referenced here but disabled, so a recurring job would
/// register against storage nothing polls.
/// </para>
/// </summary>
public class DailyNotificationWorker : RecordedPeriodicBackgroundWorkerBase, ISingletonDependency
{
    /// <summary>
    /// Every four hours, not daily. All five runs ask "is today the day", so a run can only fire once per
    /// day per recipient anyway; polling more often than the question changes simply means a host restarted
    /// at an awkward hour still catches the day, instead of silently skipping it until tomorrow.
    /// </summary>
    private const int CheckPeriodAsMilliseconds = 4 * 60 * 60 * 1000;

    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IRepository<Tenant> _tenantRepository;
    private readonly INotificationDispatchService _notificationDispatchService;
    private readonly IAbsenceViolationService _absenceViolationService;
    private readonly IDocumentOverdueReminderService _documentOverdueReminderService;
    private readonly IFixedDailyNotificationService _fixedDailyNotificationService;
    private readonly INotificationRuleRunner _notificationRuleRunner;

    public DailyNotificationWorker(
        AbpTimer timer,
        IUnitOfWorkManager unitOfWorkManager,
        IRepository<Tenant> tenantRepository,
        INotificationDispatchService notificationDispatchService,
        IAbsenceViolationService absenceViolationService,
        IDocumentOverdueReminderService documentOverdueReminderService,
        IFixedDailyNotificationService fixedDailyNotificationService,
        INotificationRuleRunner notificationRuleRunner)
        : base(timer)
    {
        _fixedDailyNotificationService = fixedDailyNotificationService;
        _notificationRuleRunner = notificationRuleRunner;
        _documentOverdueReminderService = documentOverdueReminderService;
        _unitOfWorkManager = unitOfWorkManager;
        _tenantRepository = tenantRepository;
        _notificationDispatchService = notificationDispatchService;
        _absenceViolationService = absenceViolationService;

        Timer.Period = CheckPeriodAsMilliseconds;

        // Not on start-up: a deployment or an app-pool recycle would otherwise mail every birthday and
        // overdue return again, and a restart loop would do it repeatedly.
        Timer.RunOnStart = false;

        LocalizationSourceName = HCMv3Consts.LocalizationSourceName;
    }

    protected override void DoRecordedWork()
    {
        var tenantIds = _unitOfWorkManager.WithUnitOfWork(() =>
            _tenantRepository.GetAll().Where(t => t.IsActive).Select(t => t.Id).ToList());

        foreach (var tenantId in tenantIds)
        {
            RunForTenant(tenantId);
        }
    }

    /// <summary>
    /// Each notification kind runs in its own unit of work, and one failing does not stop the rest. They are
    /// unrelated to each other, and a tenant with a broken leave configuration should still get its birthdays.
    /// </summary>
    private void RunForTenant(int tenantId)
    {
        // Legacy SendRealTimeNotification's order is kept for the ones it ran: document expiry first, the
        // Notification Master rules after news, the annual leave limit after the rules.
        Run(tenantId, "employee document expiry", () => _fixedDailyNotificationService.SendEmployeeDocumentExpiryNotificationsAsync(Clock.Now));
        Run(tenantId, "company document expiry", () => _fixedDailyNotificationService.SendCompanyDocumentExpiryNotificationsAsync(Clock.Now));

        Run(tenantId, "birthday", () => _notificationDispatchService.SendBirthdayNotificationsAsync());
        Run(tenantId, "anniversary", () => _notificationDispatchService.SendAnniversaryNotificationsAsync());
        Run(tenantId, "leave pay in advance", () => _notificationDispatchService.SendAdvancePaymentNotificationsAsync());
        Run(tenantId, "separation", () => _fixedDailyNotificationService.SendSeparationNotificationsAsync(Clock.Now));
        Run(tenantId, "leave resumption", () => _notificationDispatchService.SendLeaveResumptionNotificationsAsync());
        Run(tenantId, "punch during leave", () => _notificationDispatchService.SendPunchDuringLeaveNotificationsAsync());
        Run(tenantId, "probation", () => _fixedDailyNotificationService.SendProbationNotificationsAsync(Clock.Now));
        Run(tenantId, "news", () => _fixedDailyNotificationService.SendNewsNotificationsAsync(Clock.Now));

        // The Notification Master rules. Each rule runs in its own unit of work inside this one, so a broken
        // rule is logged and skipped without holding back the others.
        Run(tenantId, "notification rules", () => _notificationRuleRunner.RunAsync(Clock.Now.Date));

        Run(tenantId, "annual leave over the limit", () => _fixedDailyNotificationService.SendAnnualLeaveMaxLimitNotificationsAsync(Clock.Now));

        // Legacy SendRealTimeNotification ended with these two, in this order. Unlike the runs above they are
        // not "is today the day" checks but escalation ladders; running them every four hours is still safe,
        // because every step taken is logged against its day or leave and is never taken twice.
        Run(tenantId, "leave not returned violation", () => _absenceViolationService.SendNotReturnViolationNotificationsAsync(Clock.Now.Date));
        Run(tenantId, "absence violation", () => _absenceViolationService.SendAbsentViolationNotificationsAsync(Clock.Now.Date));

        // A document out of custody past its due date: the holder and the guarantor are told once a day.
        // Safe at this period for the same reason as the two above -- each movement is stamped with the day
        // it was last reminded.
        Run(tenantId, "document overdue", () => _documentOverdueReminderService.SendOverdueRemindersAsync(Clock.Now.Date));
    }

    private void Run(int tenantId, string what, Func<System.Threading.Tasks.Task> send)
    {
        try
        {
            _unitOfWorkManager.WithUnitOfWork(() =>
            {
                using (_unitOfWorkManager.Current.SetTenantId(tenantId))
                {
                    AsyncHelper.RunSync(send);
                }
            });
        }
        catch (Exception exception)
        {
            Logger.Error($"Could not send the {what} notifications for tenant {tenantId}: {exception.Message}", exception);
        }
    }
}
