namespace HCMv3.BusinessRules;

/// <summary>Column widths and engine limits for the business rule engine, shared by entities and DTOs.</summary>
public static class BusinessRuleConsts
{
    public const int MaxCodeLength = 100;
    public const int MaxNameLength = 250;
    public const int MaxDescriptionLength = 1000;
    public const int MaxBaseObjectLength = 100;

    /// <summary>
    /// The authored rule tree, serialised. Generous because a rule with many branches is legitimate, but
    /// bounded so a runaway client cannot post an unbounded blob.
    /// </summary>
    public const int MaxDefinitionLength = 200000;

    /// <summary>
    /// How deep a rule's expression tree may nest. Guards the recursive evaluator against a hand-crafted
    /// definition that would otherwise overflow the stack.
    /// </summary>
    public const int MaxExpressionDepth = 32;

    /// <summary>
    /// Ceiling on rule steps executed for a single event. A rule set that exceeds it is abandoned with an
    /// error naming the rule, rather than being allowed to hold a save open indefinitely.
    /// </summary>
    public const int MaxExecutionSteps = 5000;

    /// <summary>Cache name for the per-tenant compiled rule set, keyed by "{baseObject}|{eventType}".</summary>
    public const string RuleSetCacheName = "HCMv3.BusinessRules.RuleSet";
}
