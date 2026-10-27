using System;

namespace HCMv3.BusinessRules;

/// <summary>
/// A rule definition is malformed: an unknown node kind, a missing operand, a field the base object does
/// not expose, a tree nested past <c>BusinessRuleConsts.MaxExpressionDepth</c>.
///
/// <para>
/// Raised at PARSE time, which is the whole point of having a parser separate from the evaluator. A rule is
/// parsed when it is saved and when it is loaded into the runner's cache, so a malformed definition is
/// rejected at the authoring screen rather than discovered halfway through somebody's payroll run.
/// </para>
/// </summary>
public class RuleDefinitionException : Exception
{
    public RuleDefinitionException(string message)
        : base(message)
    {
    }

    public RuleDefinitionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// A rule failed while running: a function threw, a value would not coerce, the step budget was exhausted.
///
/// <para>
/// Distinct from <c>Abp.UI.UserFriendlyException</c> on purpose. A <c>RuleErrorAction</c> raising a
/// deliberate business error becomes a UserFriendlyException, because it is a message for the end user. This
/// exception is the engine reporting that a rule is BROKEN, which is a message for whoever maintains the
/// rule -- the runner catches it, names the rule in the execution log, and then decides whether to abort or
/// continue based on the assignment's <c>ContinueOnError</c>.
/// </para>
/// </summary>
public class RuleExecutionException : Exception
{
    public RuleExecutionException(string ruleCode, string message)
        : base(message)
    {
        RuleCode = ruleCode;
    }

    public RuleExecutionException(string ruleCode, string message, Exception innerException)
        : base(message, innerException)
    {
        RuleCode = ruleCode;
    }

    /// <summary>The rule that failed. Always populated -- an error that does not name its rule is useless.</summary>
    public string RuleCode { get; }
}
