using System;
using System.Collections.Generic;
using System.Linq;
using Abp.Dependency;
using HCMv3.BusinessRules.Definition;
using HCMv3.BusinessRules.Records;

namespace HCMv3.BusinessRules.Services;

/// <inheritdoc cref="IRuleEvaluator"/>
/// <remarks>
/// <para>
/// A recursive tree walk, and nothing more. It has no repositories, no unit of work and no session -- the
/// only things it can touch are the parameter objects handed to it in the context and the functions in the
/// registry. That is what makes it safe to run configurer-authored logic inside a save, and it is why every
/// case in this class either produces a value or throws: there is no code path where an unrecognised node
/// is skipped, because a skipped condition silently widens a rule from "if the applier is a manager" to
/// "always".
/// </para>
///
/// <para>
/// NULL HANDLING is the part worth reading twice. A field nobody has filled in yet is normal, not an error,
/// so comparisons involving null answer false rather than throwing -- with the single exception of equality
/// against null, which answers truthfully, and the unary emptiness operators, which exist precisely to ask
/// the question. The consequence a rule author must understand is that both <c>X &gt; 5</c> and
/// <c>X &lt;= 5</c> are false when X is empty. That is the same answer SQL gives, and the alternative
/// (treating empty as zero) silently satisfies conditions nobody wrote.
/// </para>
/// </remarks>
public class RuleEvaluator : IRuleEvaluator, ITransientDependency
{
    private readonly IRuleFunctionRegistry _functionRegistry;

    public RuleEvaluator(IRuleFunctionRegistry functionRegistry)
    {
        _functionRegistry = functionRegistry;
    }

    public RuleEvaluationResult Evaluate(RuleDefinition definition, string ruleCode, RuleExecutionContext context)
    {
        if (definition == null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var result = new RuleEvaluationResult();

        context.CurrentRuleCode = ruleCode;

        // Variables are scoped to one rule. Clearing here rather than trusting the caller means a rule
        // cannot accidentally read a variable another rule left behind.
        context.ResetVariables();

        foreach (var variable in definition.Variables)
        {
            var value = Evaluate(variable.Value, context);

            context.SetVariable(variable.Name, Coerce(value, variable.DataType, ruleCode, "variable " + variable.Name));
        }

        foreach (var block in definition.Blocks)
        {
            // A block with no condition is unconditional -- that is how a plain derivation is written.
            var matched = block.If == null || EvaluateCondition(block.If, context);

            if (matched)
            {
                result.ConditionMatched = true;
            }

            var actions = matched ? block.Then : block.Else;

            foreach (var action in actions ?? new List<RuleAction>())
            {
                Apply(action, ruleCode, context, result);
            }
        }

        return result;
    }

    private void Apply(RuleAction action, string ruleCode, RuleExecutionContext context, RuleEvaluationResult result)
    {
        Consume(context);

        // Checked before the action runs, so a disallowed action has no partial effect. Arming already
        // refuses this combination; reaching here means the rule was edited after it was armed.
        if (context.AllowedActionKinds != null && !context.AllowedActionKinds.Contains(action.Kind))
        {
            throw new RuleExecutionException(
                ruleCode,
                "The rule performs a '" + action.Kind + "' action, which the " + context.EventType
                + " event does not allow.");
        }

        switch (action)
        {
            case RuleSetAction set:
                ApplySet(set, ruleCode, context, result);
                break;

            case RuleErrorAction error:
                var args = error.Args.Select(a => Evaluate(a, context)).ToArray();
                result.Errors.Add(new RuleRaisedError(ruleCode, error.MessageKey, args));
                context.Trace("raised " + error.MessageKey);
                break;

            case RuleCreateRecordAction create:
                StageRecordChange(ruleCode, context, result, RuleRecordChangeKind.Create, create.Table, null, create.Values);
                break;

            case RuleUpdateRecordAction update:
                StageRecordChange(ruleCode, context, result, RuleRecordChangeKind.Update, update.Table, update.RecordId, update.Values);
                break;

            default:
                throw new RuleExecutionException(
                    ruleCode,
                    "The rule uses an action type the engine does not implement: " + action.GetType().Name + ".");
        }
    }

    /// <summary>
    /// Evaluates a record action into a <see cref="RuleRecordChange"/> and hands it back -- nothing is written.
    ///
    /// <para>
    /// The values are evaluated here, in the rule, because that is where the context lives: afterwards the
    /// variables are gone. Checking that the table and columns exist and accept those values is left to the
    /// writer, which holds the data model; the validator has already caught every mistake it can see at
    /// authoring time.
    /// </para>
    /// </summary>
    private void StageRecordChange(
        string ruleCode,
        RuleExecutionContext context,
        RuleEvaluationResult result,
        RuleRecordChangeKind kind,
        string table,
        RuleExpression recordIdExpression,
        List<RuleRecordFieldValue> fieldValues)
    {
        long? recordId = null;

        if (kind == RuleRecordChangeKind.Update)
        {
            var idValue = Evaluate(recordIdExpression, context);

            if (idValue == null)
            {
                throw new RuleExecutionException(
                    ruleCode,
                    "The rule updates a record in '" + table + "' but the record id it names is empty.");
            }

            try
            {
                recordId = RuleValueConverter.ToInteger(idValue);
            }
            catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException)
            {
                throw new RuleExecutionException(
                    ruleCode,
                    "The rule updates a record in '" + table + "' but its record id is not a number.",
                    ex);
            }
        }

        var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        foreach (var fieldValue in fieldValues ?? new List<RuleRecordFieldValue>())
        {
            // Evaluated even on a dry run, for the same reason as a set: an expression that would throw in
            // production must throw in the test harness.
            values[fieldValue.Field] = Evaluate(fieldValue.Value, context);
        }

        var change = new RuleRecordChange(ruleCode, kind, table, recordId, values);

        if (!context.AllowWrites)
        {
            context.Trace("would " + change);
            return;
        }

        result.RecordChanges.Add(change);
        context.Trace(change.ToString());
    }

