using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Notifications;
using HCMv3.GeneralMasterInfo;
using HCMv3.Notifications;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using HCMv3.SystemSecurity.Services;

namespace HCMv3.Workflow.Services;

/// <inheritdoc cref="IWorkflowNotificationDispatcher"/>
/// <remarks>
/// Ported from legacy <c>WorkFlowServices</c>, which called its email service directly at each action with the
/// step's matching configuration. The shape is the same; what differs is that the recipient list is resolved
/// once (<see cref="INotificationRecipientResolver"/>) and used for both channels, rather than the email
/// service resolving its own and the in-app side having none.
/// </remarks>
[UnitOfWork]
public class WorkflowNotificationDispatcher : HCMv3DomainServiceBase, IWorkflowNotificationDispatcher
{
    private readonly IRepository<RequestDetail> _requestDetailRepository;
    private readonly IRepository<RequestMaster> _requestMasterRepository;
    private readonly IRepository<NotificationConfig> _notificationConfigRepository;
    private readonly IRepository<EmailTemplate> _emailTemplateRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly INotificationRecipientResolver _recipientResolver;
    private readonly IRequestMasterResolver _requestMasterResolver;
    private readonly IEmailService _emailService;
    private readonly IAppNotifier _appNotifier;

    public WorkflowNotificationDispatcher(
        IRepository<RequestDetail> requestDetailRepository,
        IRepository<RequestMaster> requestMasterRepository,
        IRepository<NotificationConfig> notificationConfigRepository,
        IRepository<EmailTemplate> emailTemplateRepository,
        IRepository<Employee> employeeRepository,
        INotificationRecipientResolver recipientResolver,
        IRequestMasterResolver requestMasterResolver,
        IEmailService emailService,
        IAppNotifier appNotifier)
    {
        _requestDetailRepository = requestDetailRepository;
        _requestMasterRepository = requestMasterRepository;
        _notificationConfigRepository = notificationConfigRepository;
        _emailTemplateRepository = emailTemplateRepository;
        _employeeRepository = employeeRepository;
        _recipientResolver = recipientResolver;
        _requestMasterResolver = requestMasterResolver;
        _emailService = emailService;
        _appNotifier = appNotifier;
    }

    public async Task<bool> DispatchAsync(
        WorkflowRequest request,
        int step,
        WorkflowNotificationAction action,
        int? actingEmployeeId = null,
        string remark = null)
    {
        if (request == null)
        {
            return false;
        }

        try
        {
            var notificationConfig = await ResolveConfigAsync(request, step, action);
            if (notificationConfig == null)
            {
                return false;
            }

            var recipients = _recipientResolver.Resolve(notificationConfig, request);
            if (recipients.Count == 0)
            {
                // The configuration was found and honoured; it simply addresses nobody today. Reported as
                // handled, so a caller's fallback does not override an administrator who routed this
                // deliberately.
                return true;
            }

            if (notificationConfig.SendNotification)
            {
                await SendInAppAsync(request, action, actingEmployeeId, remark, recipients);
            }

            if (notificationConfig.SendEmail)
            {
                await SendEmailAsync(request, notificationConfig, actingEmployeeId, remark);
            }

            return true;
        }
        catch (Exception exception)
        {
            // See the interface remarks: the approval has already happened and stands. Losing the message is
            // a nuisance; losing the decision because of the message would be a defect.
            Logger.Error(
                $"Could not send the {action} notification for workflow request {request.RequestNumber}: {exception.Message}",
                exception);

            return false;
        }
    }

    /// <summary>Which configuration the step nominates for this action, if any.</summary>
    private async Task<NotificationConfig> ResolveConfigAsync(
        WorkflowRequest request,
        int step,
        WorkflowNotificationAction action)
    {
        var requestMaster = await _requestMasterResolver.ResolveAsync(request);
        if (requestMaster == null)
        {
            return null;
        }

        var requestDetail = _requestDetailRepository.GetAll()
            .FirstOrDefault(d => d.RequestMasterId == requestMaster.Id && d.Step == step);

        if (requestDetail == null)
        {
            return null;
        }

        var configId = ConfigIdFor(requestDetail, action);

        return configId == null
            ? null
            : _notificationConfigRepository.GetAll().FirstOrDefault(c => c.Id == configId.Value);
    }

