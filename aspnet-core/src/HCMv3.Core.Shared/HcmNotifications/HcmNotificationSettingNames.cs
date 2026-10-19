namespace HCMv3.HcmNotifications;

/// <summary>ABP setting names owned by HCM notifications. Registered by <c>HcmNotificationSettingProvider</c>.</summary>
public static class HcmNotificationSettingNames
{
    /// <summary>
    /// Tenant setting: the HCM notification definitions this tenant's existing users have already been
    /// subscribed to, comma-separated.
    ///
    /// <para>
    /// Zero subscribes a user to every available notification when the user is CREATED. Users who already
    /// exist when a new definition ships are never subscribed to it, and Zero's publisher with explicit
    /// recipients ignores subscriptions anyway -- so HCM filters by subscription itself, and has to give
    /// existing users the subscription once, or they would silently stop getting notices legacy sent them.
    /// Recording which definitions were handed out is what makes it once: a user who later switches one off on
    /// the Notification settings screen stays switched off.
    /// </para>
    /// </summary>
    public const string SubscribedDefinitions = "App.HcmNotifications.SubscribedDefinitions";
}