    private void ApplySet(RuleSetAction set, string ruleCode, RuleExecutionContext context, RuleEvaluationResult result)
    {
        var parameter = context.BaseObject.FindParameter(set.Parameter)
                        ?? throw new RuleExecutionException(
                            ruleCode,
                            "The rule assigns to '" + set.Parameter + "', which base object '"
                            + context.BaseObject.Code + "' does not expose.");

        var resolved = parameter.ResolvePath(set.Field)
                       ?? throw new RuleExecutionException(
                           ruleCode,
                           "The rule assigns to '" + set.Parameter + "." + set.Field + "', which does not exist.");

        // Reading through a reference is fine; writing through one is not. It would mean saving a second
        // entity as a side effect of saving the first -- a whole extra row changed by a rule that appears
        // to be editing this one.
        if (!resolved.IsDirect)
        {
            throw new RuleExecutionException(
                ruleCode,
                "The rule assigns to '" + set.Parameter + "." + set.Field
                + "', which belongs to a related record. Rules may read related records but not change them.");
        }

        var field = resolved.Field;

        // Both gates must open. A field may be writable in principle and still be unreachable through a
        // read-only parameter -- that is what keeps an "original record" companion parameter read-only even
        // though it wraps the same entity type as the live one.
        if (!parameter.IsWritable || !field.IsWritable)
        {
            throw new RuleExecutionException(
                ruleCode,
                "The rule assigns to '" + set.Parameter + "." + set.Field + "', which is read-only.");
        }

        // Evaluated even when writes are disallowed: a dry run whose value expressions never execute proves
        // nothing, and an expression that would have thrown in production must throw in the test harness.
        var value = Evaluate(set.Value, context);
        var coerced = Coerce(value, field.DataType, ruleCode, set.Parameter + "." + set.Field);

        if (!context.AllowWrites)
        {
            context.Trace("would set " + set.Parameter + "." + set.Field + " = " + RuleValueConverter.FormatInvariant(coerced));
            return;
        }

        var target = context.GetParameter(set.Parameter);

        if (target == null)
        {
            throw new RuleExecutionException(
                ruleCode,
                "The rule assigns to '" + set.Parameter + "." + set.Field + "' but that parameter is empty.");
        }

        try
        {
            field.Setter(target, coerced);
        }
        catch (Exception ex) when (ex is InvalidCastException or OverflowException or FormatException)
        {
            throw new RuleExecutionException(
                ruleCode,
                "The rule could not assign " + RuleValueConverter.FormatInvariant(coerced) + " to '"
                + set.Parameter + "." + set.Field + "': " + ex.Message,
                ex);
        }

        var write = new RuleFieldWrite(ruleCode, set.Parameter, set.Field, coerced);

        result.Writes.Add(write);
        context.Writes.Add(write);
        context.Trace(write.ToString());
    }

    private bool EvaluateCondition(RuleExpression expression, RuleExecutionContext context)
    {
        var value = Evaluate(expression, context);

        if (value == null)
        {
            // An empty condition is not a match. Reaching here means an author compared something to a
            // field that is null; treating that as true would fire the rule on exactly the records it has
            // the least information about.
            return false;
        }

        try
        {
            return RuleValueConverter.ToBoolean(value);
        }
        catch (InvalidCastException ex)
        {
            throw new RuleExecutionException(
                context.CurrentRuleCode,
                "A rule condition produced " + RuleValueConverter.FormatInvariant(value)
                + ", which is not a true/false value.",
                ex);
        }
    }

