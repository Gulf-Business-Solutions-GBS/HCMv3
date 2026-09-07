using System;
using System.Globalization;

namespace HCMv3.BusinessRules.Services;

/// <summary>
/// The one place rule values change type.
///
/// <para>
/// A rule engine lives or dies on this. The author writes <c>ProbationDays &gt; 90</c> without knowing that
/// <c>ProbationDays</c> is an <c>int</c> while <c>90</c> arrived as a string from a JSON literal, and they
/// are right not to care. Every coercion in the engine funnels through here so the answer is the same
/// whether the comparison happened in a condition, an assignment or a function argument -- scattered
/// <c>Convert.ChangeType</c> calls are how an engine ends up saying a rule matched in one place and not in
/// another.
/// </para>
///
/// <para><b>Rules that matter:</b></para>
/// <list type="bullet">
/// <item>Whole numbers are <c>long</c> and fractions are <c>decimal</c>. Never <c>double</c> -- entitlement
/// days and money must not drift, and 0.1 + 0.2 surprising a payroll configurer is not a debate worth
/// having.</item>
/// <item>Dates parse with the INVARIANT culture only. A rule stored under an English tenant and executed
/// under an Arabic one must mean the same date; culture-sensitive parsing is how "01/02" silently becomes
/// February in one place and January in another.</item>
/// <item>Null propagates. A comparison against a null operand is false rather than an exception, because a
/// rule branching on a field nobody has filled in yet is normal, not broken.</item>
/// </list>
/// </summary>
public static class RuleValueConverter
{
    /// <summary>
    /// Normalises a raw CLR value into the engine's internal representation: <c>long</c>, <c>decimal</c>,
    /// <c>bool</c>, <c>DateTime</c>, <c>string</c> or null. Enums become their numeric value so a rule can
    /// compare them to a lookup id without knowing the enum type.
    /// </summary>
    public static object Normalize(object value)
    {
        switch (value)
        {
            case null:
                return null;

            case string text:
                return text;

            case bool flag:
                return flag;

            case DateTime date:
                return date;

            case byte or sbyte or short or ushort or int or uint or long:
                return Convert.ToInt64(value, CultureInfo.InvariantCulture);

            case ulong unsigned:
                return (long)unsigned;

            case decimal or float or double:
                return Convert.ToDecimal(value, CultureInfo.InvariantCulture);

            case Enum enumValue:
                return Convert.ToInt64(enumValue, CultureInfo.InvariantCulture);

            case DateTimeOffset offset:
                return offset.DateTime;

            default:
                return value;
        }
    }

    /// <summary>
    /// Coerces a value to the declared rule data type, or throws <see cref="InvalidCastException"/> when it
    /// cannot. Callers turn that into a rule error naming the rule -- this class has no idea which rule it
    /// is serving and should not pretend to.
    /// </summary>
    public static object Coerce(object value, RuleDataType dataType)
    {
        var normalized = Normalize(value);

        if (normalized == null)
        {
            return null;
        }

        switch (dataType)
        {
            case RuleDataType.String:
                return normalized as string ?? FormatInvariant(normalized);

            case RuleDataType.Integer:
            case RuleDataType.Lookup:
            case RuleDataType.Enum:
                return ToInteger(normalized);

            case RuleDataType.Decimal:
                return ToDecimal(normalized);

            case RuleDataType.Boolean:
                return ToBoolean(normalized);

            case RuleDataType.Date:
                return ToDateTime(normalized).Date;

            case RuleDataType.DateTime:
                return ToDateTime(normalized);

            case RuleDataType.Unknown:
            default:
                return normalized;
        }
    }

    /// <summary>
    /// Converts an engine value back to a specific CLR type, for writing into an entity property. Handles
    /// the nullable and enum cases that a bare cast does not.
    /// </summary>
    public static object ConvertTo(object value, Type targetType)
    {
        if (targetType == null)
        {
            throw new ArgumentNullException(nameof(targetType));
        }

        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;
        var normalized = Normalize(value);

        if (normalized == null)
        {
            if (underlying != targetType || !targetType.IsValueType)
            {
                return null;
            }

            throw new InvalidCastException(
                "Cannot assign null to the non-nullable type " + targetType.Name + ".");
        }

