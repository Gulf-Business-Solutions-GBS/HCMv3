using System;
using System.Collections.Generic;
using Abp.Dependency;
using HCMv3.BusinessRules;
using HCMv3.BusinessRules.Definition;
using HCMv3.BusinessRules.Services;

namespace HCMv3.HcmNotifications.Rules;

/// <inheritdoc cref="INotificationCriterionEvaluator"/>
public class NotificationCriterionEvaluator : INotificationCriterionEvaluator, ITransientDependency
{
    /// <summary>The rule code criteria are traced and reported under.</summary>
    public const string CriterionRuleCode = "NOTIFICATION_CRITERION";

    private readonly IRuleEvaluator _evaluator;
    private readonly IRuleDefinitionValidator _validator;
    private readonly IRuleEntityLoader _entityLoader;

    public NotificationCriterionEvaluator(
        IRuleEvaluator evaluator,
        IRuleDefinitionValidator validator,
        IRuleEntityLoader entityLoader)
    {
        _evaluator = evaluator;
        _validator = validator;
        _entityLoader = entityLoader;
    }

    public IReadOnlyList<string> Validate(INotificationRuleSource source, string criterionJson)
    {
        if (source == null)
        {
            return new[] { "The notification type is not one HCM can notify about." };
        }

        if (string.IsNullOrWhiteSpace(criterionJson))
        {
            return Array.Empty<string>();
        }

        NotificationCriterion criterion;
        try
        {
            criterion = Compile(criterionJson);
        }
        catch (RuleDefinitionException ex)
        {
            return new[] { ex.Message };
        }

        return _validator.Validate(AsDefinition(criterion.Condition), source.Criterion, RulePurpose.Notify);
    }

    public NotificationCriterion Compile(string criterionJson)
    {
        return string.IsNullOrWhiteSpace(criterionJson)
            ? new NotificationCriterion(null)
            : new NotificationCriterion(RuleDefinitionSerializer.ParseExpression(criterionJson));
    }

    public bool Matches(
        NotificationCriterion criterion,
        INotificationRuleSource source,
        NotificationRuleCandidate candidate,
        DateTime today,
        int? tenantId)
    {
        if (criterion == null || criterion.MatchesEverything)
        {
            return true;
        }

        var context = new RuleExecutionContext(source.Criterion, RuleEventType.None, today, tenantId)
        {
            AllowWrites = false,
            AllowedActionKinds = Array.Empty<string>(),

            // Runs once per record in a batch; a log row per evaluation would dwarf the notices themselves.
            SuppressDatabaseLogging = true,
            EntityLoader = _entityLoader
        };

        source.Bind(context, candidate);

        return _evaluator.Evaluate(AsDefinition(criterion.Condition), CriterionRuleCode, context).ConditionMatched;
    }

    /// <summary>
    /// A one-block rule with the criterion as its condition and no actions. Built directly rather than parsed,
    /// because the serializer rightly refuses a stored RULE whose block does nothing.
    /// </summary>
    private static RuleDefinition AsDefinition(RuleExpression condition)
    {
        return new RuleDefinition
        {
            Blocks = { new RuleBlock { If = condition } }
        };
    }
}
