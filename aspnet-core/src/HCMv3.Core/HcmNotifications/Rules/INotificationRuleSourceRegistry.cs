using System.Collections.Generic;

namespace HCMv3.HcmNotifications.Rules;

/// <summary>The rule sources registered in code -- the whole of what a Notification Master rule may be about.</summary>
public interface INotificationRuleSourceRegistry
{
    IReadOnlyList<INotificationRuleSource> GetAll();

    /// <summary>The source with that code, or null. Case-insensitive, as stored codes may have been typed.</summary>
    INotificationRuleSource Find(string code);
}
