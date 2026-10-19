using Abp.Localization;
using Abp.Notifications;

namespace HCMv3.HcmNotifications;

/// <summary>
/// Registers the HCM notification definitions with Zero, so each appears on Zero's own "Notification
/// settings" screen and a user can switch it off there.
///
/// <para>
/// A separate provider rather than additions to Zero's <c>AppNotificationProvider</c>, which is a template
/// file. ABP merges every provider in <c>Configuration.Notifications.Providers</c>; this one is added by
/// <see cref="HCMv3HcmNotificationsModule"/>.
/// </para>
///
/// <para>
/// No permission or feature dependency: legacy sent these to whoever the Notification Config named --
/// employees, managers, role holders -- and none of those audiences maps onto a permission.
/// </para>
/// </summary>
public class HcmNotificationProvider : NotificationProvider
{
    public override void SetNotifications(INotificationDefinitionContext context)
    {
        foreach (var name in HcmNotificationNames.All)
        {
            context.Manager.Add(new NotificationDefinition(
                name,
                displayName: L(HcmNotificationNames.DisplayNameKey(name))));
        }
    }

    private static ILocalizableString L(string name)
    {
        return new LocalizableString(name, HCMv3Consts.LocalizationSourceName);
    }
}
