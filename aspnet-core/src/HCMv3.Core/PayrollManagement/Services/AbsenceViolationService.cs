using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Abp;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Notifications;
using Abp.Timing;
using HCMv3.GeneralMasterInfo;
using HCMv3.Notifications;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.SystemSecurity;
using HCMv3.SystemSecurity.Services;
using HCMv3.SystemSettings.Services;
using HCMv3.TimeManagement;
using HCMv3.Workflow;

namespace HCMv3.PayrollManagement.Services;

/// <inheritdoc cref="IAbsenceViolationService"/>
/// <remarks>
/// <para><b>How the legacy shape maps here.</b></para>
/// <list type="bullet">
/// <item><description>Legacy committed per step and swallowed every error so Hangfire would not retry the job.
/// Here each absent day and each leave gets its own unit of work (<see cref="TransactionScopeOption.RequiresNew"/>,
/// as <c>BulkImportExportService</c> does per row), and an error is logged and skips only that day or leave. The
/// record created by a step (the disciplinary or the leave) is written in a unit of work of its own, and a
/// failure there leaves the step logged with no record (<c>ObjectOId</c> 0) -- which is what legacy's inner
/// try/catch did.</description></item>
/// <item><description>Legacy's separate request tables are gone. A missing-punch request is a
/// <see cref="TimeEvent"/> row and a leave application is an <see cref="EmployeeAbsence"/> row, both hidden by
/// the <c>ApprovedOnly</c> filter until approved. Legacy counted a request as an excuse whenever it was not
/// Rejected, so the excuse checks switch that filter off and exclude Rejected rows instead.</description></item>
/// <item><description>The "no punch" sentinel is <c>default(DateTime)</c> in HCMv3 (see
/// <c>PunchResolutionStrategyBase.NoPunch</c>). Legacy's 1800-01-01 is accepted too, for day rows migrated from
/// legacy as they were.</description></item>
/// <item><description>Legacy's in-app channel created a <c>TaskEvent</c> plus one <c>EmployeesNotification</c>
/// per recipient. HCMv3's in-app channel is the ABP notification bell (<see cref="IAppNotifier"/>), as
/// <c>WorkflowNotificationDispatcher</c> uses; recipients come from <see cref="INotificationRecipientResolver"/>,
/// which covers legacy's routing flags, notification groups and delegates.</description></item>
/// <item><description>Rows the job creates carry no workflow status: they are not requests, and a row with no
/// status is effective at once, the same as one HR keys in directly. Legacy created them directly too
/// (<c>EmployeeDisciplinary</c> / <c>EmployeeAbsence</c>, not their request tables) with
/// <c>IsSystemGenerated</c>, which HCMv3 does not have; the comment on the row says where it came
/// from.</description></item>
/// </list>
/// </remarks>
[UnitOfWork]
public class AbsenceViolationService : HCMv3DomainServiceBase, IAbsenceViolationService
{
    /// <summary>Legacy <c>DateTimeExtension.DBDtNull</c>; still found on day rows migrated from legacy.</summary>
    private static readonly DateTime LegacyNoPunch = new DateTime(1800, 1, 1);

