using System;
using System.Collections.Generic;
using System.Linq;
using Abp.Dependency;
using HCMv3.BusinessRules.Services;

namespace HCMv3.BusinessRules.Functions;

/// <summary>
/// The first argument that is not empty. The rule-authoring answer to "use the override if there is one,
/// otherwise the default", written once instead of as a nested if/then per fallback level.
/// </summary>
public class CoalesceRuleFunction : IRuleFunction, ITransientDependency
{
    public string Name => "Coalesce";

    public RuleFunctionDescriptor Descriptor => new(
        Name,
        "RuleFunction_Coalesce",
        RuleDataType.Unknown,
        new RuleFunctionParameter("first", RuleDataType.Unknown),
        new RuleFunctionParameter("second", RuleDataType.Unknown),
        new RuleFunctionParameter("third", RuleDataType.Unknown, isOptional: true));

    public object Invoke(IReadOnlyList<object> args, RuleExecutionContext context)
    {
        foreach (var arg in args)
        {
            if (arg == null)
            {
                continue;
            }

            // A blank string counts as empty. A configurer choosing between "the override" and "the
            // default" means a whitespace override to lose, not to win.
            if (arg is string text && string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            return arg;
        }

        return null;
    }
}

/// <summary>Joins values into one string. Empty arguments are skipped rather than producing double separators.</summary>
public class ConcatRuleFunction : IRuleFunction, ITransientDependency
{
    public string Name => "Concat";

    public RuleFunctionDescriptor Descriptor => new(
        Name,
        "RuleFunction_Concat",
        RuleDataType.String,
        new RuleFunctionParameter("first", RuleDataType.String),
        new RuleFunctionParameter("second", RuleDataType.String),
        new RuleFunctionParameter("third", RuleDataType.String, isOptional: true),
        new RuleFunctionParameter("fourth", RuleDataType.String, isOptional: true));

    public object Invoke(IReadOnlyList<object> args, RuleExecutionContext context)
    {
        var parts = args
            .Select(a => a as string ?? (a == null ? null : RuleValueConverter.FormatInvariant(a)))
            .Where(a => !string.IsNullOrWhiteSpace(a));

        return string.Join(" ", parts);
    }
}

/// <summary>Rounds a number to a given number of decimal places, away from zero at the midpoint.</summary>
public class RoundRuleFunction : IRuleFunction, ITransientDependency
{
    public string Name => "Round";

    public RuleFunctionDescriptor Descriptor => new(
        Name,
        "RuleFunction_Round",
        RuleDataType.Decimal,
        new RuleFunctionParameter("value", RuleDataType.Decimal),
        new RuleFunctionParameter("decimals", RuleDataType.Integer, isOptional: true));

    public object Invoke(IReadOnlyList<object> args, RuleExecutionContext context)
    {
        var value = RuleValueConverter.ToDecimal(args[0]);
        var decimals = args.Count > 1 && args[1] != null
            ? checked((int)RuleValueConverter.ToInteger(args[1]))
            : 0;

        if (decimals is < 0 or > 28)
        {
            throw new ArgumentOutOfRangeException(nameof(args), "Round accepts 0 to 28 decimal places.");
        }

        // AwayFromZero, not banker's rounding. A configurer writing Round(2.5) means 3; .NET's default
        // ToEven would give 2 and the resulting support ticket would be entirely deserved.
        return Math.Round(value, decimals, MidpointRounding.AwayFromZero);
    }
}

/// <summary>The larger of two numbers.</summary>
public class MaxNumberRuleFunction : IRuleFunction, ITransientDependency
{
    public string Name => "MaxNumber";

    public RuleFunctionDescriptor Descriptor => new(
        Name,
        "RuleFunction_MaxNumber",
        RuleDataType.Decimal,
        new RuleFunctionParameter("first", RuleDataType.Decimal),
        new RuleFunctionParameter("second", RuleDataType.Decimal));

    public object Invoke(IReadOnlyList<object> args, RuleExecutionContext context)
    {
        return Math.Max(RuleValueConverter.ToDecimal(args[0]), RuleValueConverter.ToDecimal(args[1]));
    }
}

/// <summary>The smaller of two numbers.</summary>
public class MinNumberRuleFunction : IRuleFunction, ITransientDependency
{
    public string Name => "MinNumber";

    public RuleFunctionDescriptor Descriptor => new(
        Name,
        "RuleFunction_MinNumber",
        RuleDataType.Decimal,
        new RuleFunctionParameter("first", RuleDataType.Decimal),
        new RuleFunctionParameter("second", RuleDataType.Decimal));

    public object Invoke(IReadOnlyList<object> args, RuleExecutionContext context)
    {
        return Math.Min(RuleValueConverter.ToDecimal(args[0]), RuleValueConverter.ToDecimal(args[1]));
    }
}

/// <summary>Absolute value.</summary>
public class AbsRuleFunction : IRuleFunction, ITransientDependency
{
    public string Name => "Abs";

    public RuleFunctionDescriptor Descriptor => new(
        Name,
        "RuleFunction_Abs",
        RuleDataType.Decimal,
        new RuleFunctionParameter("value", RuleDataType.Decimal));

    public object Invoke(IReadOnlyList<object> args, RuleExecutionContext context)
    {
        return Math.Abs(RuleValueConverter.ToDecimal(args[0]));
    }
}
