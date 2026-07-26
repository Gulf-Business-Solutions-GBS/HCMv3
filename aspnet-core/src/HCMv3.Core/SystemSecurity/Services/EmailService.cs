using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Net.Mail;
using Abp.Runtime.Session;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSettings.Services;
using HCMv3.Workflow;

namespace HCMv3.SystemSecurity.Services;

/// <inheritdoc cref="IEmailService"/>
/// <remarks>
/// <see cref="UnitOfWorkAttribute"/> at the class level per this wave's established convention (see
/// <c>SysSettingsAccessor</c> remarks) so this domain service is independently callable.
/// </remarks>
[UnitOfWork]
public class EmailService : HCMv3DomainServiceBase, IEmailService
{
    private readonly IRepository<EmailToken> _emailTokenRepository;
    private readonly IRepository<EmailNotificationLog> _emailNotificationLogRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeDelegation> _employeeDelegationRepository;
    private readonly IRepository<EmployeeManager> _employeeManagerRepository;
    private readonly IRepository<RequestLog> _requestLogRepository;
    private readonly IEmailSender _emailSender;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;
    private readonly IAbpSession _abpSession;

    public EmailService(
        IRepository<EmailToken> emailTokenRepository,
        IRepository<EmailNotificationLog> emailNotificationLogRepository,
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeDelegation> employeeDelegationRepository,
        IRepository<EmployeeManager> employeeManagerRepository,
        IRepository<RequestLog> requestLogRepository,
        IEmailSender emailSender,
        ISysSettingsAccessor sysSettingsAccessor,
        IAbpSession abpSession)
    {
        _emailTokenRepository = emailTokenRepository;
        _emailNotificationLogRepository = emailNotificationLogRepository;
        _employeeRepository = employeeRepository;
        _employeeDelegationRepository = employeeDelegationRepository;
        _employeeManagerRepository = employeeManagerRepository;
        _requestLogRepository = requestLogRepository;
        _emailSender = emailSender;
        _sysSettingsAccessor = sysSettingsAccessor;
        _abpSession = abpSession;
    }

    public async Task<EmailResponseViewModel> SendEmailAsync(
        EmailTemplate emailTemplate,
        Employee recipient,
        object obj,
        IDictionary<string, object> mergeValues = null,
        Attachment attachment = null)
    {
        var model = new EmailResponseViewModel();

        if (recipient == null || string.IsNullOrEmpty(recipient.Email) || emailTemplate == null)
        {
            model.Status = false;
            model.Response = "Email Not Found!";
            return model;
        }

        model.EmailTo = recipient.Email;

        try
        {
            var selfServiceSettings = await _sysSettingsAccessor.GetSelfServiceSettingsAsync();
            if (selfServiceSettings != null && !selfServiceSettings.EmailNotification)
            {
                model.Status = false;
                model.Response = "Server email is inactive";
                await LogAsync(model, "System Notification", isSuccessful: false, exception: model.Response);
                return model;
            }

            var subject = await ParseTemplateAsync(emailTemplate.Subject, emailTemplate, obj, mergeValues);
            if (string.IsNullOrEmpty(subject))
            {
                model.Status = false;
                model.Response = "Email subject is not mentioned!";
                return model;
            }

            var bodyHtml = emailTemplate.EmailBodyHtml ?? string.Empty;
            var bodyText = emailTemplate.EmailBodyText ?? string.Empty;

            if (bodyHtml.Contains("$#RecipientName#$"))
            {
                bodyHtml = bodyHtml.Replace("$#RecipientName#$", recipient.FullNameEn);
            }

            if (bodyText.Contains("$#RecipientName#$"))
            {
                bodyText = bodyText.Replace("$#RecipientName#$", recipient.FullNameEn);
            }

            var body = !string.IsNullOrEmpty(bodyHtml)
                ? await ParseTemplateAsync(bodyHtml, emailTemplate, obj, mergeValues)
                : await ParseTemplateAsync(bodyText, emailTemplate, obj, mergeValues);

            var mailMessage = new MailMessage
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = !string.IsNullOrEmpty(bodyHtml)
            };
            mailMessage.To.Add(recipient.Email);

            if (attachment != null)
            {
                mailMessage.Attachments.Add(attachment);
            }

            await _emailSender.SendAsync(mailMessage);

            model.Status = true;
            model.Response = "Email sent successfully!";
            model.EmailSubject = subject;
            model.EmailBody = body;

            await LogAsync(model, "System Notification", isSuccessful: true, exception: null);
        }
        catch (Exception ex)
        {
            model.Status = false;
            model.Response = "Error Sending Email! Contact Administrator";
            await LogAsync(model, "System Notification", isSuccessful: false, exception: ex.Message);
        }

