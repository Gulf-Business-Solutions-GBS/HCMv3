using System.Text.Json.Nodes;
using HCMv3.BusinessRules;
using HCMv3.BusinessRules.Definition;
using HCMv3.BusinessRules.Seed;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.BusinessRules;

/// <summary>
/// Covers the parser that turns stored JSON into a rule tree.
///
/// This is the engine's front door for untrusted input: a rule definition arrives from a browser, is stored
/// verbatim, and is later walked inside somebody's save. Everything the parser fails to reject here becomes
/// a null reference at the worst possible moment, so the tests care as much about what is refused as about
/// what round-trips.
/// </summary>
// ReSharper disable once InconsistentNaming
public class RuleDefinitionSerializer_Tests
{
    [Fact]
    public void Should_Round_Trip_A_Rule_Without_Losing_Anything()
    {
        var original = SampleBusinessRuleDefinitions.ProbationEndDate();

        var reparsed = RuleDefinitionSerializer.Parse(RuleDefinitionSerializer.Serialize(original));

        reparsed.Blocks.Count.ShouldBe(original.Blocks.Count);

        var block = reparsed.Blocks[0];
        block.If.ShouldBeOfType<RuleComparisonExpression>();
        block.Then.Count.ShouldBe(1);
        block.Else.Count.ShouldBe(1);

        var then = block.Then[0].ShouldBeOfType<RuleSetAction>();
        then.Field.ShouldBe("ProbationEndDate");
        then.Value.ShouldBeOfType<RuleFunctionExpression>().Name.ShouldBe("AddCalendarDays");
    }

    [Fact]
    public void Should_Round_Trip_A_Routing_Rule_With_Nested_Logical_Operators()
    {
        var original = SampleBusinessRuleDefinitions.LeaveApprovalRouting();

        var reparsed = RuleDefinitionSerializer.Parse(RuleDefinitionSerializer.Serialize(original));

        reparsed.Blocks.Count.ShouldBe(2);

        var second = reparsed.Blocks[1].If.ShouldBeOfType<RuleLogicalExpression>();
        second.Operator.ShouldBe(RuleLogicalOperator.And);
        second.Operands.Count.ShouldBe(3);
        second.Operands[1].ShouldBeOfType<RuleNotExpression>();
    }

    /// <summary>
    /// The reason this parser is hand-written instead of using System.Text.Json polymorphism, which
    /// requires the type discriminator to be the first property. The Angular builder serialises whatever
    /// key order its component state happens to have, so a rule must load regardless of where "kind" sits.
    /// </summary>
    [Fact]
    public void Should_Parse_A_Node_Whose_Kind_Is_Not_The_First_Property()
    {
        var json = """
        {
          "version": 1,
          "blocks": [
            {
              "then": [
                { "parameter": "Employee", "field": "ProbationDays", "kind": "set",
                  "value": { "dataType": "Integer", "value": "90", "kind": "const" } }
              ]
            }
          ]
        }
        """;

        var definition = RuleDefinitionSerializer.Parse(json);

        var action = definition.Blocks[0].Then[0].ShouldBeOfType<RuleSetAction>();
        action.Field.ShouldBe("ProbationDays");
        action.Value.ShouldBeOfType<RuleConstantExpression>().Value.ShouldBe("90");
    }

    /// <summary>
    /// A number posted unquoted must mean the same thing as a quoted one. The builder produces either,
    /// depending on which editor filled the field in.
    /// </summary>
    [Fact]
    public void Should_Accept_A_Constant_Whose_Value_Is_An_Unquoted_Number()
    {
        var json = """
        {
          "blocks": [
            { "then": [ { "kind": "set", "parameter": "Employee", "field": "ProbationDays",
                          "value": { "kind": "const", "dataType": "Integer", "value": 90 } } ] }
          ]
        }
        """;

        var definition = RuleDefinitionSerializer.Parse(json);

        definition.Blocks[0].Then[0].ShouldBeOfType<RuleSetAction>()
            .Value.ShouldBeOfType<RuleConstantExpression>().Value.ShouldBe("90");
    }

    [Fact]
    public void Should_Reject_An_Unknown_Node_Kind()
    {
        var json = """
        {
          "blocks": [
            { "if": { "kind": "invoke-shell", "cmd": "rm -rf /" },
              "then": [ { "kind": "set", "parameter": "Employee", "field": "ProbationDays",
                          "value": { "kind": "const", "dataType": "Integer", "value": "1" } } ] }
          ]
        }
        """;

        var exception = Should.Throw<RuleDefinitionException>(() => RuleDefinitionSerializer.Parse(json));

        exception.Message.ShouldContain("invoke-shell");
    }

    /// <summary>
    /// A rule with no blocks would load, run, and quietly do nothing -- at runtime indistinguishable from a
    /// rule that is working correctly. Refused at the door instead.
    /// </summary>
    [Fact]
    public void Should_Reject_A_Definition_With_No_Blocks()
    {
        Should.Throw<RuleDefinitionException>(() => RuleDefinitionSerializer.Parse("""{ "version": 1 }"""));
    }

    [Fact]
    public void Should_Reject_A_Block_With_No_Actions()
    {
        var json = """{ "blocks": [ { "then": [] } ] }""";

        Should.Throw<RuleDefinitionException>(() => RuleDefinitionSerializer.Parse(json));
    }

