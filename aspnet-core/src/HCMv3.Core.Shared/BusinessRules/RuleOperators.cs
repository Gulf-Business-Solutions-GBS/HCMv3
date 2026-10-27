namespace HCMv3.BusinessRules;

/// <summary>
/// How two values are compared in a rule condition. Persisted by name in the rule's definition JSON, so
/// members may be added but not renamed.
/// </summary>
public enum RuleComparisonOperator
{
    None = 0,
    Equal = 1,
    NotEqual = 2,
    GreaterThan = 3,
    GreaterThanOrEqual = 4,
    LessThan = 5,
    LessThanOrEqual = 6,

    /// <summary>Substring test on <see cref="RuleDataType.String"/>. Ordinal, case-insensitive.</summary>
    Contains = 7,
    StartsWith = 8,
    EndsWith = 9,

    /// <summary>Membership test against a list literal -- the rule-authoring equivalent of SQL <c>IN</c>.</summary>
    In = 10,
    NotIn = 11,

    /// <summary>True when the value is null, or an empty/whitespace string.</summary>
    IsEmpty = 12,
    IsNotEmpty = 13
}

/// <summary>How the operands of a condition group combine.</summary>
public enum RuleLogicalOperator
{
    None = 0,
    And = 1,
    Or = 2
}

/// <summary>
/// Arithmetic available inside a rule expression. Division by zero raises a rule error rather than throwing
/// a bare <c>DivideByZeroException</c>, because the message has to name the rule for the author to fix it.
/// </summary>
public enum RuleArithmeticOperator
{
    None = 0,
    Add = 1,
    Subtract = 2,
    Multiply = 3,
    Divide = 4,
    Modulo = 5
}
