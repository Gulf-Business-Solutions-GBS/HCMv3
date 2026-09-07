using System;
using System.Collections.Generic;
using Abp.Dependency;
using HCMv3.BusinessRules.Definition;

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

    public RuleDefinitionValidator(IRuleFunctionRegistry functionRegistry)
    {
        _functionRegistry = functionRegistry;
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

        var field = parameter.FindField(set.Field);

        if (field == null)
        {
            errors.Add("'" + set.Parameter + "." + set.Field + "' does not exist.");
        }
        else if (!parameter.IsWritable || !field.IsWritable)
        {
            errors.Add("'" + set.Parameter + "." + set.Field + "' is read-only and cannot be assigned.");
        }

        ValidateExpression(set.Value, baseObject, variables, errors);
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
                else if (parameter.FindField(field.Field) == null)
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
