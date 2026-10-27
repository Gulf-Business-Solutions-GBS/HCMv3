using System.Collections.Generic;
using Abp.Dependency;
using HCMv3.BusinessRules.Services;

namespace HCMv3.BusinessRules.Functions;

/// <summary>
/// Calendar days from the execution date to a date: <c>DaysUntil(Document.ExpiryDate) &lt;= 30</c> reads
/// "expires within 30 days". Negative once the date has passed, zero on the day itself.
///
/// <para>
/// Added for notification criteria, where "how long until" is nearly every question asked. It is a plain
/// subtraction on purpose, unlike <see cref="GetNumberOfCalendarDaysRuleFunction"/>, which counts a range
/// inclusively and floors a reversed range at zero: here a negative answer is information ("expired three
/// days ago"), not a data error, and a rule asking <c>DaysUntil(x) &lt; 0</c> must be able to find it.
/// </para>
///
/// <para>
/// Measured from <c>RuleExecutionContext.ExecutionDate</c>, not the clock -- the same reason
/// <see cref="TodayRuleFunction"/> exists. An empty date returns null, which makes any comparison on it false,
/// so a record with no expiry date never matches "expires within 30 days". The unset date 0001-01-01 counts
/// as empty for the same reason.
/// </para>
/// </summary>
public class DaysUntilRuleFunction : IRuleFunction, ITransientDependency
{
    public string Name => "DaysUntil";

    public RuleFunctionDescriptor Descriptor => new(
        Name,
        "RuleFunction_DaysUntil",
        RuleDataType.Integer,
        new RuleFunctionParameter("date", RuleDataType.Date));

    public object Invoke(IReadOnlyList<object> args, RuleExecutionContext context)
    {
        if (args.Count == 0 || args[0] == null)
        {
            return null;
        }

        var date = RuleValueConverter.ToDateTime(args[0]).Date;

        // 0001-01-01 is how this schema stores "never set" in its many non-nullable date columns (a return
        // date not yet recorded, an expiry nobody entered). Read literally it is 739,000 days ago, which
        // satisfies "DaysUntil(x) <= 30" and would notify about every such record on the first run.
        if (date == default)
        {
            return null;
        }

        return (long)(date - context.ExecutionDate).TotalDays;
    }
}