        if (underlying.IsEnum)
        {
            return Enum.ToObject(underlying, ToInteger(normalized));
        }

        if (underlying == typeof(string))
        {
            return normalized as string ?? FormatInvariant(normalized);
        }

        if (underlying == typeof(DateTime))
        {
            return ToDateTime(normalized);
        }

        if (underlying == typeof(bool))
        {
            return ToBoolean(normalized);
        }

        if (underlying == typeof(decimal))
        {
            return ToDecimal(normalized);
        }

        if (underlying == typeof(double))
        {
            return (double)ToDecimal(normalized);
        }

        if (underlying == typeof(float))
        {
            return (float)ToDecimal(normalized);
        }

        if (underlying == typeof(int))
        {
            return checked((int)ToInteger(normalized));
        }

        if (underlying == typeof(long))
        {
            return ToInteger(normalized);
        }

        if (underlying == typeof(short))
        {
            return checked((short)ToInteger(normalized));
        }

        if (underlying == typeof(byte))
        {
            return checked((byte)ToInteger(normalized));
        }

        if (underlying.IsInstanceOfType(normalized))
        {
            return normalized;
        }

        throw new InvalidCastException(
            "Cannot convert " + normalized.GetType().Name + " to " + targetType.Name + ".");
    }

    /// <summary>Parses a literal from its stored string form, honouring the declared data type.</summary>
    public static object ParseLiteral(string text, RuleDataType dataType)
    {
        if (text == null)
        {
            return null;
        }

        if (dataType == RuleDataType.String)
        {
            return text;
        }

        // An empty literal is a null, not a zero. "Compare to nothing" and "compare to 0" are different
        // questions and the authoring UI can express both.
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        return Coerce(text, dataType);
    }

    public static long ToInteger(object value)
    {
        switch (value)
        {
            case long number:
                return number;
            case decimal number:
                return decimal.ToInt64(decimal.Round(number, 0, MidpointRounding.AwayFromZero));
            case bool flag:
                return flag ? 1 : 0;
            case string text when long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed):
                return parsed;
            case string text when decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed):
                return decimal.ToInt64(decimal.Round(parsed, 0, MidpointRounding.AwayFromZero));
            default:
                throw new InvalidCastException("Cannot read " + Describe(value) + " as a whole number.");
        }
    }

    public static decimal ToDecimal(object value)
    {
        switch (value)
        {
            case decimal number:
                return number;
            case long number:
                return number;
            case bool flag:
                return flag ? 1m : 0m;
            case string text when decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed):
                return parsed;
            default:
                throw new InvalidCastException("Cannot read " + Describe(value) + " as a number.");
        }
    }

    public static bool ToBoolean(object value)
    {
        switch (value)
        {
            case bool flag:
                return flag;
            case long number:
                return number != 0;
            case decimal number:
                return number != 0m;
            case string text when bool.TryParse(text, out var parsed):
                return parsed;
            // "Yes"/"No" because that is what the SuccessFactors-style editors show, and a rule copied from
            // a specification document should not fail on the word the specification used.
            case string text when string.Equals(text, "yes", StringComparison.OrdinalIgnoreCase):
                return true;
            case string text when string.Equals(text, "no", StringComparison.OrdinalIgnoreCase):
                return false;
            case string text when long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed):
                return parsed != 0;
            default:
                throw new InvalidCastException("Cannot read " + Describe(value) + " as true or false.");
        }
    }

    public static DateTime ToDateTime(object value)
    {
        switch (value)
        {
            case DateTime date:
                return date;
            case string text when DateTime.TryParse(
                text,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed):
                return parsed;
            default:
                throw new InvalidCastException("Cannot read " + Describe(value) + " as a date.");
        }
    }

    /// <summary>
    /// Renders a value for messages and the execution log. Invariant, so a trace read by a developer in one
    /// locale describes the same values the engine actually compared.
    /// </summary>
    public static string FormatInvariant(object value)
    {
        return value switch
        {
            null => "(empty)",
            DateTime date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            decimal number => number.ToString(CultureInfo.InvariantCulture),
            long number => number.ToString(CultureInfo.InvariantCulture),
            bool flag => flag ? "true" : "false",
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString()
        };
    }

    private static string Describe(object value)
    {
        return value == null ? "an empty value" : "'" + value + "'";
    }
}