    private object Evaluate(RuleExpression expression, RuleExecutionContext context)
    {
        if (expression == null)
        {
            return null;
        }

        Consume(context);

        switch (expression)
        {
            case RuleConstantExpression constant:
                return RuleValueConverter.ParseLiteral(constant.Value, constant.DataType);

            case RuleFieldExpression field:
                return EvaluateField(field, context);

            case RuleVariableExpression variable:
                if (!context.TryGetVariable(variable.Name, out var variableValue))
                {
                    throw new RuleExecutionException(
                        context.CurrentRuleCode,
                        "The rule reads the variable '" + variable.Name + "' before it is defined.");
                }

                return variableValue;

            case RuleFunctionExpression function:
                return EvaluateFunction(function, context);

            case RuleComparisonExpression comparison:
                return EvaluateComparison(comparison, context);

            case RuleLogicalExpression logical:
                return EvaluateLogical(logical, context);

            case RuleNotExpression not:
                return !EvaluateCondition(not.Operand, context);

            case RuleArithmeticExpression arithmetic:
                return EvaluateArithmetic(arithmetic, context);

            case RuleListExpression list:
                return list.Items.Select(i => Evaluate(i, context)).ToList();

            default:
                throw new RuleExecutionException(
                    context.CurrentRuleCode,
                    "The rule uses an expression type the engine does not implement: " + expression.GetType().Name + ".");
        }
    }

    private static object EvaluateField(RuleFieldExpression expression, RuleExecutionContext context)
    {
        var parameter = context.BaseObject.FindParameter(expression.Parameter)
                        ?? throw new RuleExecutionException(
                            context.CurrentRuleCode,
                            "The rule reads '" + expression.Parameter + "', which base object '"
                            + context.BaseObject.Code + "' does not expose.");

        // Resolves the whole dotted path, so "Employee.Nationality.NameEn" is followed reference by
        // reference. A one-segment path resolves with no references and behaves exactly as it always did.
        var resolved = parameter.ResolvePath(expression.Field)
                       ?? throw new RuleExecutionException(
                           context.CurrentRuleCode,
                           "The rule reads '" + expression + "', which does not exist.");

        var target = context.GetParameter(expression.Parameter);

        // A null parameter yields null fields rather than throwing. "The original record does not exist
        // because this is an insert" is an ordinary thing for a rule to branch on.
        if (target == null)
        {
            return null;
        }

        var owner = context.ResolveOwner(resolved, target);

        // An unset reference anywhere along the path reads as empty, for the same reason a null parameter
        // does: an employee with no nationality is data, not a fault.
        if (owner == null)
        {
            return null;
        }

        return RuleValueConverter.Normalize(resolved.Field.Getter(owner));
    }

    private object EvaluateFunction(RuleFunctionExpression expression, RuleExecutionContext context)
    {
        var function = _functionRegistry.Find(expression.Name)
                       ?? throw new RuleExecutionException(
                           context.CurrentRuleCode,
                           "The rule calls '" + expression.Name + "', which is not a registered rule function.");

        var descriptor = function.Descriptor;

        if (expression.Args.Count < descriptor.RequiredParameterCount
            || expression.Args.Count > descriptor.Parameters.Count)
        {
            throw new RuleExecutionException(
                context.CurrentRuleCode,
                "The rule calls " + descriptor.Name + " with " + expression.Args.Count
                + " argument(s); it takes " + descriptor.RequiredParameterCount + " to "
                + descriptor.Parameters.Count + ".");
        }

        var args = new List<object>(expression.Args.Count);

        for (var i = 0; i < expression.Args.Count; i++)
        {
            var value = Evaluate(expression.Args[i], context);

            args.Add(Coerce(
                value,
                descriptor.Parameters[i].DataType,
                context.CurrentRuleCode,
                descriptor.Name + " argument '" + descriptor.Parameters[i].Name + "'"));
        }

        try
        {
            return RuleValueConverter.Normalize(function.Invoke(args, context));
        }
        catch (RuleExecutionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // A function throwing is a broken rule, not a broken engine, and the message has to say which
            // function so the author can fix the call rather than filing a bug against the platform.
            throw new RuleExecutionException(
                context.CurrentRuleCode,
                "The rule function " + descriptor.Name + " failed: " + ex.Message,
                ex);
        }
    }

