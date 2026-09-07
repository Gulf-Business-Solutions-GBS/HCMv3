using System;
using System.Collections.Generic;
using Abp.Dependency;
using HCMv3.BusinessRules.Services;

namespace HCMv3.BusinessRules.Functions;

/// <summary>
/// The execution date, as a date with no time component.
///
/// <para>
/// Reads <c>RuleExecutionContext.ExecutionDate</c> rather than <c>DateTime.Today</c>. That indirection is
/// the whole reason this is a function and not a constant: it makes every date-dependent rule testable
/// against a fixed date, and stops a batch that runs across midnight from evaluating its first and last
/// rows differently.
/// </para>
/// </summary>
public class TodayRuleFunction : IRuleFunction, ITransientDependency
{
    public string Name => "Today";

    public RuleFunctionDescriptor Descriptor => new(Name, "RuleFunction_Today", RuleDataType.Date);

    public object Invoke(IReadOnlyList<object> args, RuleExecutionContext context)
    {
        return context.ExecutionDate;
    }
}

/// <summary>
/// Adds a number of calendar days to a date. The probation-end-date rule is built on this.
///
/// <para>
/// Calendar days, not working days -- weekends and holidays are included. The working-day equivalent needs
/// the duty period and holiday calendar and is a separate function, so that a rule which says "calendar"
/// cannot quietly start meaning "working" when somebody configures a calendar.
/// </para>
/// </summary>
public class AddCalendarDaysRuleFunction : IRuleFunction, ITransientDependency
{
    public string Name => "AddCalendarDays";

    public RuleFunctionDescriptor Descriptor => new(
        Name,
        "RuleFunction_AddCalendarDays",
        RuleDataType.Date,
        new RuleFunctionParameter("date", RuleDataType.Date),
        new RuleFunctionParameter("days", RuleDataType.Integer));

    public object Invoke(IReadOnlyList<object> args, RuleExecutionContext context)
    {
        var date = RuleValueConverter.ToDateTime(args[0]);
        var days = RuleValueConverter.ToInteger(args[1]);

        return date.AddDays(days);
    }
}

/// <summary>Adds a number of months, clamping to the end of a short month the way <c>DateTime.AddMonths</c> does.</summary>
public class AddMonthsRuleFunction : IRuleFunction, ITransientDependency
{
    public string Name => "AddMonths";

    public RuleFunctionDescriptor Descriptor => new(
        Name,
        "RuleFunction_AddMonths",
        RuleDataType.Date,
        new RuleFunctionParameter("date", RuleDataType.Date),
        new RuleFunctionParameter("months", RuleDataType.Integer));

    public object Invoke(IReadOnlyList<object> args, RuleExecutionContext context)
    {
        var date = RuleValueConverter.ToDateTime(args[0]);
        var months = checked((int)RuleValueConverter.ToInteger(args[1]));

        return date.AddMonths(months);
    }
}

/// <summary>Adds a number of years.</summary>
public class AddYearsRuleFunction : IRuleFunction, ITransientDependency
{
    public string Name => "AddYears";

    public RuleFunctionDescriptor Descriptor => new(
        Name,
        "RuleFunction_AddYears",
        RuleDataType.Date,
        new RuleFunctionParameter("date", RuleDataType.Date),
        new RuleFunctionParameter("years", RuleDataType.Integer));

    public object Invoke(IReadOnlyList<object> args, RuleExecutionContext context)
    {
        var date = RuleValueConverter.ToDateTime(args[0]);
        var years = checked((int)RuleValueConverter.ToInteger(args[1]));

        return date.AddYears(years);
    }
}

/// <summary>
/// Days between two dates, counting BOTH ends -- a single-day absence is 1, not 0.
///
/// <para>
/// This is the same convention the SuccessFactors function of this name uses, and the same one every leave
/// calculation in this system already uses, which is the point: a configurer copying a rule from a policy
/// document written against SuccessFactors gets the answer the document intended. The alternative
/// convention (a plain subtraction) is available as ordinary arithmetic for anyone who wants it.
/// </para>
/// </summary>
public class GetNumberOfCalendarDaysRuleFunction : IRuleFunction, ITransientDependency
{
    public string Name => "GetNumberOfCalendarDays";

    public RuleFunctionDescriptor Descriptor => new(
        Name,
        "RuleFunction_GetNumberOfCalendarDays",
        RuleDataType.Integer,
        new RuleFunctionParameter("startDate", RuleDataType.Date),
        new RuleFunctionParameter("endDate", RuleDataType.Date));

    public object Invoke(IReadOnlyList<object> args, RuleExecutionContext context)
    {
        var start = RuleValueConverter.ToDateTime(args[0]).Date;
        var end = RuleValueConverter.ToDateTime(args[1]).Date;

        // A reversed range is a data problem, not a negative count. Returning a negative number here would
        // silently satisfy a "less than 7 days" condition for a range entered backwards.
        if (end < start)
        {
            return 0L;
        }

        return (long)(end - start).TotalDays + 1;
    }
}

/// <summary>Whole years between a date and the execution date. Birthdays and service length.</summary>
public class GetAgeInYearsRuleFunction : IRuleFunction, ITransientDependency
{
    public string Name => "GetAgeInYears";

    public RuleFunctionDescriptor Descriptor => new(
        Name,
        "RuleFunction_GetAgeInYears",
        RuleDataType.Integer,
        new RuleFunctionParameter("date", RuleDataType.Date));

    public object Invoke(IReadOnlyList<object> args, RuleExecutionContext context)
    {
        var from = RuleValueConverter.ToDateTime(args[0]).Date;
        var to = context.ExecutionDate;

        var years = to.Year - from.Year;

        // Not yet reached this year's anniversary: someone born in December is not a year older in January.
        if (from.Date > to.AddYears(-years))
        {
            years--;
        }

        return (long)Math.Max(0, years);
    }
}

/// <summary>The later of two dates.</summary>
public class MaxDateRuleFunction : IRuleFunction, ITransientDependency
{
    public string Name => "MaxDate";

    public RuleFunctionDescriptor Descriptor => new(
        Name,
        "RuleFunction_MaxDate",
        RuleDataType.Date,
        new RuleFunctionParameter("first", RuleDataType.Date),
        new RuleFunctionParameter("second", RuleDataType.Date));

    public object Invoke(IReadOnlyList<object> args, RuleExecutionContext context)
    {
        var first = RuleValueConverter.ToDateTime(args[0]);
        var second = RuleValueConverter.ToDateTime(args[1]);

        return first >= second ? first : second;
    }
}

/// <summary>The earlier of two dates.</summary>
public class MinDateRuleFunction : IRuleFunction, ITransientDependency
{
    public string Name => "MinDate";

    public RuleFunctionDescriptor Descriptor => new(
        Name,
        "RuleFunction_MinDate",
        RuleDataType.Date,
        new RuleFunctionParameter("first", RuleDataType.Date),
        new RuleFunctionParameter("second", RuleDataType.Date));

    public object Invoke(IReadOnlyList<object> args, RuleExecutionContext context)
    {
        var first = RuleValueConverter.ToDateTime(args[0]);
        var second = RuleValueConverter.ToDateTime(args[1]);

        return first <= second ? first : second;
    }
}
