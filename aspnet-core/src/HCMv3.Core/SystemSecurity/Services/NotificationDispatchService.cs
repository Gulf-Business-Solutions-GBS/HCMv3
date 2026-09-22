using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSettings;
using HCMv3.TimeManagement;

namespace HCMv3.SystemSecurity.Services;

/// <inheritdoc cref="INotificationDispatchService"/>
/// <remarks>
/// <see cref="UnitOfWorkAttribute"/> at the class level per this wave's established convention (see
/// <c>SysSettingsAccessor</c> remarks) so this domain service is independently callable (e.g. from a future
/// scheduled/Hangfire job, mirroring how legacy dispatched these from a background scheduler).
/// </remarks>
[UnitOfWork]
public class NotificationDispatchService : HCMv3DomainServiceBase, INotificationDispatchService
{
    public const string BirthdayNotificationName = "BIRTHDAY_NOTIFICATION";
    public const string AnniversaryNotificationName = "ANNIVERSARY_NOTIFICATION";
    public const string AdvancePaymentNotificationName = "AdvancePayment_NOTIFICATION";
    public const string LeaveResumptionNotificationName = "LEAVERESUMPTION_NOTIFICATION";
    public const string PunchDuringLeaveNotificationName = "PUNCHDURINGLEAVE_NOTIFICATION";

    /// <summary>Legacy's notification name, kept verbatim so an existing configuration still matches.</summary>
    public const string PassportGuarantorNotificationName = "PassportGuarantor_Notification";

    private readonly IRepository<SystemNotification> _systemNotificationRepository;
    private readonly IRepository<NotificationConfig> _notificationConfigRepository;
    private readonly IRepository<EmailTemplate> _emailTemplateRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeAbsence> _employeeAbsenceRepository;
    private readonly IRepository<AbsenceType> _absenceTypeRepository;
    private readonly IRepository<TimeEvent> _timeEventRepository;
    private readonly IRepository<ManageNotificationSettings> _manageNotificationSettingsRepository;
    private readonly IEmailService _emailService;

    public NotificationDispatchService(
        IRepository<SystemNotification> systemNotificationRepository,
        IRepository<NotificationConfig> notificationConfigRepository,
        IRepository<EmailTemplate> emailTemplateRepository,
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeAbsence> employeeAbsenceRepository,
        IRepository<AbsenceType> absenceTypeRepository,
        IRepository<TimeEvent> timeEventRepository,
        IRepository<ManageNotificationSettings> manageNotificationSettingsRepository,
        IEmailService emailService)
    {
        _systemNotificationRepository = systemNotificationRepository;
        _notificationConfigRepository = notificationConfigRepository;
        _emailTemplateRepository = emailTemplateRepository;
        _employeeRepository = employeeRepository;
        _employeeAbsenceRepository = employeeAbsenceRepository;
        _absenceTypeRepository = absenceTypeRepository;
        _timeEventRepository = timeEventRepository;
        _manageNotificationSettingsRepository = manageNotificationSettingsRepository;
        _emailService = emailService;
    }

    public async Task SendBirthdayNotificationsAsync()
    {
        var today = DateTime.Now.Date;

        var notificationConfig = ResolveActiveNotificationConfig(BirthdayNotificationName);
        if (notificationConfig == null || !notificationConfig.SendEmail)
        {
            // notificationConfig == null: no active SystemNotification/NotificationConfig configured.
            // !SendEmail: SendNotification (in-app) flag is a documented no-op -- see interface remarks.
            return;
        }

        var emailTemplate = ResolveEmailTemplate(BirthdayNotificationName);
        if (emailTemplate == null)
        {
            return;
        }

        var employees = _employeeRepository.GetAll()
            .Where(e => e.Status == EmployeeStatus.Active
                        && e.BirthDate.Month == today.Month
                        && e.BirthDate.Day == today.Day)
            .ToList();

        foreach (var employee in employees)
        {
            var mergeValues = new Dictionary<string, object> { ["EmployeeName"] = employee.FirstNameEn };
            await _emailService.SendEmailForNotificationConfigAsync(notificationConfig, emailTemplate, employee, mergeValues);
        }
    }

    public async Task SendAnniversaryNotificationsAsync()
    {
        var today = DateTime.Now.Date;

        var notificationConfig = ResolveActiveNotificationConfig(AnniversaryNotificationName);
        if (notificationConfig == null || !notificationConfig.SendEmail)
        {
            return;
        }

        var emailTemplate = ResolveEmailTemplate(AnniversaryNotificationName);
        if (emailTemplate == null)
        {
            return;
        }

        var employees = _employeeRepository.GetAll()
            .Where(e => e.Status == EmployeeStatus.Active && e.JoiningDate != default)
            .ToList()
            .Where(e =>
            {
                var yearsOfService = CalculateYearsOfService(e.JoiningDate, today);
                var anniversaryDate = e.JoiningDate.AddYears(yearsOfService);
                return anniversaryDate.Date == today;
            })
            .ToList();

        foreach (var employee in employees)
        {
            var mergeValues = new Dictionary<string, object> { ["EmployeeName"] = employee.FirstNameEn };
            await _emailService.SendEmailForNotificationConfigAsync(notificationConfig, emailTemplate, employee, mergeValues);
        }
    }

