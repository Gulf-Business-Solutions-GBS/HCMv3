using System;
using System.Collections.Generic;
using Abp.Dependency;
using HCMv3.BusinessRules.Definition;
using HCMv3.BusinessRules.Records;

namespace HCMv3.BusinessRules.Services;

/// <summary>
/// Checks a parsed rule against the registries: do the fields exist, are the functions registered, are the
/// arities right, is every assignment target actually writable.
///
/// <para>
/// The parser answers "is this well-formed JSON in the shape of a rule". This answers "could this rule
/// actually run". Both run when a rule is saved, so a configurer learns about a typo at the authoring
/// screen where they can fix it, instead of at 2am when the rule finally fires on the one record that
/// reaches that branch.
/// </para>
/// </summary>
public interface IRuleDefinitionValidator
{
    /// <summary>
    /// Returns every problem found, empty when the rule is sound. Collects rather than throwing on the
    /// first: an author fixing one mistake per save round-trip is a bad screen.
    /// </summary>
    IReadOnlyList<string> Validate(RuleDefinition definition, RuleBaseObjectDescriptor baseObject, RulePurpose purpose);
}

/// <inheritdoc cref="IRuleDefinitionValidator"/>
public class RuleDefinitionValidator : IRuleDefinitionValidator, ITransientDependency
{
    private readonly IRuleFunctionRegistry _functionRegistry;
    private readonly IRuleRecordSchema _recordSchema;

    public RuleDefinitionValidator(IRuleFunctionRegistry functionRegistry, IRuleRecordSchema recordSchema)
    {
        _functionRegistry = functionRegistry;
        _recordSchema = recordSchema;
    }

    public IReadOnlyList<string> Validate(
        RuleDefinition definition,
        RuleBaseObjectDescriptor baseObject,
        RulePurpose purpose)
    {
        var errors = new List<string>();

        if (definition == null)
        {
            errors.Add("The rule has no definition.");
            return errors;
        }

        if (baseObject == null)
        {
            errors.Add("The rule names a base object that does not exist.");
            return errors;
        }

        var declaredVariables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var variable in definition.Variables)
        {
            if (string.IsNullOrWhiteSpace(variable.Name))
            {
                errors.Add("A variable has no name.");
                continue;
            }

            // Checked BEFORE the variable is declared, so a variable cannot reference itself. Self-reference
            // would not loop -- the evaluator reads variables that already exist -- but it would silently
            // read the previous rule's value, which is worse than an error.
            ValidateExpression(variable.Value, baseObject, declaredVariables, errors);

            if (!declaredVariables.Add(variable.Name))
            {
                errors.Add("The variable '" + variable.Name + "' is declared more than once.");
            }
        }

        foreach (var block in definition.Blocks)
        {
            if (block.If != null)
            {
                ValidateExpression(block.If, baseObject, declaredVariables, errors);
            }

            ValidateActions(block.Then, baseObject, declaredVariables, purpose, errors);
            ValidateActions(block.Else, baseObject, declaredVariables, purpose, errors);
        }

