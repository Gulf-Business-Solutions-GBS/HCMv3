using System.Collections.Generic;

namespace HCMv3.BusinessRules.Definition;

/// <summary>
/// A node in a rule's expression tree.
///
/// <para>
/// The tree is the rule. It is what the authoring UI edits, what is persisted in
/// <c>BusinessRule.DefinitionJson</c>, and what <c>IRuleEvaluator</c> walks -- one representation end to
/// end, so there is no round-trip between a visual form and a text expression that could disagree.
/// </para>
///
/// <para><b>Why an interpreted tree rather than compiled expressions.</b> A rule authored by a configurer
/// is untrusted input. A tree of closed node types can only do what the node types allow: read a
/// whitelisted field, call a registered function, compare, assign. There is no escape hatch into
/// reflection, IO or arbitrary code, which is precisely the property a string-expression engine cannot
/// offer. Interpretation costs microseconds against a save that costs milliseconds.</para>
///
/// <para><b>Adding a node type</b> means a class here, a case in the converter, a case in the evaluator and
/// a case in the Angular builder. The evaluator's switch is exhaustive and throws on an unknown kind rather
/// than defaulting -- a node the evaluator does not understand must never be silently skipped, because a
/// skipped condition turns "if manager" into "always".</para>
/// </summary>
public abstract class RuleExpression
{
    /// <summary>
    /// The discriminator persisted in JSON. Stable string, never renamed -- it is written into every
    /// tenant's stored rules.
    /// </summary>
    public abstract string Kind { get; }
}

/// <summary>A literal typed value: 100, "CHRO", true, a date.</summary>
public class RuleConstantExpression : RuleExpression
{
    public const string KindName = "const";

    public override string Kind => KindName;

    public RuleDataType DataType { get; set; }

    /// <summary>
    /// Held as the raw string form and coerced on evaluation against <see cref="DataType"/>. Storing the
    /// text rather than a boxed value keeps the JSON round-trip lossless for dates and decimals, which is
    /// where a "just keep the JsonElement" approach quietly loses precision or timezone-shifts a date.
    /// </summary>
    public string Value { get; set; }
}

/// <summary>
/// Reads a field of a parameter, such as the joining date of the Employee parameter.
///
/// <para>
/// The path is exactly two segments -- parameter name, then field name. It is not a navigation path, and
/// that is deliberate: a chain like Employee.Manager.Manager.Department.Name would be an unbounded set of
/// lazy loads inside a save. Anything deeper than one hop is exposed as a purpose-built function or as a
/// flattened field on the base object descriptor, where its cost is visible to whoever added it.
/// </para>
/// </summary>
public class RuleFieldExpression : RuleExpression
{
    public const string KindName = "field";

    public override string Kind => KindName;

    /// <summary>The parameter, e.g. "Employee" or "Applier".</summary>
    public string Parameter { get; set; }

    /// <summary>The whitelisted field name on that parameter.</summary>
    public string Field { get; set; }

    public override string ToString() => Parameter + "." + Field;
}

/// <summary>Reads a variable computed earlier in the same rule.</summary>
public class RuleVariableExpression : RuleExpression
{
    public const string KindName = "var";

    public override string Kind => KindName;

    public string Name { get; set; }
}

/// <summary>
/// Calls a registered function, such as GetNumberOfCalendarDays(start, end). Functions are the extension
/// point developers own -- anything a rule needs that is not a field, a comparison or arithmetic arrives as
/// a function, so the vocabulary grows without the node types having to.
/// </summary>
public class RuleFunctionExpression : RuleExpression
{
    public const string KindName = "fn";

    public override string Kind => KindName;

    public string Name { get; set; }

    public List<RuleExpression> Args { get; set; } = new();
}

/// <summary>Compares two values. The whole point of a rule's If.</summary>
public class RuleComparisonExpression : RuleExpression
{
    public const string KindName = "compare";

    public override string Kind => KindName;

    public RuleComparisonOperator Operator { get; set; }

    public RuleExpression Left { get; set; }

    /// <summary>Null for the unary operators IsEmpty and IsNotEmpty.</summary>
    public RuleExpression Right { get; set; }
}

/// <summary>
/// Combines conditions with AND / OR. Rendered as the "and" / "or" junction box in the authoring UI, and
/// evaluated with short-circuiting, so an And whose first operand is false never evaluates the rest.
/// </summary>
public class RuleLogicalExpression : RuleExpression
{
    public const string KindName = "logical";

    public override string Kind => KindName;

    public RuleLogicalOperator Operator { get; set; }

    public List<RuleExpression> Operands { get; set; } = new();
}

/// <summary>Negates a condition.</summary>
public class RuleNotExpression : RuleExpression
{
    public const string KindName = "not";

    public override string Kind => KindName;

    public RuleExpression Operand { get; set; }
}

/// <summary>Arithmetic over numbers, and date-minus-date yielding a day count.</summary>
public class RuleArithmeticExpression : RuleExpression
{
    public const string KindName = "arith";

    public override string Kind => KindName;

    public RuleArithmeticOperator Operator { get; set; }

    public RuleExpression Left { get; set; }

    public RuleExpression Right { get; set; }
}

/// <summary>A list literal, the right-hand side of In and NotIn.</summary>
public class RuleListExpression : RuleExpression
{
    public const string KindName = "list";

    public override string Kind => KindName;

    public List<RuleExpression> Items { get; set; } = new();
}
