using System.Collections.Generic;
using System.Threading.Tasks;
using Abp;

namespace HCMv3.HcmNotifications;

/// <summary>
/// Honours Zero's per-user notification subscriptions for HCM notices: who, of the people a notice is
/// addressed to, has not switched it off on the Notification settings screen.
///
/// <para>
/// Needed because Zero's publisher, given explicit recipients, delivers to them whether they are subscribed
/// or not -- subscriptions only steer "publish to everyone subscribed". HCM always knows its recipients (the
/// Notification Config names them), so without this the user's switch would do nothing.
/// </para>
/// </summary>
public interface IHcmNotificationSubscriptionGate
{
    /// <summary>
    /// <paramref name="users"/> narrowed to those subscribed to <paramref name="notificationName"/>, in the
    /// current tenant. First gives every existing user of the tenant the subscriptions they have never been
    /// offered -- see <see cref="HcmNotificationSettingNames.SubscribedDefinitions"/>.
    /// </summary>
    Task<List<UserIdentifier>> FilterSubscribedAsync(string notificationName, IEnumerable<UserIdentifier> users);

    /// <summary>
    /// Subscribes every active user of <paramref name="tenantId"/> to each HCM definition the tenant has not
    /// yet handed out, once. Safe to call repeatedly: definitions already handed out are skipped, so a user who
    /// switched one off is not switched back on.
    /// </summary>
    Task EnsureDefaultSubscriptionsAsync(int tenantId);
}