    /// <summary>
    /// An empty AND evaluates to true and an empty OR to false. Both read on screen as "no condition set
    /// yet", so both would fire a rule the author believed was still unconfigured.
    /// </summary>
    [Fact]
    public void Should_Reject_A_Logical_Expression_With_No_Operands()
    {
        var json = """
        {
          "blocks": [
            { "if": { "kind": "logical", "op": "And", "operands": [] },
              "then": [ { "kind": "set", "parameter": "Employee", "field": "ProbationDays",
                          "value": { "kind": "const", "dataType": "Integer", "value": "1" } } ] }
          ]
        }
        """;

        Should.Throw<RuleDefinitionException>(() => RuleDefinitionSerializer.Parse(json));
    }

    [Fact]
    public void Should_Reject_A_Comparison_Missing_Its_Right_Operand()
    {
        var json = """
        {
          "blocks": [
            { "if": { "kind": "compare", "op": "Equal",
                      "left": { "kind": "field", "parameter": "Employee", "field": "Code" } },
              "then": [ { "kind": "set", "parameter": "Employee", "field": "ProbationDays",
                          "value": { "kind": "const", "dataType": "Integer", "value": "1" } } ] }
          ]
        }
        """;

        Should.Throw<RuleDefinitionException>(() => RuleDefinitionSerializer.Parse(json));
    }

    /// <summary>IsEmpty takes no right operand, so the same shape must be accepted for it.</summary>
    [Fact]
    public void Should_Accept_A_Unary_Comparison_With_No_Right_Operand()
    {
        var json = """
        {
          "blocks": [
            { "if": { "kind": "compare", "op": "IsEmpty",
                      "left": { "kind": "field", "parameter": "Employee", "field": "Code" } },
              "then": [ { "kind": "set", "parameter": "Employee", "field": "ProbationDays",
                          "value": { "kind": "const", "dataType": "Integer", "value": "1" } } ] }
          ]
        }
        """;

        var definition = RuleDefinitionSerializer.Parse(json);

        definition.Blocks[0].If.ShouldBeOfType<RuleComparisonExpression>().Right.ShouldBeNull();
    }

    /// <summary>
    /// The recursion guard. A tree deeper than the limit would recurse just as far in the evaluator and
    /// overflow the stack, which is a process-level failure rather than a rule-level one.
    /// </summary>
    [Fact]
    public void Should_Reject_A_Tree_Nested_Past_The_Depth_Limit()
    {
        var node = new JsonObject
        {
            ["kind"] = "const",
            ["dataType"] = "Boolean",
            ["value"] = "true"
        };

        JsonNode condition = node;

        for (var i = 0; i < BusinessRuleConsts.MaxExpressionDepth + 5; i++)
        {
            condition = new JsonObject { ["kind"] = "not", ["operand"] = condition };
        }

        var root = new JsonObject
        {
            ["blocks"] = new JsonArray
            {
                new JsonObject
                {
                    ["if"] = condition,
                    ["then"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["kind"] = "set",
                            ["parameter"] = "Employee",
                            ["field"] = "ProbationDays",
                            ["value"] = new JsonObject
                            {
                                ["kind"] = "const",
                                ["dataType"] = "Integer",
                                ["value"] = "1"
                            }
                        }
                    }
                }
            }
        };

        var exception = Should.Throw<RuleDefinitionException>(
            () => RuleDefinitionSerializer.Parse(root.ToJsonString()));

        exception.Message.ShouldContain("nested");
    }

    [Fact]
    public void Should_Reject_Malformed_Json()
    {
        Should.Throw<RuleDefinitionException>(() => RuleDefinitionSerializer.Parse("{ not json"));
    }

    /// <summary>
    /// Enum members are stored by NAME. Accepting the numeric form would let a future renumbering silently
    /// change what every stored rule means.
    /// </summary>
    [Fact]
    public void Should_Reject_An_Operator_Given_As_A_Number()
    {
        var json = """
        {
          "blocks": [
            { "if": { "kind": "compare", "op": "1",
                      "left": { "kind": "field", "parameter": "Employee", "field": "Code" },
                      "right": { "kind": "const", "dataType": "String", "value": "X" } },
              "then": [ { "kind": "set", "parameter": "Employee", "field": "ProbationDays",
                          "value": { "kind": "const", "dataType": "Integer", "value": "1" } } ] }
          ]
        }
        """;

        Should.Throw<RuleDefinitionException>(() => RuleDefinitionSerializer.Parse(json));
    }

    [Fact]
    public void Should_Accept_Operator_Names_Case_Insensitively()
    {
        var json = """
        {
          "blocks": [
            { "if": { "kind": "compare", "op": "greaterthan",
                      "left": { "kind": "field", "parameter": "Employee", "field": "ProbationDays" },
                      "right": { "kind": "const", "dataType": "Integer", "value": "5" } },
              "then": [ { "kind": "set", "parameter": "Employee", "field": "ProbationDays",
                          "value": { "kind": "const", "dataType": "Integer", "value": "1" } } ] }
          ]
        }
        """;

        RuleDefinitionSerializer.Parse(json).Blocks[0].If
            .ShouldBeOfType<RuleComparisonExpression>()
            .Operator.ShouldBe(RuleComparisonOperator.GreaterThan);
    }
}
