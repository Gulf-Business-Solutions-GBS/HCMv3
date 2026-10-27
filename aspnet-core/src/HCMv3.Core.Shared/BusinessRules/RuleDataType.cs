namespace HCMv3.BusinessRules;

/// <summary>
/// The value types a rule can read, write and compare. Deliberately small: every domain type the authoring
/// UI exposes is projected onto one of these, so the rule builder needs one editor per member rather than
/// one per CLR type, and the evaluator has a single coercion table.
/// </summary>
public enum RuleDataType
{
    Unknown = 0,

    /// <summary>Text. Compared with the ordinal, case-insensitive comparer -- rule authors are not thinking about culture.</summary>
    String = 1,

    /// <summary>Whole numbers. Backed by <c>long</c> internally so an <c>int</c> field and a literal never disagree.</summary>
    Integer = 2,

    /// <summary>Fractional numbers. Backed by <c>decimal</c>, never <c>double</c> -- money and entitlement days must not drift.</summary>
    Decimal = 3,

    /// <summary>True/false.</summary>
    Boolean = 4,

    /// <summary>A date with no meaningful time-of-day. Compared by <c>Date</c> only.</summary>
    Date = 5,

    /// <summary>A date and time.</summary>
    DateTime = 6,

    /// <summary>
    /// A reference to a lookup row, carried as its integer id. The field descriptor names the lookup source
    /// so the builder can show "Annual Leave (1000)" the way the SuccessFactors editor does, while the
    /// evaluator still compares plain ids.
    /// </summary>
    Lookup = 7,

    /// <summary>An enum member, carried as its integer value with the descriptor naming the enum type.</summary>
    Enum = 8
}