        return model;
    }

    public async Task<EmailResponseViewModel> SendEmailToAddressAsync(
        EmailTemplate emailTemplate,
        string emailAddress,
        object obj,
        IDictionary<string, object> mergeValues = null)
    {
        var model = new EmailResponseViewModel { EmailTo = emailAddress };

        if (string.IsNullOrEmpty(emailAddress) || emailTemplate == null)
        {
            model.Status = false;
            model.Response = "Email Not Found!";
            return model;
        }

        try
        {
            var selfServiceSettings = await _sysSettingsAccessor.GetSelfServiceSettingsAsync();
            if (selfServiceSettings != null && !selfServiceSettings.EmailNotification)
            {
                model.Status = false;
                model.Response = "Server email is inactive";
                await LogAsync(model, "Recruiting", isSuccessful: false, exception: model.Response);
                return model;
            }

            var subject = await ParseTemplateAsync(emailTemplate.Subject, emailTemplate, obj, mergeValues);
            if (string.IsNullOrEmpty(subject))
            {
                model.Status = false;
                model.Response = "Email subject is not mentioned!";
                return model;
            }

            var body = await ParseTemplateAsync(emailTemplate.EmailBodyText ?? emailTemplate.EmailBodyHtml, emailTemplate, obj, mergeValues);

            var mailMessage = new MailMessage
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = string.IsNullOrEmpty(emailTemplate.EmailBodyText)
            };
            mailMessage.To.Add(emailAddress);

            await _emailSender.SendAsync(mailMessage);

            model.Status = true;
            model.Response = "Email sent successfully!";
            model.EmailSubject = subject;
            model.EmailBody = body;

            await LogAsync(model, "Recruiting", isSuccessful: true, exception: null);
        }
        catch (Exception ex)
        {
            model.Status = false;
            model.Response = "Error Sending Email! Contact Administrator";
            await LogAsync(model, "Recruiting", isSuccessful: false, exception: ex.Message);
        }

        return model;
    }

    public async Task<string> ParseTemplateAsync(
        string content,
        EmailTemplate emailTemplate,
        object obj,
        IDictionary<string, object> mergeValues = null)
    {
        if (string.IsNullOrEmpty(content) || emailTemplate == null)
        {
            return content;
        }

        var dateFormat = await _sysSettingsAccessor.GetSysDateFormatAsync();
        var timeFormat = await _sysSettingsAccessor.GetSysTimeFormatAsync();

        var tokens = _emailTokenRepository.GetAll()
            .Where(t => t.EmailTemplateId == emailTemplate.Id)
            .ToList();

        foreach (var token in tokens)
        {
            try
            {
                object value = null;

                if (token.ObjectKey && mergeValues != null &&
                    mergeValues.TryGetValue(token.TokenValue, out var mergeValue))
                {
                    value = mergeValue;
                }
                else if (obj != null && !string.IsNullOrEmpty(token.TokenValue))
                {
                    value = GetPropertyValue(obj, token.TokenValue);
                }

                string replacement;
                if (value != null)
                {
                    if (token.IsDateTimeType)
                    {
                        var dateTimeValue = Convert.ToDateTime(value);
                        replacement = token.TokenName.Contains("Time")
                            ? dateTimeValue.ToString(timeFormat)
                            : dateTimeValue.ToString(dateFormat);
                    }
                    else
                    {
                        replacement = value.ToString();
                    }
                }
                else
                {
                    replacement = string.Empty;
                }

                content = content.Replace("$#" + token.TokenName + "#$", replacement);
            }
            catch
            {
                content = content.Replace("$#" + token.TokenName + "#$", string.Empty);
            }
        }

        return content;
    }

    public object GetPropertyValue(object src, string propName)
    {
        if (src == null || string.IsNullOrEmpty(propName))
        {
            return null;
        }

        try
        {
            if (propName.Contains("."))
            {
                var parts = propName.Split(new[] { '.' }, 2);
                return GetPropertyValue(GetPropertyValue(src, parts[0]), parts[1]);
            }

            var property = src.GetType().GetProperty(propName);
            return property?.GetValue(src, null);
        }
        catch
        {
            return null;
        }
    }

    public async Task SendEmailForNotificationConfigAsync(
        NotificationConfig notificationConfig,
        EmailTemplate emailTemplate,
        object obj,
        IDictionary<string, object> mergeValues = null)
    {
        if (notificationConfig == null || emailTemplate == null)
        {
            return;
        }

        var sentEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var employee = ResolveEmployee(obj);

        if (notificationConfig.ToEmployee && employee != null && employee.Status != EmployeeStatus.Inactive)
        {
            await SendWithDelegateAsync(employee, emailTemplate, obj, mergeValues, sentEmails);
        }

        if (notificationConfig.ToRequestCreator)
        {
            var creator = ResolveRequestCreator(obj);
            if (creator != null && creator.Status != EmployeeStatus.Inactive && sentEmails.Add(creator.Email ?? string.Empty))
            {
                await SendEmailAsync(emailTemplate, creator, obj, mergeValues);
            }
        }

        if (notificationConfig.ToManager && employee != null && employee.Status != EmployeeStatus.Inactive)
        {
            var manager = ResolveCurrentManager(employee.Id);
            if (manager != null && manager.Status == EmployeeStatus.Active)
            {
                await SendWithDelegateAsync(manager, emailTemplate, obj, mergeValues, sentEmails);
            }
        }

        if (notificationConfig.SendToAllApproved && obj is WorkflowRequest workflowRequest)
        {
            var approverIds = _requestLogRepository.GetAll()
                .Where(r => r.WorkflowRequestId == workflowRequest.Id && r.ActionType == RequestActionType.Approved
                            && r.ActionById != null)
                .Select(r => r.ActionById.Value)
                .Distinct()
                .ToList();

            var approvers = _employeeRepository.GetAll().Where(e => approverIds.Contains(e.Id)).ToList();

            foreach (var approver in approvers.Where(a => a != null && a.Status != EmployeeStatus.Inactive))
            {
                await SendWithDelegateAsync(approver, emailTemplate, obj, mergeValues, sentEmails);
            }
        }

        // SKIPPED: legacy NotificationGroup -> EmployeeWorkflowRole fan-out -- see IEmailService remarks.

        if (notificationConfig.SendToAll)
        {
            var activeEmployees = _employeeRepository.GetAll()
                .Where(e => e.Status == EmployeeStatus.Active)
                .ToList();

            foreach (var activeEmployee in activeEmployees)
            {
                if (sentEmails.Add(activeEmployee.Email ?? string.Empty))
                {
                    await SendEmailAsync(emailTemplate, activeEmployee, obj, mergeValues);
                }
            }
        }
    }

    public async Task SendEmailForNotificationConfigAsync(
        NotificationConfig notificationConfig,
        EmailTemplate emailTemplate,
        object obj,
        string employeeCodeList,
        IDictionary<string, object> mergeValues = null)
    {
        if (notificationConfig == null || emailTemplate == null || string.IsNullOrWhiteSpace(employeeCodeList))
        {
            return;
        }

        var sentEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var codes = employeeCodeList.Split(',').Select(c => c.Trim()).Where(c => c.Length > 0).ToList();

        var employees = _employeeRepository.GetAll()
            .Where(e => codes.Contains(e.Code) && e.Status == EmployeeStatus.Active)
            .ToList();

        foreach (var employee in employees)
        {
            await SendWithDelegateAsync(employee, emailTemplate, obj, mergeValues, sentEmails);
        }
    }

    public async Task<EmailResponseViewModel> SendPunchEmailWithResponseAsync(
        EmailTemplate emailTemplate,
        Employee recipient,
        string emailBody,
        bool forEmployee,
        object obj = null,
        IDictionary<string, object> mergeValues = null)
    {
        var (model, _) = await SendPunchEmailCoreAsync(emailTemplate, recipient, emailBody, forEmployee, obj, mergeValues);
        return model;
    }

    /// <summary>
    /// Same as <see cref="SendPunchEmailWithResponseAsync"/> but also returns the (already persisted)
    /// <see cref="EmailNotificationLog"/> row for the attempt, for callers (like <see cref="SendPunchEmailAsync"/>)
    /// that need to collect the log entities without inserting a duplicate row.
    /// </summary>
    private async Task<(EmailResponseViewModel Model, EmailNotificationLog Log)> SendPunchEmailCoreAsync(
        EmailTemplate emailTemplate,
        Employee recipient,
        string emailBody,
        bool forEmployee,
        object obj,
        IDictionary<string, object> mergeValues)
    {
        var model = new EmailResponseViewModel();

        if (recipient == null || string.IsNullOrEmpty(recipient.Email) || emailTemplate == null)
        {
            model.Status = false;
            model.Response = "Invalid Email!";
            return (model, await LogAsync(model, "Punch Notification", isSuccessful: false, exception: model.Response));
        }

        model.EmailTo = recipient.Email;

        try
        {
            var selfServiceSettings = await _sysSettingsAccessor.GetSelfServiceSettingsAsync();
            if (selfServiceSettings != null && !selfServiceSettings.EmailNotification)
            {
                model.Status = false;
                model.Response = "EMAIL_NOTIFICATION is disabled in Configuration.";
                var disabledLog = await LogAsync(model, "Punch Notification", isSuccessful: false, exception: model.Response);
                return (model, disabledLog);
            }

            string subject;
            string body;
            bool isBodyHtml;

            if (forEmployee)
            {
                subject = await ParseTemplateAsync(emailTemplate.Subject, emailTemplate, obj, mergeValues);
                var bodyHtml = emailTemplate.EmailBodyHtml ?? string.Empty;
                if (bodyHtml.Contains("$#RecipientName#$"))
                {
                    bodyHtml = bodyHtml.Replace("$#RecipientName#$", recipient.FullNameEn);
                }

                body = await ParseTemplateAsync(bodyHtml, emailTemplate, obj, mergeValues);
                isBodyHtml = true;
            }
            else
            {
                subject = await ParseTemplateAsync(emailTemplate.Subject, emailTemplate, null, null);
                body = emailBody;
                isBodyHtml = true;
            }

            if (string.IsNullOrEmpty(subject))
            {
                model.Status = false;
                model.Response = "Email subject is not mentioned!";
                return (model, await LogAsync(model, "Punch Notification", isSuccessful: false, exception: model.Response));
            }

            var mailMessage = new MailMessage
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = isBodyHtml
            };
            mailMessage.To.Add(recipient.Email);

            await _emailSender.SendAsync(mailMessage);

            model.Status = true;
            model.Response = "Email Sent Successfully!";
            model.EmailSubject = subject;
            model.EmailBody = body;

            var log = await LogAsync(model, "Punch Notification", isSuccessful: true, exception: null);
            return (model, log);
        }
        catch (Exception ex)
        {
            model.Status = false;
            model.Response = ex.Message;
            var log = await LogAsync(model, "Punch Notification", isSuccessful: false, exception: ex.Message);
            return (model, log);
        }
    }

    private async Task<(EmailResponseViewModel Model, EmailNotificationLog Log)> SendPunchEmailWithResponseAndLogAsync(
        EmailTemplate emailTemplate,
        Employee recipient,
        string emailBody,
        object obj)
    {
        return await SendPunchEmailCoreAsync(emailTemplate, recipient, emailBody, forEmployee: false, obj, mergeValues: null);
    }

    public async Task<List<EmailNotificationLog>> SendPunchEmailAsync(
        NotificationConfig notificationConfig,
        EmailTemplate emailTemplate,
        object obj,
        string emailBody)
    {
        var logs = new List<EmailNotificationLog>();

        if (notificationConfig == null || emailTemplate == null || string.IsNullOrEmpty(emailBody))
        {
            return logs;
        }

        var employee = ResolveEmployee(obj);

        async Task SendAndLogAsync(Employee recipient)
        {
            if (recipient == null)
            {
                return;
            }

            var delegateTo = ResolveActiveDelegate(recipient.Id);
            var target = delegateTo ?? recipient;

            // NOTE: SendPunchEmailWithResponseAsync already persists its own EmailNotificationLog row
            // (see LogAsync calls within it) -- the entity appended to the returned list here is that
            // very persisted row, not a second insert.
            var (model, log) = await SendPunchEmailWithResponseAndLogAsync(emailTemplate, target, emailBody, obj);
            logs.Add(log);
        }

        if (notificationConfig.ToEmployee && employee != null && employee.Status != EmployeeStatus.Inactive)
        {
            await SendAndLogAsync(employee);
        }

        if (notificationConfig.ToRequestCreator)
        {
            var creator = ResolveRequestCreator(obj);
            if (creator != null && creator.Status != EmployeeStatus.Inactive)
            {
                await SendAndLogAsync(creator);
            }
        }

        if (notificationConfig.ToManager && employee != null && employee.Status != EmployeeStatus.Inactive)
        {
            var manager = ResolveCurrentManager(employee.Id);
            if (manager != null && manager.Status == EmployeeStatus.Active)
            {
                await SendAndLogAsync(manager);
            }
        }

        if (notificationConfig.SendToAllApproved && obj is WorkflowRequest workflowRequest)
        {
            var approverIds = _requestLogRepository.GetAll()
                .Where(r => r.WorkflowRequestId == workflowRequest.Id && r.ActionType == RequestActionType.Approved
                            && r.ActionById != null)
                .Select(r => r.ActionById.Value)
                .Distinct()
                .ToList();

            var approvers = _employeeRepository.GetAll().Where(e => approverIds.Contains(e.Id)).ToList();

            foreach (var approver in approvers.Where(a => a != null && a.Status != EmployeeStatus.Inactive))
            {
                await SendAndLogAsync(approver);
            }
        }

        if (notificationConfig.SendToAll)
        {
            var activeEmployees = _employeeRepository.GetAll()
                .Where(e => e.Status == EmployeeStatus.Active)
                .ToList();

            foreach (var activeEmployee in activeEmployees)
            {
                await SendAndLogAsync(activeEmployee);
            }
        }

        return logs;
    }

    private async Task SendWithDelegateAsync(
        Employee employee,
        EmailTemplate emailTemplate,
        object obj,
        IDictionary<string, object> mergeValues,
        HashSet<string> sentEmails)
    {
        var delegateTo = ResolveActiveDelegate(employee.Id);
        if (delegateTo != null && sentEmails.Add(delegateTo.Email ?? string.Empty))
        {
            await SendEmailAsync(emailTemplate, delegateTo, obj, mergeValues);
        }

        if (sentEmails.Add(employee.Email ?? string.Empty))
        {
            await SendEmailAsync(emailTemplate, employee, obj, mergeValues);
        }
    }

    // NOTE: navigation properties (EmployeeDelegation.DelegateTo / EmployeeManager.Manager) are resolved via
    // a second flat-FK repository lookup rather than the navigation property itself: this Core-layer domain
    // service has no EF Core "Include()" available (see SysSettingsAccessor remarks), and without eager
    // loading or an already-tracked instance in the same DbContext, the navigation would come back null.

    private Employee ResolveActiveDelegate(int employeeId)
    {
        var today = DateTime.Now.Date;
        var delegateToId = _employeeDelegationRepository.GetAll()
            .Where(d => d.EmployeeId == employeeId && d.StartDate <= today && d.EndDate >= today)
            .Select(d => (int?)d.DelegateToId)
            .FirstOrDefault();

        return delegateToId == null ? null : _employeeRepository.GetAll().FirstOrDefault(e => e.Id == delegateToId);
    }

    private Employee ResolveCurrentManager(int employeeId)
    {
        var today = DateTime.Now.Date;
        var managerId = _employeeManagerRepository.GetAll()
            .Where(m => m.EmployeeId == employeeId && m.StartDate.Date <= today && m.EndDate.Date >= today)
            .Select(m => (int?)m.ManagerId)
            .FirstOrDefault();

        return managerId == null ? null : _employeeRepository.GetAll().FirstOrDefault(e => e.Id == managerId);
    }

    private static Employee ResolveEmployee(object obj)
    {
        return obj as Employee;
    }

    private Employee ResolveRequestCreator(object obj)
    {
        var sysCreatorValue = GetPropertyValue(obj, "SysCreator");
        if (sysCreatorValue == null)
        {
            return null;
        }

        if (!int.TryParse(sysCreatorValue.ToString(), out var sysCreatorId) || sysCreatorId == 0)
        {
            return null;
        }

        return _employeeRepository.GetAll().FirstOrDefault(e => e.Id == sysCreatorId);
    }

    private async Task<EmailNotificationLog> LogAsync(EmailResponseViewModel model, string source, bool isSuccessful, string exception)
    {
        var log = new EmailNotificationLog
        {
            TenantId = _abpSession.GetTenantId(),
            Date = DateTime.Now,
            Email = model.EmailTo,
            Source = source,
            Subject = model.EmailSubject,
            Body = model.EmailBody,
            IsSuccessful = isSuccessful,
            Exception = exception
        };

        return await _emailNotificationLogRepository.InsertAsync(log);
    }
}