    private object EvaluateLogical(RuleLogicalExpression expression, RuleExecutionContext context)
    {
        switch (expression.Operator)
        {
            case RuleLogicalOperator.And:
                foreach (var operand in expression.Operands)
                {
                    // Short-circuits, so an And can safely lead with a null guard and follow it with an
                    // expression that would fail on null.
                    if (!EvaluateCondition(operand, context))
                    {
                        return false;
                    }
                }

                return true;

            case RuleLogicalOperator.Or:
                foreach (var operand in expression.Operands)
                {
                    if (EvaluateCondition(operand, context))
                    {
                        return true;
                    }
                }

                return false;

            default:
                throw new RuleExecutionException(
                    context.CurrentRuleCode,
                    "The rule uses an unsupported logical operator.");
        }
    }

    private object EvaluateComparison(RuleComparisonExpression expression, RuleExecutionContext context)
    {
        var left = Evaluate(expression.Left, context);

        switch (expression.Operator)
        {
            case RuleComparisonOperator.IsEmpty:
                return IsEmpty(left);

            case RuleComparisonOperator.IsNotEmpty:
                return !IsEmpty(left);
        }

        var right = Evaluate(expression.Right, context);

        switch (expression.Operator)
        {
            case RuleComparisonOperator.Equal:
                return AreEqual(left, right);

            case RuleComparisonOperator.NotEqual:
                return !AreEqual(left, right);

            case RuleComparisonOperator.GreaterThan:
                return TryCompare(left, right) > 0;

            case RuleComparisonOperator.GreaterThanOrEqual:
                return TryCompare(left, right) >= 0;

            case RuleComparisonOperator.LessThan:
                return TryCompare(left, right) < 0;

            case RuleComparisonOperator.LessThanOrEqual:
                return TryCompare(left, right) <= 0;

            case RuleComparisonOperator.Contains:
                return TextOf(left).Contains(TextOf(right), StringComparison.OrdinalIgnoreCase);

            case RuleComparisonOperator.StartsWith:
                return TextOf(left).StartsWith(TextOf(right), StringComparison.OrdinalIgnoreCase);

            case RuleComparisonOperator.EndsWith:
                return TextOf(left).EndsWith(TextOf(right), StringComparison.OrdinalIgnoreCase);

            case RuleComparisonOperator.In:
                return IsIn(left, right);

            case RuleComparisonOperator.NotIn:
                return !IsIn(left, right);

            default:
                throw new RuleExecutionException(
                    context.CurrentRuleCode,
                    "The rule uses an unsupported comparison operator.");
        }
    }

    private object EvaluateArithmetic(RuleArithmeticExpression expression, RuleExecutionContext context)
    {
        var left = Evaluate(expression.Left, context);
        var right = Evaluate(expression.Right, context);

        if (left == null || right == null)
        {
            // Arithmetic on an unfilled field yields nothing rather than treating it as zero. A rule that
            // wants zero can say so with Coalesce.
            return null;
        }

        // Date minus date is a day count -- the one arithmetic shape where the operand types differ from
        // the result type, and common enough in leave rules to be worth special-casing rather than forcing
        // every author through GetNumberOfCalendarDays.
        if (left is DateTime leftDate && right is DateTime rightDate)
        {
            if (expression.Operator != RuleArithmeticOperator.Subtract)
            {
                throw new RuleExecutionException(
                    context.CurrentRuleCode,
                    "Only subtraction is defined between two dates.");
            }

            return (long)(leftDate.Date - rightDate.Date).TotalDays;
        }

        // Date plus/minus a number shifts by days.
        if (left is DateTime baseDate)
        {
            var days = RuleValueConverter.ToDecimal(right);

            return expression.Operator switch
            {
                RuleArithmeticOperator.Add => baseDate.AddDays((double)days),
                RuleArithmeticOperator.Subtract => baseDate.AddDays(-(double)days),
                _ => throw new RuleExecutionException(
                    context.CurrentRuleCode,
                    "Only addition and subtraction are defined between a date and a number.")
            };
        }

        decimal leftNumber;
        decimal rightNumber;

        try
        {
            leftNumber = RuleValueConverter.ToDecimal(left);
            rightNumber = RuleValueConverter.ToDecimal(right);
        }
        catch (InvalidCastException ex)
        {
            throw new RuleExecutionException(
                context.CurrentRuleCode,
                "The rule performs arithmetic on a value that is not a number: " + ex.Message,
                ex);
        }