    /// <summary>Ported verbatim from legacy <c>CalculateYearsOfService</c> (deliberately simplistic: current
    /// year minus joining year, not a full elapsed-years calculation).</summary>
    private static int CalculateYearsOfService(DateTime startDate, DateTime endDate)
    {
        return endDate.Year - startDate.Year;
    }

    public async Task SendAdvancePaymentNotificationsAsync()
    {
        var today = DateTime.Now.Date;

        var systemNotification = _systemNotificationRepository.GetAll()
            .Where(n => n.NotificationName == AdvancePaymentNotificationName && n.IsActive)
            .Select(n => new { n.NotificationConfigId, n.ReminderDays })
            .FirstOrDefault();

        if (systemNotification == null)
        {
            return;
        }

        var notificationConfig = _notificationConfigRepository.GetAll()
            .FirstOrDefault(c => c.Id == systemNotification.NotificationConfigId);
        if (notificationConfig == null || !notificationConfig.SendEmail)
        {
            return;
        }

        var emailTemplate = ResolveEmailTemplate(AdvancePaymentNotificationName);
        if (emailTemplate == null)
        {
            return;
        }

        // Reminder-date basis (LeavePaymentDate vs LeaveStartDate) is a tenant-wide setting; take the first
        // (only) ManageNotificationSettings row, mirroring legacy's single-row settings lookup.
        var reminderDateType = _manageNotificationSettingsRepository.GetAll()
            .Select(s => (AdvancePaymentReminderDateType?)s.AdvancePaymentReminderDateType)
            .FirstOrDefault() ?? AdvancePaymentReminderDateType.LeaveStartDate;

        var payableAbsenceTypeIds = _absenceTypeRepository.GetAll()
            .Where(t => t.IsPaid)
            .Select(t => t.Id)
            .ToList();

        var reminderDays = systemNotification.ReminderDays;

        var query = _employeeAbsenceRepository.GetAll()
            .Where(a => payableAbsenceTypeIds.Contains(a.AbsenceTypeId)
                        && a.LeavePaymentType == LeavePaymentType.PayInAdvance
                        && !a.AdvanceLeavePaid);

        query = reminderDateType == AdvancePaymentReminderDateType.LeavePaymentDate
            ? query.Where(a => a.PaymentDate.Date.AddDays(-1 * reminderDays) == today)
            : query.Where(a => a.StartDate.Date.AddDays(-1 * reminderDays) == today);

        var absences = query.ToList();
        if (absences.Count == 0)
        {
            return;
        }

        var activeEmployeesById = ResolveActiveEmployeesById(absences.Select(a => a.EmployeeId));

        foreach (var absence in absences)
        {
            if (!activeEmployeesById.TryGetValue(absence.EmployeeId, out var employee))
            {
                continue;
            }

            var mergeValues = new Dictionary<string, object> { ["EmployeeName"] = employee.FirstNameEn };
            await _emailService.SendEmailForNotificationConfigAsync(notificationConfig, emailTemplate, employee, mergeValues);
        }
    }

    public async Task SendLeaveResumptionNotificationsAsync()
    {
        var today = DateTime.Now.Date;

        var notificationConfig = ResolveActiveNotificationConfig(LeaveResumptionNotificationName);
        if (notificationConfig == null || !notificationConfig.SendEmail)
        {
            return;
        }

        var emailTemplate = ResolveEmailTemplate(LeaveResumptionNotificationName);
        if (emailTemplate == null)
        {
            return;
        }

        var returnDateRequiredAbsenceTypeIds = _absenceTypeRepository.GetAll()
            .Where(t => t.IsReturnDateRequired)
            .Select(t => t.Id)
            .ToList();

        // ADAPTATION: legacy also excluded employees whose return was already recorded via an approved
        // AbsenceRequest/WorkflowRequest -- not applicable here, see interface remarks (EmployeeAbsence's
        // WfRequest FK was deliberately not ported).
        var absences = _employeeAbsenceRepository.GetAll()
            .Where(a => returnDateRequiredAbsenceTypeIds.Contains(a.AbsenceTypeId)
                        && !a.IsReturned
                        && a.ReturnDate == default
                        && a.EndDate < today)
            .ToList();

        if (absences.Count == 0)
        {
            return;
        }

        var activeEmployeesById = ResolveActiveEmployeesById(absences.Select(a => a.EmployeeId));

        foreach (var absence in absences)
        {
            if (!activeEmployeesById.TryGetValue(absence.EmployeeId, out var employee))
            {
                continue;
            }

            var mergeValues = new Dictionary<string, object> { ["EmployeeName"] = employee.FirstNameEn };
            await _emailService.SendEmailForNotificationConfigAsync(notificationConfig, emailTemplate, employee, mergeValues);
        }
    }

