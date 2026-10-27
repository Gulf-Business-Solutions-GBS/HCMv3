using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Domain.Services;

namespace HCMv3.HcmNotifications;

/// <summary>
/// Sends one HCM notice through Zero's channels: the in-app bell (Zero <c>IAppNotifier</c>, under an HCM
/// notification definition, honouring the user's subscription) and email (the existing templated
/// <c>IEmailService</c>), to the people <c>INotificationRecipientResolver</c> says the Notification Config
/// addresses.
///
/// <para>
/// The single place the fixed daily notifications and the Notification Master rules both go through, so the
/// two cannot disagree about who is told or whether a user's "off" switch is respected. It mirrors the
/// workflow dispatcher's shape -- resolve recipients once, then each channel the configuration switches on.
/// </para>
/// </summary>
public interface INotificationDeliveryService : IDomainService
{
    Task<NotificationDeliveryResult> DeliverAsync(NotificationDelivery delivery);

    /// <summary>
    /// The in-app leg alone, to users already chosen by the caller -- for a notice that has no Notification
    /// Config, such as news, whose audience is its organisation unit. Still honours each user's subscription.
    /// Returns how many users it reached.
    /// </summary>
    Task<int> DeliverInAppAsync(string notificationName, string message, IEnumerable<long> userIds);
}