    private readonly IRepository<AbsenceViolationRule> _ruleRepository;
    private readonly IRepository<AbsenceViolationRuleDetail> _ruleDetailRepository;
    private readonly IRepository<AbsenceViolationRuleException> _ruleExceptionRepository;
    private readonly IRepository<AbsenceViolationActionRecord> _actionRecordRepository;
    private readonly IRepository<AbsenceViolationActionLog> _actionLogRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<TimeRegister> _timeRegisterRepository;
    private readonly IRepository<TimeEvent> _timeEventRepository;
    private readonly IRepository<EmployeeAbsence> _absenceRepository;
    private readonly IRepository<EmployeeDisciplinary> _disciplinaryRepository;
    private readonly IRepository<AbsenceType> _absenceTypeRepository;
    private readonly IRepository<DisciplinaryType> _disciplinaryTypeRepository;
    private readonly IRepository<NotificationConfig> _notificationConfigRepository;
    private readonly IRepository<EmailTemplate> _emailTemplateRepository;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;
    private readonly IEmployeeAbsenceCalculationService _absenceCalculationService;
    private readonly ILeaveQuotaLedger _quotaLedger;
    private readonly IProbationEndDateService _probationEndDateService;
    private readonly INotificationRecipientResolver _recipientResolver;
    private readonly IAppNotifier _appNotifier;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public AbsenceViolationService(
        IRepository<AbsenceViolationRule> ruleRepository,
        IRepository<AbsenceViolationRuleDetail> ruleDetailRepository,
        IRepository<AbsenceViolationRuleException> ruleExceptionRepository,
        IRepository<AbsenceViolationActionRecord> actionRecordRepository,
        IRepository<AbsenceViolationActionLog> actionLogRepository,
        IRepository<Employee> employeeRepository,
        IRepository<TimeRegister> timeRegisterRepository,
        IRepository<TimeEvent> timeEventRepository,
        IRepository<EmployeeAbsence> absenceRepository,
        IRepository<EmployeeDisciplinary> disciplinaryRepository,
        IRepository<AbsenceType> absenceTypeRepository,
        IRepository<DisciplinaryType> disciplinaryTypeRepository,
        IRepository<NotificationConfig> notificationConfigRepository,
        IRepository<EmailTemplate> emailTemplateRepository,
        ISysSettingsAccessor sysSettingsAccessor,
        IEmployeeAbsenceCalculationService absenceCalculationService,
        ILeaveQuotaLedger quotaLedger,
        IProbationEndDateService probationEndDateService,
        INotificationRecipientResolver recipientResolver,
        IAppNotifier appNotifier,
        IEmailService emailService,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _ruleRepository = ruleRepository;
        _ruleDetailRepository = ruleDetailRepository;
        _ruleExceptionRepository = ruleExceptionRepository;
        _actionRecordRepository = actionRecordRepository;
        _actionLogRepository = actionLogRepository;
        _employeeRepository = employeeRepository;
        _timeRegisterRepository = timeRegisterRepository;
        _timeEventRepository = timeEventRepository;
        _absenceRepository = absenceRepository;
        _disciplinaryRepository = disciplinaryRepository;
        _absenceTypeRepository = absenceTypeRepository;
        _disciplinaryTypeRepository = disciplinaryTypeRepository;
        _notificationConfigRepository = notificationConfigRepository;
        _emailTemplateRepository = emailTemplateRepository;
        _sysSettingsAccessor = sysSettingsAccessor;
        _absenceCalculationService = absenceCalculationService;
        _quotaLedger = quotaLedger;
        _probationEndDateService = probationEndDateService;
        _recipientResolver = recipientResolver;
        _appNotifier = appNotifier;
        _emailService = emailService;
        _unitOfWorkManager = unitOfWorkManager;
    }

    #region Absent violation (legacy 2901-3269)

    public virtual async Task SendAbsentViolationNotificationsAsync(DateTime today)
    {
        today = today.Date;

        var tenantId = _unitOfWorkManager.Current?.GetTenantId();
        if (tenantId == null)
        {
            return;
        }

        var timeSettings = await _sysSettingsAccessor.GetTimeSettingsAsync();
        if (timeSettings?.AbsenceViolationRuleId == null)
        {
            return;
        }

        var rule = _ruleRepository.GetAll().FirstOrDefault(r => r.Id == timeSettings.AbsenceViolationRuleId.Value);
        if (rule == null)
        {
            return;
        }

        var steps = LoadSteps(rule.Id);
        var effectiveDate = timeSettings.AbsenceViolationEffectiveDate.Date;
        var yesterday = today.AddDays(-1);

        // Legacy 2913-2915: a day whose record is closed is finished with -- excused, or every step taken.
        var closedTimeRegisterIds = _actionRecordRepository.GetAll()
            .Where(r => r.ActionLogType == ActionLogType.TimeEvent && r.LogStatus == LogStatus.Closed)
            .Select(r => r.EntityObjectId)
            .ToHashSet();

        foreach (var employeeId in ActiveEmployeesOutsideExceptions(rule.Id, today))
        {
            // Legacy 2939-2959.
            var absentDays = _timeRegisterRepository.GetAll()
                .Where(r => r.EmployeeId == employeeId
                            && r.DayDate <= yesterday
                            && (r.ActualIn == default(DateTime) || r.ActualIn == LegacyNoPunch)
                            && (r.ActualOut == default(DateTime) || r.ActualOut == LegacyNoPunch)
                            && r.WeeklyOff == 0
                            && r.RestDay == 0
                            && r.PublicHoliday == 0
                            && r.LeaveDay == 0
                            && (effectiveDate == default(DateTime) || r.DayDate >= effectiveDate))
                .OrderBy(r => r.DayDate)
                .Select(r => new { r.Id, r.DayDate })
                .ToList()
                .Where(r => !closedTimeRegisterIds.Contains(r.Id))
                .ToList();

            foreach (var absentDay in absentDays)
            {
                try
                {
                    await RunInOwnUnitOfWorkAsync(tenantId.Value, () =>
                        ProcessAbsentDayAsync(tenantId.Value, rule, steps, employeeId, absentDay.Id, absentDay.DayDate.Date, today));
                }
                catch (Exception exception)
                {
                    Logger.Error(
                        $"Absence violation: could not process employee {employeeId} for {absentDay.DayDate:yyyy-MM-dd}: {exception.Message}",
                        exception);
                }
            }
        }
    }