    public async Task SendPunchDuringLeaveNotificationsAsync()
    {
        var today = DateTime.Now.Date;

        var notificationConfig = ResolveActiveNotificationConfig(PunchDuringLeaveNotificationName);
        if (notificationConfig == null || !notificationConfig.SendEmail)
        {
            return;
        }

        var emailTemplate = ResolveEmailTemplate(PunchDuringLeaveNotificationName);
        if (emailTemplate == null)
        {
            return;
        }

        var returnDateRequiredAbsenceTypeIds = _absenceTypeRepository.GetAll()
            .Where(t => t.IsReturnDateRequired)
            .Select(t => t.Id)
            .ToList();

        var absences = _employeeAbsenceRepository.GetAll()
            .Where(a => returnDateRequiredAbsenceTypeIds.Contains(a.AbsenceTypeId)
                        && !a.IsReturned
                        && a.ReturnDate == default
                        && a.EndDate >= today)
            .ToList();

        if (absences.Count == 0)
        {
            return;
        }

        var activeEmployeesById = ResolveActiveEmployeesById(absences.Select(a => a.EmployeeId));

        foreach (var absence in absences)
        {
            if (!activeEmployeesById.TryGetValue(absence.EmployeeId, out var employee))
            {
                continue;
            }

            var timeEvent = _timeEventRepository.GetAll()
                .Where(t => t.EmployeeId == absence.EmployeeId
                            && t.TransDate.Date <= absence.EndDate.Date
                            && t.TransDate.Date > absence.StartDate.Date)
                .FirstOrDefault();

            if (timeEvent == null)
            {
                continue;
            }

            var mergeValues = new Dictionary<string, object> { ["TransDate"] = timeEvent.TransDate.ToString("dd/MM/yyyy") };
            await _emailService.SendEmailForNotificationConfigAsync(notificationConfig, emailTemplate, employee, mergeValues);
        }
    }

    public async Task SendPassportGuarantorNotificationAsync(int guarantorId, int employeeId, string requestNumber)
    {
        var notificationConfig = ResolveActiveNotificationConfig(PassportGuarantorNotificationName);
        if (notificationConfig == null || !notificationConfig.SendEmail)
        {
            return;
        }

        var emailTemplate = ResolveEmailTemplate(PassportGuarantorNotificationName);
        if (emailTemplate == null)
        {
            return;
        }

        var people = ResolveActiveEmployeesById(new[] { guarantorId, employeeId });
        if (!people.TryGetValue(guarantorId, out var guarantor) || !people.TryGetValue(employeeId, out var employee))
        {
            return;
        }

        // Addressed to the GUARANTOR, who is being asked to stand behind somebody else's request, so the
        // config's routing resolves against them; the requester's name travels as a merge value, exactly as
        // legacy's two-value list did.
        var mergeValues = new Dictionary<string, object>
        {
            ["EmpCodeWithFullNameEn"] = $"{employee.Code} - {employee.FullNameEn}",
            ["GuarantorNameEn"] = guarantor.FirstNameEn,
            ["RequestNumber"] = requestNumber ?? string.Empty
        };

        await _emailService.SendEmailForNotificationConfigAsync(notificationConfig, emailTemplate, guarantor, mergeValues);
    }

    private Dictionary<int, Employee> ResolveActiveEmployeesById(IEnumerable<int> employeeIds)
    {
        var distinctIds = employeeIds.Distinct().ToList();
        return _employeeRepository.GetAll()
            .Where(e => distinctIds.Contains(e.Id) && e.Status == EmployeeStatus.Active)
            .ToList()
            .ToDictionary(e => e.Id);
    }

    private NotificationConfig ResolveActiveNotificationConfig(string notificationName)
    {
        var systemNotification = _systemNotificationRepository.GetAll()
            .Where(n => n.NotificationName == notificationName && n.IsActive)
            .Select(n => new { n.NotificationConfigId })
            .FirstOrDefault();

        return systemNotification == null
            ? null
            : _notificationConfigRepository.GetAll().FirstOrDefault(c => c.Id == systemNotification.NotificationConfigId);
    }

    private EmailTemplate ResolveEmailTemplate(string code)
    {
        return _emailTemplateRepository.GetAll().FirstOrDefault(t => t.Code == code);
    }
}
