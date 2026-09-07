using System.Collections.Generic;

namespace HCMv3.BusinessRules.Definition;

/// <summary>
/// Something a rule DOES once its condition holds -- the "Then" half of the tree.
///
/// <para>
/// Actions are a closed set on purpose. A rule can assign a whitelisted field or raise an error; it cannot
/// call a service, send a mail or write another table. Everything with a side effect stays in C# where it
/// is reviewed, tested and traceable. If a rule needs to trigger a side effect, the right shape is for the
/// rule to set a field and for ordinary domain code to react to that field.
/// </para>
/// </summary>
public abstract class RuleAction
{
    public abstract string Kind { get; }
}

/// <summary>
/// Assigns a value to a writable field -- the probation-end-date derivation is exactly this action.
///
/// <para>
/// The target must be marked writable in the base object descriptor. Read-only fields exist so a rule can
/// branch on identity, status or audit columns without being able to rewrite them.
/// </para>
/// </summary>
public class RuleSetAction : RuleAction
{
    public const string KindName = "set";

    public override string Kind => KindName;

    public string Parameter { get; set; }

    public string Field { get; set; }

    public RuleExpression Value { get; set; }

    public override string ToString() => "Set " + Parameter + "." + Field;
}

/// <summary>
/// Raises a business error, aborting whatever operation the rule was running inside.
///
/// <para>
/// The message is a LOCALIZATION KEY, not literal text. A rule authored in one tenant's English must still
/// produce Arabic for an Arabic user, and letting authors type raw strings would guarantee it does not --
/// the same reason no user-facing string is hardcoded anywhere else in this codebase.
/// </para>
/// </summary>
public class RuleErrorAction : RuleAction
{
    public const string KindName = "error";

    public override string Kind => KindName;

    public string MessageKey { get; set; }

    /// <summary>Values substituted into the localized message, in order.</summary>
    public List<RuleExpression> Args { get; set; } = new();
}
