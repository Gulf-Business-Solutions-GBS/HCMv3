using System.Collections.Generic;
using HCMv3.GeneralMasterInfo;
using HCMv3.SystemSecurity;

namespace HCMv3.HcmNotifications;

/// <summary>One notice to deliver: what it is about, who the configuration addresses, and what it says.</summary>
public class NotificationDelivery
{
    /// <summary>The Zero notification definition (<see cref="HcmNotificationNames"/>).</summary>
    public string NotificationName { get; set; }

    /// <summary>Who to tell and by which channels. Nothing is sent without one.</summary>
    public NotificationConfig NotificationConfig { get; set; }

    /// <summary>
    /// What the notice is about -- an <c>Employee</c>, or a record carrying <c>EmployeeId</c>. The recipient
    /// resolver reads the subject employee off it, and the email template reads its merge fields off it.
    /// </summary>
    public object Subject { get; set; }

    /// <summary>The email template; also the in-app text (its parsed subject), as legacy's task event used.</summary>
    public EmailTemplate EmailTemplate { get; set; }

    /// <summary>Legacy's key/value list: values for <c>ObjectKey</c> tokens in the template.</summary>
    public IDictionary<string, object> MergeValues { get; set; }

    /// <summary>The in-app text when there is no template, or its subject parses to nothing.</summary>
    public string FallbackMessage { get; set; }
}

/// <summary>What a delivery did.</summary>
public class NotificationDeliveryResult
{
    /// <summary>Everyone the configuration addressed (after the resolver's de-duplication).</summary>
    public int RecipientCount { get; set; }

    /// <summary>Users given the in-app notice, after dropping those without a login or who switched it off.</summary>
    public int InAppCount { get; set; }

    public bool EmailSent { get; set; }
}