    private async Task ProcessAbsentDayAsync(
        int tenantId,
        AbsenceViolationRule rule,
        List<AbsenceViolationRuleDetail> steps,
        int employeeId,
        int timeRegisterId,
        DateTime absentDate,
        DateTime today)
    {
        var record = _actionRecordRepository.GetAll()
            .FirstOrDefault(r => r.ActionLogType == ActionLogType.TimeEvent && r.EntityObjectId == timeRegisterId);

        // Legacy 3238-3259: an excused day is closed, whatever steps it had already taken, and nothing is sent.
        if (IsAbsentDayExcused(employeeId, absentDate))
        {
            if (record == null)
            {
                await InsertRecordAsync(tenantId, ActionLogType.TimeEvent, timeRegisterId, LogStatus.Closed);
            }
            else
            {
                record.LogStatus = LogStatus.Closed;
                await _actionRecordRepository.UpdateAsync(record);
            }

            return;
        }

        // Legacy 2994-3019: resume after the last step taken, counting from the later of the absent day and
        // the day that step was taken on.
        var nextStep = 1;
        var lastStepOnDate = absentDate;

        if (record != null)
        {
            var lastLog = _actionLogRepository.GetAll()
                .Where(l => l.AbsenceViolationActionRecordId == record.Id)
                .OrderByDescending(l => l.Step)
                .FirstOrDefault();

            if (lastLog != null)
            {
                nextStep = lastLog.Step + 1;
                lastStepOnDate = lastStepOnDate < lastLog.ActionDate ? lastLog.ActionDate.Date : lastStepOnDate;
            }
        }

        // Legacy 3021-3039: the following working days up to and including today. A day off, a holiday or a
        // leave day is not a day a step can be taken on. The day need not be unpunched.
        var followingWorkingDays = _timeRegisterRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId
                        && r.DayDate > lastStepOnDate
                        && r.DayDate <= today
                        && r.WeeklyOff == 0
                        && r.RestDay == 0
                        && r.PublicHoliday == 0
                        && r.LeaveDay == 0)
            .OrderBy(r => r.DayDate)
            .Select(r => r.DayDate)
            .ToList();

        if (followingWorkingDays.Count == 0)
        {
            return;
        }

        TimeRegister timeRegister = null;
        string employeeName = null;