        return errors;
    }

    private void ValidateActions(
        IEnumerable<RuleAction> actions,
        RuleBaseObjectDescriptor baseObject,
        HashSet<string> variables,
        RulePurpose purpose,
        List<string> errors)
    {
        if (actions == null)
        {
            return;
        }

        foreach (var action in actions)
        {
            switch (action)
            {
                case RuleSetAction set:
                    ValidateSet(set, baseObject, variables, purpose, errors);
                    break;

                case RuleErrorAction error:
                    if (string.IsNullOrWhiteSpace(error.MessageKey))
                    {
                        errors.Add("An error action has no message key.");
                    }

                    foreach (var arg in error.Args)
                    {
                        ValidateExpression(arg, baseObject, variables, errors);
                    }

                    break;

                case RuleCreateRecordAction create:
                    ValidateRecordAction(create.Table, null, create.Values, isUpdate: false, baseObject, variables, purpose, errors);
                    break;

                case RuleUpdateRecordAction update:
                    ValidateRecordAction(update.Table, update.RecordId, update.Values, isUpdate: true, baseObject, variables, purpose, errors);
                    break;

                default:
                    errors.Add("Unsupported action type: " + action.GetType().Name + ".");
                    break;
            }
        }
    }

    private void ValidateSet(
        RuleSetAction set,
        RuleBaseObjectDescriptor baseObject,
        HashSet<string> variables,
        RulePurpose purpose,
        List<string> errors)
    {
        if (purpose == RulePurpose.Validate)
        {
            // Caught here rather than silently dropped at run time. A validation rule containing an
            // assignment means the author misunderstood the purpose field, and telling them beats letting
            // them ship a rule whose Then branch does nothing.
            errors.Add(
                "'" + set.Parameter + "." + set.Field
                + "' is assigned by a rule whose purpose is Validate. Validation rules may only raise errors.");
        }

        var parameter = baseObject.FindParameter(set.Parameter);

        if (parameter == null)
        {
            errors.Add("'" + set.Parameter + "' is not a parameter of base object '" + baseObject.Code + "'.");
            return;
        }

        var resolved = parameter.ResolvePath(set.Field);

        if (resolved == null)
        {
            errors.Add("'" + set.Parameter + "." + set.Field + "' does not exist.");
        }
        else if (!resolved.IsDirect)
        {
            // Caught here so the author is told at the authoring screen, not when the rule first fires.
            errors.Add(
                "'" + set.Parameter + "." + set.Field
                + "' belongs to a related record. Rules may read related records but not change them.");
        }
        else if (!parameter.IsWritable || !resolved.Field.IsWritable)
        {
            errors.Add("'" + set.Parameter + "." + set.Field + "' is read-only and cannot be assigned.");
        }

        ValidateExpression(set.Value, baseObject, variables, errors);
    }

    /// <summary>
    /// Everything about a record action that can be known before it runs: the table and every column exist and
    /// are writable, no column is set twice, and an update names its row. Whether a VALUE fits its column is
    /// checked when the change is applied, because most values are only known then.
    /// </summary>
    private void ValidateRecordAction(
        string tableName,
        RuleExpression recordId,
        List<RuleRecordFieldValue> values,
        bool isUpdate,
        RuleBaseObjectDescriptor baseObject,
        HashSet<string> variables,
        RulePurpose purpose,
        List<string> errors)
    {
        var what = (isUpdate ? "An update" : "A create") + " of '" + tableName + "'";

        if (purpose == RulePurpose.Validate)
        {
            // Same reasoning as a set: the runner turns writes off for validation rules, so this would silently
            // do nothing. Better the author hears it now.
            errors.Add(what + " belongs to a rule whose purpose is Validate. Validation rules may only raise errors.");
        }

        if (string.IsNullOrWhiteSpace(tableName))
        {
            errors.Add((isUpdate ? "An update" : "A create") + " record action has no table.");
            return;
        }

        var table = _recordSchema.FindTable(tableName);

        if (table == null)
        {
            errors.Add(
                "'" + tableName + "' is not a table rules can write. It does not exist, or it is a system table "
                + "rules are not allowed to change.");
            return;
        }

        if (isUpdate)
        {
            if (recordId == null)
            {
                errors.Add(what + " does not say which record to update.");
            }
            else
            {
                ValidateExpression(recordId, baseObject, variables, errors);
            }
        }

        if (values == null || values.Count == 0)
        {
            errors.Add(what + " does not set any fields.");
            return;
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value.Field))
            {
                errors.Add(what + " has a value with no field.");
                continue;
            }

            if (table.FindColumn(value.Field) == null)
            {
                errors.Add(
                    "'" + table.Name + "." + value.Field + "' is not a field rules can set. It does not exist, or it "
                    + "is a system field such as Id, TenantId or an audit stamp.");
            }

            if (!seen.Add(value.Field))
            {
                errors.Add(what + " sets '" + value.Field + "' more than once.");
            }

            ValidateExpression(value.Value, baseObject, variables, errors);
        }
    }

    private void ValidateExpression(
        RuleExpression expression,
        RuleBaseObjectDescriptor baseObject,
        HashSet<string> variables,
        List<string> errors)
    {
        switch (expression)
        {
            case null:
                errors.Add("An expression is missing.");
                return;

            case RuleConstantExpression:
                return;

            case RuleFieldExpression field:
                var parameter = baseObject.FindParameter(field.Parameter);

                if (parameter == null)
                {
                    errors.Add("'" + field.Parameter + "' is not a parameter of base object '" + baseObject.Code + "'.");
                }
                else if (parameter.ResolvePath(field.Field) == null)
                {
                    errors.Add("'" + field + "' does not exist.");
                }

                return;

            case RuleVariableExpression variable:
                if (!variables.Contains(variable.Name))
                {
                    errors.Add("The variable '" + variable.Name + "' is used before it is defined.");
                }

                return;

            case RuleFunctionExpression function:
                ValidateFunction(function, baseObject, variables, errors);
                return;

            case RuleComparisonExpression comparison:
                ValidateExpression(comparison.Left, baseObject, variables, errors);

                if (comparison.Operator is not (RuleComparisonOperator.IsEmpty or RuleComparisonOperator.IsNotEmpty))
                {
                    ValidateExpression(comparison.Right, baseObject, variables, errors);
                }

                return;

            case RuleLogicalExpression logical:
                foreach (var operand in logical.Operands)
                {
                    ValidateExpression(operand, baseObject, variables, errors);
                }

                return;

            case RuleNotExpression not:
                ValidateExpression(not.Operand, baseObject, variables, errors);
                return;

            case RuleArithmeticExpression arithmetic:
                ValidateExpression(arithmetic.Left, baseObject, variables, errors);
                ValidateExpression(arithmetic.Right, baseObject, variables, errors);
                return;

            case RuleListExpression list:
                foreach (var item in list.Items)
                {
                    ValidateExpression(item, baseObject, variables, errors);
                }

                return;

            default:
                errors.Add("Unsupported expression type: " + expression.GetType().Name + ".");
                return;
        }
    }

    private void ValidateFunction(
        RuleFunctionExpression function,
        RuleBaseObjectDescriptor baseObject,
        HashSet<string> variables,
        List<string> errors)
    {
        var registered = _functionRegistry.Find(function.Name);

        if (registered == null)
        {
            errors.Add("'" + function.Name + "' is not a registered rule function.");
        }
        else
        {
            var descriptor = registered.Descriptor;

            if (function.Args.Count < descriptor.RequiredParameterCount
                || function.Args.Count > descriptor.Parameters.Count)
            {
                errors.Add(
                    descriptor.Name + " takes " + descriptor.RequiredParameterCount + " to "
                    + descriptor.Parameters.Count + " argument(s) but is called with " + function.Args.Count + ".");
            }
        }

        foreach (var arg in function.Args)
        {
            ValidateExpression(arg, baseObject, variables, errors);
        }
    }
}
