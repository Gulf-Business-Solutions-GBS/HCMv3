using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp;
using Abp.Domain.Repositories;
using Abp.Notifications;
using HCMv3.GeneralMasterInfo;
using HCMv3.Notifications;
using HCMv3.SystemSecurity;
using HCMv3.SystemSecurity.Services;

namespace HCMv3.PayrollManagement.Services;

/// <inheritdoc cref="IPayrollPeriodNotificationService"/>
/// <remarks>
/// Legacy parity, kept as legacy behaved (legacy <c>PayrollControlRecordController.cs</c> 142-176, 191-225,
/// 240-275):
/// <list type="bullet">
/// <item><description>The whole send sits inside <c>foreach (var notifyGroup in notificationConfig.NotificationGroup)</c>,
/// and the loop variable is never used. So a configuration with NO notification group sends nothing at all, and
/// one with two groups sends everything twice.</description></item>
/// <item><description>Inside it, legacy sends once per control record whose payroll area has the same NAME as
/// this one -- normally just this record, since there is one record per area.</description></item>
/// <item><description>The object the message is about is the control record itself, not an employee, so
/// "to employee" / "to manager" reach nobody; the groups, "send to all" and "to request creator" do the work.
/// Recipients are resolved by <see cref="INotificationRecipientResolver"/>, as for every other notification.</description></item>
/// <item><description>Email goes only when <c>SendEmail</c> is ticked and the configuration has a template; the
/// in-app notification only when <c>SendNotification</c> is ticked. The one merge value legacy supplied was an
/// empty <c>EmployeeName</c>.</description></item>
/// </list>
/// </remarks>
public class PayrollPeriodNotificationService : HCMv3DomainServiceBase, IPayrollPeriodNotificationService
{
    private readonly IRepository<PayrollArea> _payrollAreaRepository;
    private readonly IRepository<PayrollControlRecord> _controlRecordRepository;
    private readonly IRepository<NotificationConfig> _notificationConfigRepository;
    private readonly IRepository<NotificationGroup> _notificationGroupRepository;
    private readonly IRepository<EmailTemplate> _emailTemplateRepository;
    private readonly INotificationRecipientResolver _recipientResolver;
    private readonly IEmailService _emailService;
    private readonly IAppNotifier _appNotifier;

    public PayrollPeriodNotificationService(
        IRepository<PayrollArea> payrollAreaRepository,
        IRepository<PayrollControlRecord> controlRecordRepository,
        IRepository<NotificationConfig> notificationConfigRepository,
        IRepository<NotificationGroup> notificationGroupRepository,
        IRepository<EmailTemplate> emailTemplateRepository,
        INotificationRecipientResolver recipientResolver,
        IEmailService emailService,
        IAppNotifier appNotifier)
    {
        _payrollAreaRepository = payrollAreaRepository;
        _controlRecordRepository = controlRecordRepository;
        _notificationConfigRepository = notificationConfigRepository;
        _notificationGroupRepository = notificationGroupRepository;
        _emailTemplateRepository = emailTemplateRepository;
        _recipientResolver = recipientResolver;
        _emailService = emailService;
        _appNotifier = appNotifier;
    }

    public virtual async Task NotifyStateChangedAsync(PayrollControlRecord record, PayrollPeriodState state)
    {
        if (record == null)
        {
            return;
        }

        try
        {
            var area = _payrollAreaRepository.GetAll().FirstOrDefault(a => a.Id == record.PayrollAreaId);
            if (area == null)
            {
                return;
            }

            var configId = ConfigIdFor(area, state);
            if (configId == null)
            {
                return;
            }

            var config = _notificationConfigRepository.GetAll().FirstOrDefault(c => c.Id == configId.Value);
            if (config == null)
            {
                return;
            }

            // Legacy's outer loop: once per notification group on the configuration -- none means no send.
            var groupCount = _notificationGroupRepository.GetAll().Count(g => g.NotificationConfigId == config.Id);
            if (groupCount == 0)
            {
                return;
            }

            var areaName = area.Name;
            var sameNamedRecords = _controlRecordRepository.GetAll()
                .Where(r => r.PayrollArea.Name == areaName)
                .ToList();

            var emailTemplate = config.EmailTemplateId == null
                ? null
                : _emailTemplateRepository.GetAll().FirstOrDefault(t => t.Id == config.EmailTemplateId.Value);

            var mergeValues = new Dictionary<string, object> { ["EmployeeName"] = string.Empty };

            for (var group = 0; group < groupCount; group++)
            {
                foreach (var controlRecord in sameNamedRecords)
                {
                    if (config.SendEmail && emailTemplate != null)
                    {
                        await _emailService.SendEmailForNotificationConfigAsync(config, emailTemplate, controlRecord, mergeValues);
                    }

                    if (config.SendNotification)
                    {
                        await SendInAppAsync(config, emailTemplate, controlRecord, areaName, state, mergeValues);
                    }
                }
            }
        }
        catch (Exception exception)
        {
            // The period has moved and stays moved; losing the message must not undo that.
            Logger.Error(
                $"Payroll period: could not send the {state} notification for payroll area {record.PayrollAreaId}: {exception.Message}",
                exception);
        }
    }

    private static int? ConfigIdFor(PayrollArea area, PayrollPeriodState state)
    {
        switch (state)
        {
            case PayrollPeriodState.ReleasedForPayroll:
                return area.PAReleaseNotificationId;
            case PayrollPeriodState.Closed:
                return area.PAExitNotificationId;
            case PayrollPeriodState.ReleasedForCorrection:
                return area.PACorrectionNotificationId;
            default:
                return null;
        }
    }

    /// <summary>
    /// The bell carries the template subject merged against the control record, as legacy's <c>TaskEvent.Subject</c>
    /// did, or a plain sentence when the configuration has no template. People without a login cannot be reached
    /// this way.
    /// </summary>
    private async Task SendInAppAsync(
        NotificationConfig config,
        EmailTemplate emailTemplate,
        PayrollControlRecord controlRecord,
        string areaName,
        PayrollPeriodState state,
        IDictionary<string, object> mergeValues)
    {
        var userIds = _recipientResolver.Resolve(config, controlRecord)
            .Where(e => e.UserId.HasValue)
            .Select(e => e.UserId.Value)
            .Distinct()
            .ToList();

        if (userIds.Count == 0)
        {
            return;
        }

        var message = emailTemplate == null || string.IsNullOrWhiteSpace(emailTemplate.Subject)
            ? null
            : await _emailService.ParseTemplateAsync(emailTemplate.Subject, emailTemplate, controlRecord, mergeValues);

        if (string.IsNullOrWhiteSpace(message))
        {
            message = L(FallbackMessageKey(state), areaName, controlRecord.PayrollMonth, controlRecord.PayrollYear);
        }

        foreach (var userId in userIds)
        {
            await _appNotifier.SendMessageAsync(
                new UserIdentifier(controlRecord.TenantId, userId),
                message,
                NotificationSeverity.Info);
        }
    }

    private static string FallbackMessageKey(PayrollPeriodState state)
    {
        switch (state)
        {
            case PayrollPeriodState.ReleasedForPayroll:
                return "PayrollPeriodReleasedForPayrollMessage";
            case PayrollPeriodState.Closed:
                return "PayrollPeriodExitedMessage";
            default:
                return "PayrollPeriodReleasedForCorrectionMessage";
        }
    }
}
