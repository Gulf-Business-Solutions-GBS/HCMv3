using System;
using System.Collections.Generic;
using HCMv3.BusinessRules.Definition;

namespace HCMv3.HcmNotifications.Rules;

/// <summary>
/// The Notification Master criterion, through the business rule engine: parsed by its serializer, checked by
/// its validator against the source's whitelisted fields, and evaluated by its evaluator. No second formula
/// language and no second interpreter -- the formula editor's output is stored and run as-is.
///
/// <para>
/// The stored criterion is ONE expression (the JSON of a condition, exactly what the formula editor turns
/// "DaysUntil(Document.ExpiryDate) &lt;= 30" into). It is evaluated as a rule whose only block has that
/// condition and no actions, purpose <c>Notify</c>, with writes off: a criterion can test a record but never
/// change it. A blank criterion matches every record of the type and sub type.
/// </para>
/// </summary>
public interface INotificationCriterionEvaluator
{
    /// <summary>Every problem with the criterion, empty when it can run. A blank criterion has none.</summary>
    IReadOnlyList<string> Validate(INotificationRuleSource source, string criterionJson);

    /// <summary>
    /// Parses once for a whole run. Throws <c>RuleDefinitionException</c> on a criterion that cannot parse;
    /// returns a criterion that matches everything for a blank one.
    /// </summary>
    NotificationCriterion Compile(string criterionJson);

    /// <summary>Whether <paramref name="candidate"/> meets the criterion on <paramref name="today"/>.</summary>
    bool Matches(
        NotificationCriterion criterion,
        INotificationRuleSource source,
        NotificationRuleCandidate candidate,
        DateTime today,
        int? tenantId);
}

/// <summary>A parsed criterion, ready to evaluate against many records.</summary>
public class NotificationCriterion
{
    public NotificationCriterion(RuleExpression condition)
    {
        Condition = condition;
    }

    /// <summary>Null for a blank criterion, which matches every record.</summary>
    public RuleExpression Condition { get; }

    public bool MatchesEverything => Condition == null;
}
