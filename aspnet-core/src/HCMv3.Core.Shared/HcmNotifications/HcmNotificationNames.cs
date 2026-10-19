using System.Collections.Generic;

namespace HCMv3.HcmNotifications;

/// <summary>
/// The Zero notification definition names HCM publishes under, one per kind of notice a user can switch off.
///
/// <para>
/// Kept apart from Zero's own <c>AppNotificationNames</c> so the template file stays untouched. Every name
/// here is registered by <c>HcmNotificationProvider</c>, which is what puts it on Zero's own
/// "Notification settings" screen; a notice published under a name that is not registered would reach
/// people with no way for them to turn it off.
/// </para>
///
/// <para><b>Never rename a value.</b> Users' subscriptions are stored against these strings, so a rename
/// silently unsubscribes everybody from that notice.</para>
/// </summary>
public static class HcmNotificationNames
{
    private const string Prefix = "Hcm.";

    // The six fixed daily notifications ported from legacy SendRealTimeNotification.
    public const string EmployeeDocumentExpiry = Prefix + "EmployeeDocumentExpiry";
    public const string CompanyDocumentExpiry = Prefix + "CompanyDocumentExpiry";
    public const string ProbationEnd = Prefix + "ProbationEnd";
    public const string Separation = Prefix + "Separation";
    public const string AnnualLeaveMaxLimit = Prefix + "AnnualLeaveMaxLimit";
    public const string News = Prefix + "News";

    /// <summary>
    /// The definition a Notification Master rule on <paramref name="sourceCode"/> publishes under. One per
    /// SOURCE rather than per rule: rules are tenant data and come and go, while a definition is code, and a
    /// user deciding "no notices about loans" is deciding it about the source.
    /// </summary>
    public static string ForRuleSource(string sourceCode) => Prefix + "Rule." + sourceCode;

    /// <summary>Localization key of a definition's display name on the Notification settings screen.</summary>
    public static string DisplayNameKey(string notificationName) =>
        "NotificationDefinition_" + notificationName.Replace(Prefix, string.Empty).Replace(".", "_");

    /// <summary>The fixed definitions. Rule-source definitions come from <see cref="NotificationRuleSourceCodes.All"/>.</summary>
    public static readonly IReadOnlyList<string> Fixed = new[]
    {
        EmployeeDocumentExpiry,
        CompanyDocumentExpiry,
        ProbationEnd,
        Separation,
        AnnualLeaveMaxLimit,
        News
    };

    /// <summary>Every HCM definition: the fixed ones followed by one per rule source.</summary>
    public static IReadOnlyList<string> All
    {
        get
        {
            var all = new List<string>(Fixed);

            foreach (var sourceCode in NotificationRuleSourceCodes.All)
            {
                all.Add(ForRuleSource(sourceCode));
            }

            return all;
        }
    }
}