        switch (expression.Operator)
        {
            case RuleArithmeticOperator.Add:
                return Denormalize(leftNumber + rightNumber, left, right);

            case RuleArithmeticOperator.Subtract:
                return Denormalize(leftNumber - rightNumber, left, right);

            case RuleArithmeticOperator.Multiply:
                return Denormalize(leftNumber * rightNumber, left, right);

            case RuleArithmeticOperator.Divide:
                if (rightNumber == 0m)
                {
                    // Named rather than a bare DivideByZeroException: the author needs to know which rule.
                    throw new RuleExecutionException(context.CurrentRuleCode, "The rule divides by zero.");
                }

                return leftNumber / rightNumber;

            case RuleArithmeticOperator.Modulo:
                if (rightNumber == 0m)
                {
                    throw new RuleExecutionException(context.CurrentRuleCode, "The rule takes a remainder modulo zero.");
                }

                return Denormalize(leftNumber % rightNumber, left, right);

            default:
                throw new RuleExecutionException(
                    context.CurrentRuleCode,
                    "The rule uses an unsupported arithmetic operator.");
        }
    }

    /// <summary>
    /// Keeps whole-number arithmetic whole. Without this, 5 + 5 would come back as a decimal and an
    /// integer field assignment would round-trip through a conversion it did not need.
    /// </summary>
    private static object Denormalize(decimal value, object left, object right)
    {
        if (left is long && right is long && decimal.Truncate(value) == value)
        {
            return decimal.ToInt64(value);
        }

        return value;
    }

    private static bool IsEmpty(object value)
    {
        return value switch
        {
            null => true,
            string text => string.IsNullOrWhiteSpace(text),
            _ => false
        };
    }

    private static bool AreEqual(object left, object right)
    {
        if (left == null && right == null)
        {
            return true;
        }

        if (left == null || right == null)
        {
            return false;
        }

        var comparison = TryCompare(left, right);

        if (comparison.HasValue)
        {
            return comparison.Value == 0;
        }

        // Last resort for values that will not order against each other -- comparing their canonical text
        // is forgiving in the direction an author expects, e.g. a lookup id typed as text.
        return string.Equals(
            RuleValueConverter.FormatInvariant(left),
            RuleValueConverter.FormatInvariant(right),
            StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Orders two values, or returns null when they cannot be ordered. Null operands are deliberately
    /// incomparable, which makes every ordering operator false against an empty field.
    /// </summary>
    private static int? TryCompare(object left, object right)
    {
        if (left == null || right == null)
        {
            return null;
        }

        try
        {
            if (left is DateTime || right is DateTime)
            {
                return RuleValueConverter.ToDateTime(left).CompareTo(RuleValueConverter.ToDateTime(right));
            }

            if (left is bool || right is bool)
            {
                return RuleValueConverter.ToBoolean(left).CompareTo(RuleValueConverter.ToBoolean(right));
            }

            if (left is long or decimal || right is long or decimal)
            {
                return RuleValueConverter.ToDecimal(left).CompareTo(RuleValueConverter.ToDecimal(right));
            }

            return string.Compare(TextOf(left), TextOf(right), StringComparison.OrdinalIgnoreCase);
        }
        catch (InvalidCastException)
        {
            return null;
        }
    }

    private static bool IsIn(object value, object candidates)
    {
        if (value == null)
        {
            return false;
        }

        if (candidates is IEnumerable<object> list)
        {
            return list.Any(candidate => AreEqual(value, candidate));
        }

        // A single value on the right is treated as a one-item list rather than rejected: the authoring UI
        // can produce either shape depending on how the author built the condition.
        return AreEqual(value, candidates);
    }

    private static string TextOf(object value)
    {
        return value == null ? string.Empty : value as string ?? RuleValueConverter.FormatInvariant(value);
    }

    private static object Coerce(object value, RuleDataType dataType, string ruleCode, string what)
    {
        try
        {
            return RuleValueConverter.Coerce(value, dataType);
        }
        catch (Exception ex) when (ex is InvalidCastException or OverflowException or FormatException)
        {
            throw new RuleExecutionException(
                ruleCode,
                "The rule could not read " + RuleValueConverter.FormatInvariant(value) + " as " + dataType
                + " for " + what + ".",
                ex);
        }
    }

    /// <summary>
    /// Spends one step of the execution budget. The budget is shared across every rule on an event, so a
    /// pathological rule cannot hide by being split in two, and a save can never be held open indefinitely
    /// by configuration alone.
    /// </summary>
    private static void Consume(RuleExecutionContext context)
    {
        if (--context.RemainingSteps < 0)
        {
            throw new RuleExecutionException(
                context.CurrentRuleCode,
                "Rule execution exceeded " + BusinessRuleConsts.MaxExecutionSteps + " steps and was stopped.");
        }
    }
}
