using System.Collections.Generic;

namespace HCMv3.BusinessRules.Definition;

/// <summary>
/// The parsed form of <c>BusinessRule.DefinitionJson</c>: optional variables, then an ordered list of
/// if/then/else blocks. Mirrors the SuccessFactors rule layout (a Variables section above the If/Then
/// tree), because that layout is what configurers coming from that product already know how to read.
/// </summary>
public class RuleDefinition
{
    /// <summary>
    /// Schema version of the definition format. Present from day one so a future change to the node shapes
    /// can be migrated rather than guessed at -- stored rules outlive the code that wrote them.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Named intermediate values, evaluated in order before any block. They exist to keep a tree readable:
    /// a duration computed once and referenced three times beats the same subtree pasted three times, and
    /// the evaluator only walks it once.
    /// </summary>
    public List<RuleVariable> Variables { get; set; } = new();

    /// <summary>
    /// Evaluated in order, and ALL of them are evaluated -- this is not a switch. A rule with several
    /// blocks is several independent if/thens sharing one set of variables.
    /// </summary>
    public List<RuleBlock> Blocks { get; set; } = new();
}

/// <summary>A named value computed once per execution.</summary>
public class RuleVariable
{
    public string Name { get; set; }

    public RuleDataType DataType { get; set; }

    public RuleExpression Value { get; set; }
}

/// <summary>
/// One if/then/else. A block with a null <see cref="If"/> is unconditional, which is how a plain derivation
/// ("always set probation end date") is expressed without a dummy always-true condition.
/// </summary>
public class RuleBlock
{
    public RuleExpression If { get; set; }

    public List<RuleAction> Then { get; set; } = new();

    public List<RuleAction> Else { get; set; } = new();
}
