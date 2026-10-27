using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp;
using Abp.Domain.Uow;
using Abp.Notifications;
using HCMv3.Notifications;
using HCMv3.SystemSecurity.Services;

namespace HCMv3.HcmNotifications;

/// <inheritdoc cref="INotificationDeliveryService"/>
public class NotificationDeliveryService : HCMv3DomainServiceBase, INotificationDeliveryService
{
    private readonly INotificationRecipientResolver _recipientResolver;
    private readonly IAppNotifier _appNotifier;
    private readonly IEmailService _emailService;
    private readonly IHcmNotificationSubscriptionGate _subscriptionGate;

    public NotificationDeliveryService(
        INotificationRecipientResolver recipientResolver,
        IAppNotifier appNotifier,
        IEmailService emailService,
        IHcmNotificationSubscriptionGate subscriptionGate)
    {
        _recipientResolver = recipientResolver;
        _appNotifier = appNotifier;
        _emailService = emailService;
        _subscriptionGate = subscriptionGate;
    }

    [UnitOfWork]
    public virtual async Task<NotificationDeliveryResult> DeliverAsync(NotificationDelivery delivery)
    {
        var result = new NotificationDeliveryResult();
        var config = delivery?.NotificationConfig;

        if (config == null || (!config.SendNotification && !config.SendEmail))
        {
            return result;
        }

        var recipients = _recipientResolver.Resolve(config, delivery.Subject);
        result.RecipientCount = recipients.Count;

        if (recipients.Count == 0)
        {
            return result;
        }

        if (config.SendNotification)
        {
            // Employees are recipients, users receive notices. Somebody with no login cannot be reached in the
            // app -- the email leg still reaches them.
            var userIds = recipients
                .Where(e => e.UserId.HasValue)
                .Select(e => e.UserId.Value)
                .ToList();

            if (userIds.Count > 0)
            {
                var message = await BuildMessageAsync(delivery);

                result.InAppCount = await DeliverInAppAsync(delivery.NotificationName, message, userIds);
            }
        }

        // Legacy sent the email only when the configuration had a template to send; with none, the in-app
        // notice is the whole of it.
        if (config.SendEmail && delivery.EmailTemplate != null)
        {
            await _emailService.SendEmailForNotificationConfigAsync(
                config,
                delivery.EmailTemplate,
                delivery.Subject,
                delivery.MergeValues);

            result.EmailSent = true;
        }

        return result;
    }

    [UnitOfWork]
    public virtual async Task<int> DeliverInAppAsync(string notificationName, string message, IEnumerable<long> userIds)
    {
        var tenantId = CurrentUnitOfWork?.GetTenantId();

        var users = (userIds ?? Enumerable.Empty<long>())
            .Distinct()
            .Select(userId => new UserIdentifier(tenantId, userId))
            .ToList();

        if (users.Count == 0 || string.IsNullOrWhiteSpace(message))
        {
            return 0;
        }

        var subscribed = await _subscriptionGate.FilterSubscribedAsync(notificationName, users);
        if (subscribed.Count == 0)
        {
            return 0;
        }

        await _appNotifier.SendMessageAsync(notificationName, message, subscribed.ToArray(), NotificationSeverity.Info);

        return subscribed.Count;
    }

    /// <summary>
    /// The template's parsed subject -- what legacy's task event showed as its subject -- or the caller's
    /// fallback when there is no template or it parses to nothing.
    /// </summary>
    private async Task<string> BuildMessageAsync(NotificationDelivery delivery)
    {
        if (delivery.EmailTemplate != null && !string.IsNullOrWhiteSpace(delivery.EmailTemplate.Subject))
        {
            var subject = await _emailService.ParseTemplateAsync(
                delivery.EmailTemplate.Subject,
                delivery.EmailTemplate,
                delivery.Subject,
                delivery.MergeValues);

            if (!string.IsNullOrWhiteSpace(subject))
            {
                return subject;
            }
        }

        return delivery.FallbackMessage ?? string.Empty;
    }
}