        foreach (var stepDay in followingWorkingDays.Select(d => d.Date))
        {
            var step = steps.FirstOrDefault(s => s.Step == nextStep);
            if (step == null)
            {
                // No step with this number: nothing more to do, and the record stays open (legacy 3044).
                break;
            }

            // Legacy 3047: the step waits until its day interval after the absent day has passed.
            if (absentDate.AddDays(step.DayInterval) > stepDay)
            {
                continue;
            }

            // Legacy 3053-3063: hold until the employee punches on a following working day.
            if (step.HoldTillPunchFound && !HasEffectivePunch(employeeId, stepDay))
            {
                continue;
            }

            if (timeRegister == null)
            {
                timeRegister = await _timeRegisterRepository.GetAsync(timeRegisterId);
                var employee = await _employeeRepository.GetAsync(employeeId);

                // Merge tokens read the notification subject by property path (e.g. Employee.FullNameEn).
                timeRegister.Employee ??= employee;
                employeeName = employee.FullNameEn;
            }

            var comments = "Transaction created due to absence violation rule : " + rule.NameEn
                           + " on not punch in/out found on Date : " + absentDate.ToString("dd/MM/yyyy");

            // Legacy 3069-3163.
            var objectId = await PerformActionAsync(
                tenantId,
                step,
                employeeId,
                disciplinaryDate: stepDay,
                disciplinaryTime: stepDay,
                leaveStartDate: absentDate,
                leaveEndDate: absentDate,
                comments);

            // Legacy 3166-3184.
            await NotifyAsync(
                tenantId,
                step,
                timeRegister,
                mergeValues: null,
                fallbackMessage: () => L(
                    "AbsentViolationNotificationMessage",
                    employeeName,
                    absentDate.ToString("dd/MM/yyyy"),
                    rule.NameEn,
                    step.Step));

            // Legacy 3186-3225.
            record ??= await InsertRecordAsync(tenantId, ActionLogType.TimeEvent, timeRegisterId, LogStatus.Open);

            await _actionLogRepository.InsertAsync(new AbsenceViolationActionLog
            {
                TenantId = tenantId,
                AbsenceViolationActionRecordId = record.Id,
                Step = step.Step,
                Action = step.Action,
                ActionDate = stepDay,
                ObjectOId = objectId
            });

            nextStep++;

            if (steps.All(s => s.Step != nextStep))
            {
                record.LogStatus = LogStatus.Closed;
                await _actionRecordRepository.UpdateAsync(record);
                break;
            }
        }
    }

    /// <summary>
    /// Legacy 2965-2989. A punch that day, a missing-punch request, or a leave covering the day -- each counted
    /// unless it was rejected, so a request still waiting for approval excuses the day, as it did in legacy.
    ///
    /// <para>
    /// A cancellation needs no test of its own: in HCMv3 cancelling a leave is an amendment request against the
    /// leave row, and the row stays (and is counted) until the cancellation is approved and deletes it. Legacy
    /// likewise still found the leave itself while a cancellation request was pending.
    /// </para>
    /// </summary>
    private bool IsAbsentDayExcused(int employeeId, DateTime day)
    {
        var nextDay = day.AddDays(1);

        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var hasPunchOrMissingPunchRequest = _timeEventRepository.GetAll()
                .Any(t => t.EmployeeId == employeeId
                          && t.TransDate >= day
                          && t.TransDate < nextDay
                          && t.WorkflowStatus != RequestStatus.Rejected);

            if (hasPunchOrMissingPunchRequest)
            {
                return true;
            }

            return _absenceRepository.GetAll()
                .Any(a => a.EmployeeId == employeeId
                          && a.StartDate < nextDay
                          && a.EndDate >= day
                          && a.WorkflowStatus != RequestStatus.Rejected);
        }
    }

    #endregion

    #region Not-returned violation (legacy 2623-2897)

    public virtual async Task SendNotReturnViolationNotificationsAsync(DateTime today)
    {
        today = today.Date;

        var tenantId = _unitOfWorkManager.Current?.GetTenantId();
        if (tenantId == null)
        {
            return;
        }

        var absenceTypes = _absenceTypeRepository.GetAll()
            .Where(t => t.AbsenceViolationRuleId != null)
            .Select(t => new { t.Id, t.NameEn, RuleId = t.AbsenceViolationRuleId.Value })
            .ToList();

        if (absenceTypes.Count == 0)
        {
            return;
        }

        var closedLeaveIds = _actionRecordRepository.GetAll()
            .Where(r => r.ActionLogType == ActionLogType.Leave && r.LogStatus == LogStatus.Closed)
            .Select(r => r.EntityObjectId)
            .ToHashSet();

        foreach (var absenceType in absenceTypes)
        {
            var rule = _ruleRepository.GetAll().FirstOrDefault(r => r.Id == absenceType.RuleId);
            if (rule == null)
            {
                continue;
            }

            var steps = LoadSteps(rule.Id);
            var exceptedEmployeeIds = ExceptedEmployeeIds(rule.Id, today);

            // Legacy 2644-2661. The ordinary query sees only effective leave, which is what legacy's
            // EmployeeAbsence table held.
            var notReturned = _absenceRepository.GetAll()
                .Where(a => a.AbsenceTypeId == absenceType.Id
                            && a.Employee.Status == EmployeeStatus.Active
                            && !a.IsReturned
                            && a.ReturnDate == default(DateTime)
                            && a.EndDate < today)
                .Select(a => new { a.Id, a.EmployeeId, a.StartDate, a.EndDate })
                .ToList()
                .Where(a => !closedLeaveIds.Contains(a.Id) && !exceptedEmployeeIds.Contains(a.EmployeeId))
                .ToList();

            foreach (var leave in notReturned)
            {
                try
                {
                    await RunInOwnUnitOfWorkAsync(tenantId.Value, () =>
                        ProcessNotReturnedLeaveAsync(
                            tenantId.Value, rule, steps, leave.Id, leave.EmployeeId, leave.StartDate.Date,
                            leave.EndDate.Date, absenceType.NameEn, today));
                }
                catch (Exception exception)
                {
                    Logger.Error(
                        $"Absence violation: could not process leave {leave.Id} not returned from: {exception.Message}",
                        exception);
                }
            }
        }
    }

    private async Task ProcessNotReturnedLeaveAsync(
        int tenantId,
        AbsenceViolationRule rule,
        List<AbsenceViolationRuleDetail> steps,
        int absenceId,
        int employeeId,
        DateTime leaveStartDate,
        DateTime leaveEndDate,
        string absenceTypeName,
        DateTime today)
    {
        // Legacy 2665-2679: another leave asked for after this one means the employee is accounted for.
        if (HasLaterLeave(employeeId, leaveEndDate))
        {
            return;
        }

        var record = _actionRecordRepository.GetAll()
            .FirstOrDefault(r => r.ActionLogType == ActionLogType.Leave && r.EntityObjectId == absenceId);

        var nextStep = 1;
        if (record != null)
        {
            var lastStep = _actionLogRepository.GetAll()
                .Where(l => l.AbsenceViolationActionRecordId == record.Id)
                .Select(l => (int?)l.Step)
                .Max();

            nextStep = (lastStep ?? 0) + 1;
        }

        var step = steps.FirstOrDefault(s => s.Step == nextStep);
        if (step == null)
        {
            return;
        }

        // Legacy 2699-2700: one step per run, once its day interval after the leave's end has passed.
        if (leaveEndDate.AddDays(step.DayInterval) > today)
        {
            return;
        }

        // Legacy 2703-2711: hold until the employee punches today.
        if (step.HoldTillPunchFound && !HasEffectivePunch(employeeId, today))
        {
            return;
        }

        var employee = await _employeeRepository.GetAsync(employeeId);
        var leave = await _absenceRepository.GetAsync(absenceId);
        leave.Employee ??= employee;

        var comments = "Transaction created due to absence violation rule : " + rule.NameEn
                       + " on not return from leave with leave type: " + absenceTypeName
                       + ", start Date : " + leaveStartDate.ToString("dd/MM/yyyy")
                       + ", end Date : " + leaveEndDate.ToString("dd/MM/yyyy");

        var now = Clock.Now;
        var actionTime = today.Add(now.TimeOfDay);

        // Legacy 2717-2830: the disciplinary is dated today; the leave runs from the day after the leave
        // ended up to today.
        var objectId = await PerformActionAsync(
            tenantId,
            step,
            employeeId,
            disciplinaryDate: today,
            disciplinaryTime: actionTime,
            leaveStartDate: leaveEndDate.AddDays(1),
            leaveEndDate: today,
            comments);

        // Legacy 2834-2854: the one merge value legacy passed.
        var mergeValues = new Dictionary<string, object>
        {
            ["EmployeeName"] = employee.FirstNameEn ?? string.Empty
        };

        await NotifyAsync(
            tenantId,
            step,
            leave,
            mergeValues,
            fallbackMessage: () => L(
                "NotReturnedViolationNotificationMessage",
                employee.FullNameEn,
                absenceTypeName,
                leaveEndDate.ToString("dd/MM/yyyy"),
                rule.NameEn,
                step.Step));

        // Legacy 2857-2885.
        record ??= await InsertRecordAsync(tenantId, ActionLogType.Leave, absenceId, LogStatus.Open);

        await _actionLogRepository.InsertAsync(new AbsenceViolationActionLog
        {
            TenantId = tenantId,
            AbsenceViolationActionRecordId = record.Id,
            Step = step.Step,
            Action = step.Action,
            ActionDate = actionTime,
            ObjectOId = objectId
        });

        if (steps.All(s => s.Step != nextStep + 1))
        {
            record.LogStatus = LogStatus.Closed;
            await _actionRecordRepository.UpdateAsync(record);
        }
    }

    /// <summary>
    /// Legacy 2665-2676 looked for a leave or absence REQUEST starting after this leave ended, not rejected and
    /// not a cancellation. HCMv3 keeps requests and leave in one table, so this looks at every leave row
    /// (pending ones included) except rejected ones, and except leave this job booked itself: legacy booked
    /// those straight into <c>EmployeeAbsence</c>, never as a request, so they did not stop the escalation.
    /// </summary>
    private bool HasLaterLeave(int employeeId, DateTime leaveEndDate)
    {
        List<int> laterLeaveIds;
        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            laterLeaveIds = _absenceRepository.GetAll()
                .Where(a => a.EmployeeId == employeeId
                            && a.StartDate > leaveEndDate
                            && a.WorkflowStatus != RequestStatus.Rejected)
                .Select(a => a.Id)
                .ToList();
        }

        if (laterLeaveIds.Count == 0)
        {
            return false;
        }

        var bookedByThisJob = _actionLogRepository.GetAll()
            .Where(l => l.Action == AbsenceViolationAction.Leave && laterLeaveIds.Contains(l.ObjectOId))
            .Select(l => l.ObjectOId)
            .ToHashSet();

        return laterLeaveIds.Any(id => !bookedByThisJob.Contains(id));
    }

    #endregion

    #region Shared

    private List<AbsenceViolationRuleDetail> LoadSteps(int ruleId)
    {
        return _ruleDetailRepository.GetAll()
            .Where(d => d.AbsenceViolationRuleId == ruleId)
            .OrderBy(d => d.Step)
            .ToList();
    }

    /// <summary>Employees with an exception to the rule covering TODAY (legacy checks today, not the absent day).</summary>
    private HashSet<int> ExceptedEmployeeIds(int ruleId, DateTime today)
    {
        return _ruleExceptionRepository.GetAll()
            .Where(e => e.AbsenceViolationRuleId == ruleId && e.StartDate <= today && e.EndDate >= today)
            .Select(e => e.EmployeeId)
            .ToHashSet();
    }

    private List<int> ActiveEmployeesOutsideExceptions(int ruleId, DateTime today)
    {
        var excepted = ExceptedEmployeeIds(ruleId, today);

        return _employeeRepository.GetAll()
            .Where(e => e.Status == EmployeeStatus.Active)
            .Select(e => e.Id)
            .ToList()
            .Where(id => !excepted.Contains(id))
            .ToList();
    }

    /// <summary>
    /// A punch the Time Engine counts: legacy's <c>TimeEvent</c> table held only those, its requests living
    /// elsewhere, so the <c>ApprovedOnly</c> filter stays on here.
    /// </summary>
    private bool HasEffectivePunch(int employeeId, DateTime day)
    {
        var nextDay = day.AddDays(1);

        return _timeEventRepository.GetAll()
            .Any(t => t.EmployeeId == employeeId && t.TransDate >= day && t.TransDate < nextDay);
    }

    private async Task<AbsenceViolationActionRecord> InsertRecordAsync(
        int tenantId,
        ActionLogType actionLogType,
        int entityObjectId,
        LogStatus logStatus)
    {
        var record = new AbsenceViolationActionRecord
        {
            TenantId = tenantId,
            ActionLogType = actionLogType,
            EntityObjectId = entityObjectId,
            LogStatus = logStatus
        };

        record.Id = await _actionRecordRepository.InsertAndGetIdAsync(record);
        return record;
    }

    /// <summary>
    /// Takes the step's action and returns the id of what it created, or 0 when it created nothing (a
    /// notification-only step, a leave that would overlap one already there, or a step whose type is missing).
    ///
    /// <para>
    /// Written in a unit of work of its own, as legacy committed it on its own: if the record cannot be saved,
    /// that is logged and the step still counts as taken, with nothing created -- legacy's inner try/catch.
    /// </para>
    /// </summary>
    private async Task<int> PerformActionAsync(
        int tenantId,
        AbsenceViolationRuleDetail step,
        int employeeId,
        DateTime disciplinaryDate,
        DateTime disciplinaryTime,
        DateTime leaveStartDate,
        DateTime leaveEndDate,
        string comments)
    {
        if (step.Action == AbsenceViolationAction.Notification)
        {
            return 0;
        }

        var objectId = 0;

        try
        {
            await RunInOwnUnitOfWorkAsync(tenantId, async () =>
            {
                objectId = step.Action == AbsenceViolationAction.Violation
                    ? await CreateDisciplinaryAsync(tenantId, step, employeeId, disciplinaryDate, disciplinaryTime, comments)
                    : await CreateLeaveAsync(tenantId, step, employeeId, leaveStartDate, leaveEndDate, comments);
            });
        }
        catch (Exception exception)
        {
            Logger.Warn(
                $"Absence violation: step {step.Step} of rule {step.AbsenceViolationRuleId} could not create its {step.Action} for employee {employeeId}: {exception.Message}",
                exception);

            return 0;
        }

        return objectId;
    }

    /// <summary>
    /// Legacy <c>Action.Violation</c>: the employee reported against themselves, severity low. Filed directly,
    /// not routed for approval -- legacy created the <c>EmployeeDisciplinary</c> itself, not a
    /// <c>DisciplinaryRequest</c>, so the deduction tier the approval handler stamps is not applied either.
    /// </summary>
    private async Task<int> CreateDisciplinaryAsync(
        int tenantId,
        AbsenceViolationRuleDetail step,
        int employeeId,
        DateTime disciplinaryDate,
        DateTime disciplinaryTime,
        string comments)
    {
        var disciplinaryTypeId = step.ActionTypeId == null
            ? (int?)null
            : _disciplinaryTypeRepository.GetAll()
                .Where(t => t.Id == step.ActionTypeId.Value)
                .Select(t => (int?)t.Id)
                .FirstOrDefault();

        if (disciplinaryTypeId == null)
        {
            Logger.Warn($"Absence violation: step {step.Step} of rule {step.AbsenceViolationRuleId} names no violation type that exists.");
            return 0;
        }

        return await _disciplinaryRepository.InsertAndGetIdAsync(new EmployeeDisciplinary
        {
            TenantId = tenantId,
            EmployeeId = employeeId,
            ReportedById = employeeId,
            DisciplinaryDate = disciplinaryDate.Date,
            DisciplinaryTime = disciplinaryTime,
            DisciplinaryTypeId = disciplinaryTypeId.Value,
            Severity = Severity.Low,
            Comments = Truncate(comments, 500),
            WorkflowStatus = null,
            WorkflowRequestId = null
        });
    }

    /// <summary>
    /// Legacy <c>Action.Leave</c>: a leave of the step's type, unpaid through payroll
    /// (<see cref="LeavePaymentType.None"/>), only when no leave already overlaps the dates. Its days are charged
    /// to the quota as legacy's <c>EmployeeAbsence.OnSaving</c> -> <c>UpdateQuotaInformation</c> did, and an
    /// unpaid type during probation pushes the probation end date out, as the same <c>OnSaving</c> did.
    ///
    /// <para>
    /// The overlap check counts leave awaiting approval as well (not rejected), as HR's own leave screen does:
    /// booking over a pending request would make the two collide when it is approved.
    /// </para>
    /// </summary>
    private async Task<int> CreateLeaveAsync(
        int tenantId,
        AbsenceViolationRuleDetail step,
        int employeeId,
        DateTime startDate,
        DateTime endDate,
        string comments)
    {
        var absenceTypeId = step.ActionTypeId == null
            ? (int?)null
            : _absenceTypeRepository.GetAll()
                .Where(t => t.Id == step.ActionTypeId.Value)
                .Select(t => (int?)t.Id)
                .FirstOrDefault();

        if (absenceTypeId == null)
        {
            Logger.Warn($"Absence violation: step {step.Step} of rule {step.AbsenceViolationRuleId} names no leave type that exists.");
            return 0;
        }

        startDate = startDate.Date;
        endDate = endDate.Date;

        bool overlaps;
        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            overlaps = _absenceRepository.GetAll()
                .Any(a => a.EmployeeId == employeeId
                          && a.StartDate < endDate.AddDays(1)
                          && a.EndDate >= startDate
                          && a.WorkflowStatus != RequestStatus.Rejected);
        }

        if (overlaps)
        {
            return 0;
        }

        var totalLeaveDays = await _absenceCalculationService.CalculateTotalLeaveDaysAsync(
            employeeId, absenceTypeId.Value, startDate, endDate, 0);

        var leave = new EmployeeAbsence
        {
            TenantId = tenantId,
            EmployeeId = employeeId,
            AbsenceTypeId = absenceTypeId.Value,
            StartDate = startDate,
            EndDate = endDate,
            InitialEndDate = endDate,
            CalendarDays = (endDate - startDate).TotalDays + 1,
            TotalLeaveDays = totalLeaveDays,
            LeavePaymentType = LeavePaymentType.None,
            Comments = Truncate(comments, 4000),
            WorkflowStatus = null,
            WorkflowRequestId = null
        };

        var leaveId = await _absenceRepository.InsertAndGetIdAsync(leave);

        await _quotaLedger.ConsumeAsync(employeeId, absenceTypeId.Value, totalLeaveDays, startDate);
        await _probationEndDateService.RecalculateAfterLeaveChangeAsync(employeeId, absenceTypeId.Value);

        return leaveId;
    }

    /// <summary>
    /// Legacy 3166-3184 / 2834-2854, following <c>WorkflowNotificationDispatcher</c>: one recipient list for
    /// both channels. The bell carries the configuration's email template subject, merged against the day row
    /// or the leave as legacy's <c>TaskEvent.Subject</c> was, or a plain sentence when the configuration has
    /// no template. A failure to notify is logged and never undoes the step: the action has been taken.
    /// </summary>
    private async Task NotifyAsync(
        int tenantId,
        AbsenceViolationRuleDetail step,
        object subject,
        IDictionary<string, object> mergeValues,
        Func<string> fallbackMessage)
    {
        if (step.NotificationConfigId == null)
        {
            return;
        }

        try
        {
            var config = _notificationConfigRepository.GetAll()
                .FirstOrDefault(c => c.Id == step.NotificationConfigId.Value);

            if (config == null || (!config.SendNotification && !config.SendEmail))
            {
                return;
            }

            var emailTemplate = config.EmailTemplateId == null
                ? null
                : _emailTemplateRepository.GetAll().FirstOrDefault(t => t.Id == config.EmailTemplateId.Value);

            if (config.SendNotification)
            {
                var userIds = _recipientResolver.Resolve(config, subject)
                    .Where(e => e.UserId.HasValue)
                    .Select(e => e.UserId.Value)
                    .Distinct()
                    .ToList();

                if (userIds.Count > 0)
                {
                    var message = emailTemplate == null || string.IsNullOrWhiteSpace(emailTemplate.Subject)
                        ? null
                        : await _emailService.ParseTemplateAsync(emailTemplate.Subject, emailTemplate, subject, mergeValues);

                    if (string.IsNullOrWhiteSpace(message))
                    {
                        message = fallbackMessage();
                    }

                    foreach (var userId in userIds)
                    {
                        await _appNotifier.SendMessageAsync(
                            new UserIdentifier(tenantId, userId),
                            message,
                            NotificationSeverity.Warn);
                    }
                }
            }

            // A configuration with no template sends no email, as in WorkflowNotificationDispatcher.
            if (config.SendEmail && emailTemplate != null)
            {
                await _emailService.SendEmailForNotificationConfigAsync(config, emailTemplate, subject, mergeValues);
            }
        }
        catch (Exception exception)
        {
            Logger.Error(
                $"Absence violation: could not send the notification for step {step.Step} of rule {step.AbsenceViolationRuleId}: {exception.Message}",
                exception);
        }
    }

    private async Task RunInOwnUnitOfWorkAsync(int tenantId, Func<Task> action)
    {
        using (var uow = _unitOfWorkManager.Begin(TransactionScopeOption.RequiresNew))
        {
            using (_unitOfWorkManager.Current.SetTenantId(tenantId))
            {
                await action();
                await uow.CompleteAsync();
            }
        }
    }

    private static string Truncate(string value, int maxLength)
    {
        return value == null || value.Length <= maxLength ? value : value.Substring(0, maxLength);
    }

    #endregion
}