    private static int? ConfigIdFor(RequestDetail requestDetail, WorkflowNotificationAction action)
    {
        switch (action)
        {
            case WorkflowNotificationAction.Submit:
                return requestDetail.SubmitNotificationConfigId;
            case WorkflowNotificationAction.Approve:
                return requestDetail.NotificationConfigId;
            case WorkflowNotificationAction.Reject:
                return requestDetail.RejectNotificationConfigId;
            case WorkflowNotificationAction.SendBack:
                return requestDetail.SendBackNotificationConfigId;
            case WorkflowNotificationAction.Remark:
                return requestDetail.RemarksNotificationConfigId;
            case WorkflowNotificationAction.Reassign:
                return requestDetail.AssignNotificationConfigId;
            case WorkflowNotificationAction.Reminder:
                return requestDetail.ReminderNotificationConfigId;
            default:
                return null;
        }
    }

    private async Task SendInAppAsync(
        WorkflowRequest request,
        WorkflowNotificationAction action,
        int? actingEmployeeId,
        string remark,
        IEnumerable<Employee> recipients)
    {
        // Recipients are employees; notifications go to users. Somebody with no login simply cannot be
        // reached this way, which is a reason to give them one rather than to fail the send.
        var userIds = recipients
            .Where(e => e.UserId.HasValue)
            .Select(e => e.UserId.Value)
            .Distinct()
            .ToList();

        if (userIds.Count == 0)
        {
            return;
        }

        var message = BuildMessage(request, action, actingEmployeeId, remark);
        var severity = action == WorkflowNotificationAction.Reject || action == WorkflowNotificationAction.SendBack
            ? NotificationSeverity.Warn
            : NotificationSeverity.Info;

        foreach (var userId in userIds)
        {
            await _appNotifier.SendMessageAsync(
                new Abp.UserIdentifier(request.TenantId, userId),
                message,
                severity);
        }
    }

    /// <summary>
    /// The one-line summary that appears on the bell: what happened, to which request, and who did it. The
    /// request number leads because that is what the recipient will quote when they ask about it.
    /// </summary>
    private string BuildMessage(
        WorkflowRequest request,
        WorkflowNotificationAction action,
        int? actingEmployeeId,
        string remark)
    {
        var message = string.Format(
            L(LocalizationKeyFor(action)),
            request.RequestNumber,
            request.SummaryEn,
            NameOf(actingEmployeeId));

        return string.IsNullOrWhiteSpace(remark) ? message : $"{message} - {remark}";
    }

    private static string LocalizationKeyFor(WorkflowNotificationAction action)
    {
        switch (action)
        {
            case WorkflowNotificationAction.Submit:
                return "WorkflowRequestHasBeenSubmitted";
            case WorkflowNotificationAction.Approve:
                return "WorkflowRequestHasBeenApproved";
            case WorkflowNotificationAction.Reject:
                return "WorkflowRequestHasBeenRejected";
            case WorkflowNotificationAction.SendBack:
                return "WorkflowRequestHasBeenSentBack";
            case WorkflowNotificationAction.Remark:
                return "WorkflowRequestHasANewRemark";
            case WorkflowNotificationAction.Reassign:
                return "WorkflowRequestHasBeenReassigned";
            default:
                return "WorkflowRequestIsStillWaiting";
        }
    }

    private string NameOf(int? employeeId)
    {
        if (employeeId == null)
        {
            return string.Empty;
        }

        return _employeeRepository.GetAll()
            .Where(e => e.Id == employeeId.Value)
            .Select(e => e.FullNameEn)
            .FirstOrDefault() ?? string.Empty;
    }

    /// <summary>
    /// Emails the configuration's own template. A configuration with no template attached sends nothing by
    /// email, which is how an administrator switches the channel off for one action without touching the
    /// others.
    /// </summary>
    private async Task SendEmailAsync(
        WorkflowRequest request,
        NotificationConfig notificationConfig,
        int? actingEmployeeId,
        string remark)
    {
        if (notificationConfig.EmailTemplateId == null)
        {
            return;
        }

        var emailTemplate = _emailTemplateRepository.GetAll()
            .FirstOrDefault(t => t.Id == notificationConfig.EmailTemplateId.Value);

        if (emailTemplate == null)
        {
            return;
        }

        var mergeValues = new Dictionary<string, object>
        {
            ["RequestNumber"] = request.RequestNumber ?? string.Empty,
            ["RequestSummary"] = request.SummaryEn ?? string.Empty,
            ["ActionBy"] = NameOf(actingEmployeeId),
            ["Remarks"] = remark ?? string.Empty
        };

        await _emailService.SendEmailForNotificationConfigAsync(
            notificationConfig,
            emailTemplate,
            request,
            mergeValues);
    }
}
