using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Domain.Services;

namespace HCMv3.HcmNotifications.Rules;

/// <summary>
/// Runs the Notification Master rules of the current tenant: for each enabled rule, every record of its
/// type and sub type that meets its criterion and has not been sent about by that rule is notified once,
/// and logged so it never is again. Legacy <c>SendNotification_Generic</c>, with the per-rule log in place of
/// the shared <c>NotificationSent</c> flag.
/// </summary>
public interface INotificationRuleRunner : IDomainService
{
    /// <summary>Runs every enabled rule. Each rule in its own unit of work; one failing does not stop the rest.</summary>
    Task<NotificationRuleRunSummary> RunAsync(DateTime today);

    /// <summary>
    /// What a rule would do today, without sending anything: how many records match, how many of those the
    /// rule has not yet sent about (when <paramref name="notificationMasterId"/> is given), and the first few
    /// by name. Shown before a rule is switched on, because a broad rule can address hundreds of people on its
    /// first run.
    /// </summary>
    Task<NotificationRulePreview> PreviewAsync(
        string sourceCode,
        int? subTypeId,
        string criterionJson,
        int? notificationMasterId,
        DateTime today);

    /// <summary>
    /// Logs every record that matches the rule today as already sent, without sending -- the go-live answer to
    /// "the first run would notify about every old record". Returns how many were marked.
    /// </summary>
    Task<int> MarkExistingAsSentAsync(int notificationMasterId, DateTime today);
}

public class NotificationRuleRunSummary
{
    public int RulesRun { get; set; }

    public int RulesFailed { get; set; }

    public int RulesSkipped { get; set; }

    public int NotificationsSent { get; set; }
}

public class NotificationRulePreview
{
    public int MatchCount { get; set; }

    /// <summary>Matches the rule has not logged yet -- what the next run would actually send.</summary>
    public int NotYetSentCount { get; set; }

    public List<NotificationRuleCandidate> Samples { get; } = new();

    /// <summary>Why the rule cannot run, when it cannot. Nothing is counted then.</summary>
    public List<string> Errors { get; } = new();
}
