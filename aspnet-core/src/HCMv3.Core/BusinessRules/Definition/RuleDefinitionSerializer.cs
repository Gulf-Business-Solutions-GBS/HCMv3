using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace HCMv3.BusinessRules.Definition;

/// <summary>
/// Converts between <see cref="RuleDefinition"/> and its stored JSON form.
///
/// <para>
/// Hand-written rather than attribute-driven polymorphism, for a specific reason: <c>System.Text.Json</c>
/// polymorphic deserialization requires the type discriminator to be the FIRST property of each object.
/// The Angular builder serialises whatever key order its component state happens to have, so a rule that
/// saved fine yesterday could fail to load today purely because a field was added to a form model. Reading
/// through <see cref="JsonNode"/> removes that class of bug entirely -- key order stops mattering.
/// </para>
///
/// <para>
/// The parser is also strict where <c>System.Text.Json</c> is lenient. An unknown node kind, a missing
/// operand or a comparison with no left-hand side is an exception, not a null property that surfaces later
/// as a <c>NullReferenceException</c> in the middle of somebody's save. Everything this class rejects is a
/// rule that could not have run correctly anyway.
/// </para>
///
/// <para><b>This is syntax only.</b> Whether <c>Employee.JoiningDate</c> exists, whether a function is
/// registered, and whether a target field is writable are SEMANTIC questions answered by
/// <c>IRuleDefinitionValidator</c> against the registries. Splitting them keeps the parser free of
/// dependencies, so it can be used in tests and seed data without a container.</para>
/// </summary>
public static class RuleDefinitionSerializer
{
    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = false
    };

    /// <summary>
    /// Parses stored JSON into a definition tree, or throws <see cref="RuleDefinitionException"/> naming
    /// what is wrong and where.
    /// </summary>
    public static RuleDefinition Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new RuleDefinitionException("The rule definition is empty.");
        }

        JsonNode root;
        try
        {
            root = JsonNode.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new RuleDefinitionException("The rule definition is not valid JSON: " + ex.Message, ex);
        }

        if (root is not JsonObject rootObject)
        {
            throw new RuleDefinitionException("The rule definition must be a JSON object.");
        }

        var definition = new RuleDefinition
        {
            Version = GetInt(rootObject, "version") ?? 1
        };

        if (rootObject["variables"] is JsonArray variables)
        {
            foreach (var node in variables)
            {
                definition.Variables.Add(ParseVariable(node));
            }
        }

        if (rootObject["blocks"] is JsonArray blocks)
        {
            foreach (var node in blocks)
            {
                definition.Blocks.Add(ParseBlock(node));
            }
        }

        if (definition.Blocks.Count == 0)
        {
            // A rule with no blocks would load, run, and do nothing -- indistinguishable at runtime from a
            // rule that is working. Refuse it at the door.
            throw new RuleDefinitionException("The rule definition contains no if/then blocks.");
        }

        return definition;
    }

    /// <summary>Writes a definition back to its stored form. Used by seeds, imports and the test harness.</summary>
    public static string Serialize(RuleDefinition definition)
    {
        if (definition == null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        var root = new JsonObject
        {
            ["version"] = definition.Version
        };

        if (definition.Variables.Count > 0)
        {
            var variables = new JsonArray();
            foreach (var variable in definition.Variables)
            {
                variables.Add(new JsonObject
                {
                    ["name"] = variable.Name,
                    ["dataType"] = variable.DataType.ToString(),
                    ["value"] = WriteExpression(variable.Value)
                });
            }

            root["variables"] = variables;
        }

        var blocks = new JsonArray();
        foreach (var block in definition.Blocks)
        {
            var blockObject = new JsonObject();

            if (block.If != null)
            {
                blockObject["if"] = WriteExpression(block.If);
            }

            blockObject["then"] = WriteActions(block.Then);

            if (block.Else is { Count: > 0 })
            {
                blockObject["else"] = WriteActions(block.Else);
            }

            blocks.Add(blockObject);
        }

        root["blocks"] = blocks;

        return root.ToJsonString(WriteOptions);
    }

    private static RuleVariable ParseVariable(JsonNode node)
    {
        if (node is not JsonObject obj)
        {
            throw new RuleDefinitionException("Each variable must be a JSON object.");
        }

        var name = GetString(obj, "name");
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new RuleDefinitionException("A variable is missing its name.");
        }

        return new RuleVariable
        {
            Name = name,
            DataType = ParseEnum<RuleDataType>(GetString(obj, "dataType"), "variable " + name + " dataType"),
            Value = ParseExpression(obj["value"], 1)
        };
    }

    private static RuleBlock ParseBlock(JsonNode node)
    {
        if (node is not JsonObject obj)
        {
            throw new RuleDefinitionException("Each block must be a JSON object.");
        }

        var block = new RuleBlock
        {
            If = obj["if"] == null ? null : ParseExpression(obj["if"], 1),
            Then = ParseActions(obj["then"]),
            Else = ParseActions(obj["else"])
        };

        if (block.Then.Count == 0 && block.Else.Count == 0)
        {
            throw new RuleDefinitionException("A block must contain at least one action.");
        }

        return block;
    }

    private static List<RuleAction> ParseActions(JsonNode node)
    {
        var actions = new List<RuleAction>();

        if (node == null)
        {
            return actions;
        }

        if (node is not JsonArray array)
        {
            throw new RuleDefinitionException("Rule actions must be a JSON array.");
        }

        foreach (var item in array)
        {
            actions.Add(ParseAction(item));
        }

        return actions;
    }

    private static RuleAction ParseAction(JsonNode node)
    {
        if (node is not JsonObject obj)
        {
            throw new RuleDefinitionException("Each action must be a JSON object.");
        }

        var kind = GetString(obj, "kind");

        switch (kind)
        {
            case RuleSetAction.KindName:
                return new RuleSetAction
                {
                    Parameter = RequireString(obj, "parameter", "set action"),
                    Field = RequireString(obj, "field", "set action"),
                    Value = ParseExpression(obj["value"], 1)
                };

            case RuleErrorAction.KindName:
                return new RuleErrorAction
                {
                    MessageKey = RequireString(obj, "messageKey", "error action"),
                    Args = ParseExpressionList(obj["args"], 1)
                };

            case RuleCreateRecordAction.KindName:
                return new RuleCreateRecordAction
                {
                    Table = RequireString(obj, "table", "create record action"),
                    Values = ParseRecordValues(obj["values"], "create record action")
                };

            case RuleUpdateRecordAction.KindName:
                return new RuleUpdateRecordAction
                {
                    Table = RequireString(obj, "table", "update record action"),
                    RecordId = ParseExpression(RequireNode(obj, "recordId", "update record action"), 1),
                    Values = ParseRecordValues(obj["values"], "update record action")
                };

            default:
                throw new RuleDefinitionException("Unknown rule action kind: " + (kind ?? "(missing)"));
        }
    }

    private static List<RuleExpression> ParseExpressionList(JsonNode node, int depth)
    {
        var expressions = new List<RuleExpression>();

        if (node == null)
        {
            return expressions;
        }

        if (node is not JsonArray array)
        {
            throw new RuleDefinitionException("Expected a JSON array of expressions.");
        }

        foreach (var item in array)
        {
            expressions.Add(ParseExpression(item, depth + 1));
        }

        return expressions;
    }

    private static RuleExpression ParseExpression(JsonNode node, int depth)
    {
        if (depth > BusinessRuleConsts.MaxExpressionDepth)
        {
            // Guards the recursive walk. A tree this deep is either generated or hostile; either way the
            // evaluator would recurse just as far and overflow the stack rather than raise anything useful.
            throw new RuleDefinitionException(
                "The rule expression is nested deeper than " + BusinessRuleConsts.MaxExpressionDepth + " levels.");
        }

        if (node is not JsonObject obj)
        {
            throw new RuleDefinitionException("Each expression must be a JSON object.");
        }

        var kind = GetString(obj, "kind");

        switch (kind)
        {
            case RuleConstantExpression.KindName:
                return new RuleConstantExpression
                {
                    DataType = ParseEnum<RuleDataType>(GetString(obj, "dataType"), "constant dataType"),
                    Value = GetString(obj, "value")
                };

            case RuleFieldExpression.KindName:
                return new RuleFieldExpression
                {
                    Parameter = RequireString(obj, "parameter", "field expression"),
                    Field = RequireString(obj, "field", "field expression")
                };

            case RuleVariableExpression.KindName:
                return new RuleVariableExpression
                {
                    Name = RequireString(obj, "name", "variable expression")
                };

            case RuleFunctionExpression.KindName:
                return new RuleFunctionExpression
                {
                    Name = RequireString(obj, "name", "function expression"),
                    Args = ParseExpressionList(obj["args"], depth)
                };

            case RuleComparisonExpression.KindName:
                return ParseComparison(obj, depth);

            case RuleLogicalExpression.KindName:
                return ParseLogical(obj, depth);

            case RuleNotExpression.KindName:
                return new RuleNotExpression
                {
                    Operand = ParseExpression(RequireNode(obj, "operand", "not expression"), depth + 1)
                };

            case RuleArithmeticExpression.KindName:
                return new RuleArithmeticExpression
                {
                    Operator = ParseEnum<RuleArithmeticOperator>(GetString(obj, "op"), "arithmetic operator"),
                    Left = ParseExpression(RequireNode(obj, "left", "arithmetic expression"), depth + 1),
                    Right = ParseExpression(RequireNode(obj, "right", "arithmetic expression"), depth + 1)
                };

            case RuleListExpression.KindName:
                return new RuleListExpression
                {
                    Items = ParseExpressionList(obj["items"], depth)
                };

            default:
                throw new RuleDefinitionException("Unknown rule expression kind: " + (kind ?? "(missing)"));
        }
    }

    private static RuleExpression ParseComparison(JsonObject obj, int depth)
    {
        var op = ParseEnum<RuleComparisonOperator>(GetString(obj, "op"), "comparison operator");

        var comparison = new RuleComparisonExpression
        {
            Operator = op,
            Left = ParseExpression(RequireNode(obj, "left", "comparison"), depth + 1)
        };

        var isUnary = op is RuleComparisonOperator.IsEmpty or RuleComparisonOperator.IsNotEmpty;

        if (!isUnary)
        {
            comparison.Right = ParseExpression(RequireNode(obj, "right", "comparison"), depth + 1);
        }

        return comparison;
    }

    private static RuleExpression ParseLogical(JsonObject obj, int depth)
    {
        var operands = ParseExpressionList(obj["operands"], depth);

        if (operands.Count == 0)
        {
            // An empty And would evaluate to true and an empty Or to false. Both are traps: the author sees
            // an empty junction box on screen and reads it as "no condition yet", not as "always".
            throw new RuleDefinitionException("A logical expression must have at least one operand.");
        }

        return new RuleLogicalExpression
        {
            Operator = ParseEnum<RuleLogicalOperator>(GetString(obj, "op"), "logical operator"),
            Operands = operands
        };
    }

    private static JsonNode WriteExpression(RuleExpression expression)
    {
        if (expression == null)
        {
            throw new RuleDefinitionException("Cannot serialize a null expression.");
        }

        var obj = new JsonObject { ["kind"] = expression.Kind };

        switch (expression)
        {
            case RuleConstantExpression constant:
                obj["dataType"] = constant.DataType.ToString();
                obj["value"] = constant.Value;
                break;

            case RuleFieldExpression field:
                obj["parameter"] = field.Parameter;
                obj["field"] = field.Field;
                break;

            case RuleVariableExpression variable:
                obj["name"] = variable.Name;
                break;

            case RuleFunctionExpression function:
                obj["name"] = function.Name;
                obj["args"] = WriteExpressions(function.Args);
                break;

            case RuleComparisonExpression comparison:
                obj["op"] = comparison.Operator.ToString();
                obj["left"] = WriteExpression(comparison.Left);
                if (comparison.Right != null)
                {
                    obj["right"] = WriteExpression(comparison.Right);
                }

                break;

            case RuleLogicalExpression logical:
                obj["op"] = logical.Operator.ToString();
                obj["operands"] = WriteExpressions(logical.Operands);
                break;

            case RuleNotExpression not:
                obj["operand"] = WriteExpression(not.Operand);
                break;

            case RuleArithmeticExpression arithmetic:
                obj["op"] = arithmetic.Operator.ToString();
                obj["left"] = WriteExpression(arithmetic.Left);
                obj["right"] = WriteExpression(arithmetic.Right);
                break;

            case RuleListExpression list:
                obj["items"] = WriteExpressions(list.Items);
                break;

            default:
                throw new RuleDefinitionException(
                    "No serializer case for expression type " + expression.GetType().Name + ".");
        }

        return obj;
    }

    private static JsonArray WriteExpressions(IEnumerable<RuleExpression> expressions)
    {
        var array = new JsonArray();

        foreach (var expression in expressions ?? Enumerable.Empty<RuleExpression>())
        {
            array.Add(WriteExpression(expression));
        }

        return array;
    }

    /// <summary>
    /// A record action with no columns is allowed through here and rejected by the validator instead, which can
    /// say which table it was -- the serializer only knows the JSON is well formed.
    /// </summary>
    private static List<RuleRecordFieldValue> ParseRecordValues(JsonNode node, string what)
    {
        var values = new List<RuleRecordFieldValue>();

        if (node == null)
        {
            return values;
        }

        if (node is not JsonArray array)
        {
            throw new RuleDefinitionException("The values of a " + what + " must be a JSON array.");
        }

        foreach (var item in array)
        {
            if (item is not JsonObject valueObject)
            {
                throw new RuleDefinitionException("Each value of a " + what + " must be a JSON object.");
            }

            values.Add(new RuleRecordFieldValue
            {
                Field = RequireString(valueObject, "field", what),
                Value = ParseExpression(RequireNode(valueObject, "value", what), 1)
            });
        }

        return values;
    }

    private static JsonArray WriteRecordValues(IEnumerable<RuleRecordFieldValue> values)
    {
        var array = new JsonArray();

        foreach (var value in values ?? Enumerable.Empty<RuleRecordFieldValue>())
        {
            array.Add(new JsonObject
            {
                ["field"] = value.Field,
                ["value"] = WriteExpression(value.Value)
            });
        }

        return array;
    }

    private static JsonArray WriteActions(IEnumerable<RuleAction> actions)
    {
        var array = new JsonArray();

        foreach (var action in actions ?? Enumerable.Empty<RuleAction>())
        {
            var obj = new JsonObject { ["kind"] = action.Kind };

            switch (action)
            {
                case RuleSetAction set:
                    obj["parameter"] = set.Parameter;
                    obj["field"] = set.Field;
                    obj["value"] = WriteExpression(set.Value);
                    break;

                case RuleErrorAction error:
                    obj["messageKey"] = error.MessageKey;
                    obj["args"] = WriteExpressions(error.Args);
                    break;

                case RuleCreateRecordAction create:
                    obj["table"] = create.Table;
                    obj["values"] = WriteRecordValues(create.Values);
                    break;

                case RuleUpdateRecordAction update:
                    obj["table"] = update.Table;
                    obj["recordId"] = WriteExpression(update.RecordId);
                    obj["values"] = WriteRecordValues(update.Values);
                    break;

                default:
                    throw new RuleDefinitionException(
                        "No serializer case for action type " + action.GetType().Name + ".");
            }

            array.Add(obj);
        }

        return array;
    }

    private static TEnum ParseEnum<TEnum>(string value, string what) where TEnum : struct
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new RuleDefinitionException("Missing " + what + ".");
        }

        // Names only. Enum.TryParse happily accepts "1" and returns whichever member has that value, so the
        // numeric form has to be rejected explicitly -- otherwise renumbering an enum would silently change
        // the meaning of every rule already stored against it.
        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
        {
            throw new RuleDefinitionException(
                "The " + what + " must be given by name, not as the number " + value + ".");
        }

        if (!Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed) || !Enum.IsDefined(typeof(TEnum), parsed))
        {
            throw new RuleDefinitionException("Unknown " + what + ": " + value);
        }

        return parsed;
    }

    private static JsonNode RequireNode(JsonObject obj, string property, string what)
    {
        var node = obj[property];

        if (node == null)
        {
            throw new RuleDefinitionException("A " + what + " is missing its " + property + ".");
        }

        return node;
    }

    private static string RequireString(JsonObject obj, string property, string what)
    {
        var value = GetString(obj, property);

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new RuleDefinitionException("A " + what + " is missing its " + property + ".");
        }

        return value;
    }

    private static string GetString(JsonObject obj, string property)
    {
        var node = obj[property];

        if (node == null)
        {
            return null;
        }

        // Numbers and booleans reach us as strings too: the builder may post 100 or "100" for a constant
        // depending on which editor produced it, and both mean the same thing.
        return node.GetValueKind() == JsonValueKind.String
            ? node.GetValue<string>()
            : node.ToJsonString().Trim('"');
    }

    private static int? GetInt(JsonObject obj, string property)
    {
        var value = GetString(obj, property);

        return int.TryParse(value, out var parsed) ? parsed : null;
    }
}
