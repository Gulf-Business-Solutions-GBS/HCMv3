using System.Collections.Generic;
using HCMv3.BusinessRules.Definition;
using HCMv3.BusinessRules.Records;

namespace HCMv3.BusinessRules.Services;

/// <summary>
/// Walks one parsed rule against one context: evaluates the variables, tests each block's condition, and
/// applies the resulting actions.
///
/// <para>
/// Split from <c>IBusinessRuleRunner</c> because the two have genuinely different jobs. The evaluator is
/// pure interpretation -- no database, no cache, no transaction, no idea what a tenant is -- which is what
/// makes it exhaustively unit-testable without a container. The runner is the part that decides WHICH rules
/// run, in what order, and what to do when one fails.
/// </para>
/// </summary>
public interface IRuleEvaluator
{
    /// <summary>
    /// Evaluates a rule. Throws <see cref="RuleExecutionException"/> when the rule is broken; deliberate
    /// business errors raised by the rule come back in the result instead, because those are a normal
    /// outcome rather than a failure of the engine.
    /// </summary>
    RuleEvaluationResult Evaluate(RuleDefinition definition, string ruleCode, RuleExecutionContext context);
}

/// <summary>What one rule did.</summary>
public class RuleEvaluationResult
{
    /// <summary>
    /// True when at least one block's condition held (or was unconditional). Distinguishes "the rule ran
    /// and decided not to act" from "the rule did not run", which is the first question anyone debugging
    /// configurable logic asks.
    /// </summary>
    public bool ConditionMatched { get; set; }

    /// <summary>Fields the rule assigned. Empty when the rule was evaluated with writes disallowed.</summary>
    public List<RuleFieldWrite> Writes { get; } = new();

    /// <summary>
    /// Business errors the rule raised, in order. Collected rather than thrown at the first one so a
    /// validation pass can tell the user everything that is wrong with their record in a single round trip.
    /// </summary>
    public List<RuleRaisedError> Errors { get; } = new();

    /// <summary>Creates and updates to other tables the rule decided on. Applied by the caller, never here.</summary>
    public List<RuleRecordChange> RecordChanges { get; } = new();

    public bool HasErrors => Errors.Count > 0;
}

/// <summary>A deliberate business error raised by a rule's error action.</summary>
public class RuleRaisedError
{
    public RuleRaisedError(string ruleCode, string messageKey, object[] args)
    {
        RuleCode = ruleCode;
        MessageKey = messageKey;
        Args = args;
    }

    public string RuleCode { get; }

    /// <summary>A localization key. Resolved by the caller, which is the layer that knows the user's language.</summary>
    public string MessageKey { get; }

    public object[] Args { get; }
}
