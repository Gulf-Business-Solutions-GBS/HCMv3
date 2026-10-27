using System;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// The two run-type predicates the legacy calculated-pay branches repeat verbatim in front of nearly every
/// wage rule of <c>HCM10.PayrollEngine/EmployeeCalculatedPayService.cs</c>. These are NOT the four dispatcher
/// gates of <see cref="IWageTypeCalculatorResolver"/> (IsOnHold / AllowToCalculate / Allow_CalcWageType /
/// IsNotReturnFromLeave) -- those are applied once, before a calculator runs. These two are per-rule
/// behaviour: legacy applies <see cref="AllowIncludingAdvanceLeave"/> to some codes and
/// <see cref="AllowEndOfServiceOnly"/> to others, and the difference is deliberate.
///
/// <para>Factored here rather than copied into 20 calculators, per wave P6 instruction 5.</para>
/// </summary>
public static class PayrollRunGates
{
    /// <summary>
    /// Legacy's three-arm predicate used by wage codes 5, 7, 9, 11, 13, 23, 25, 27, 29, 31, 33, 35, 1006 and
    /// by <c>GetSocialSecurityDed</c>/<c>GetAirTicketPay</c>/<c>GetTravelPerDiemPay</c> (e.g. legacy lines
    /// 134-136, 530-532, 573-575, 3009-3011):
    /// <code>
    /// payrollRunType == Regular
    /// || (OffCycle &amp;&amp; offCycleReasons == reasonAdvanceLeave &amp;&amp; currWageType.IncludeInAdvancePay)
    /// || (OffCycle &amp;&amp; offCycleReasons == endOfServiceOffcycle)
    /// </code>
    /// </summary>
    public static bool AllowIncludingAdvanceLeave(PayrollCalculationContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (context.PayrollRunType == PayrollRunType.Regular)
        {
            return true;
        }

        if (context.PayrollRunType != PayrollRunType.OffCycle)
        {
            return false;
        }

        if (context.IsAdvanceLeave && context.WageType != null && context.WageType.IncludeInAdvancePay)
        {
            return true;
        }

        return context.IsEndOfService;
    }

    /// <summary>
    /// Legacy's two-arm predicate used by wage codes 2000, 2007, 2010 and by the Violation Deduction branch
    /// (legacy lines 50, 398, 435, 470):
    /// <code>payrollRunType == Regular || (OffCycle &amp;&amp; offCycleReasons == endOfServiceOffcycle)</code>
    /// </summary>
    public static bool AllowEndOfServiceOnly(PayrollCalculationContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        return context.PayrollRunType == PayrollRunType.Regular
               || (context.PayrollRunType == PayrollRunType.OffCycle && context.IsEndOfService);
    }

    /// <summary>
    /// Legacy's ubiquitous <c>if (payrollRunType == OffCycle &amp;&amp; currWageType.DontImpactNetPay == false)</c>
    /// guard that fronts every "Advance Payment Ded (WageCode : 2005)" sub-block (e.g. legacy lines 106, 163,
    /// 559, 602, 688, 776, 866, 1001, 1045).
    /// </summary>
    public static bool ShouldPostAdvancePaymentDed(PayrollCalculationContext context)
    {
        return context != null
               && context.PayrollRunType == PayrollRunType.OffCycle
               && context.WageType != null
               && context.WageType.DontImpactNetPay == false;
    }

    /// <summary>
    /// Legacy's <c>new DateTime(payDate.Year, payDate.Month, 01, 0, 0, 0)</c>, recomputed inside almost every
    /// legacy helper rather than taken from <see cref="PayrollCalculationContext.PayStartDate"/>. Kept as a
    /// helper so ported rules read like the legacy source they came from.
    /// </summary>
    public static DateTime MonthStart(DateTime date)
    {
        return new DateTime(date.Year, date.Month, 1, 0, 0, 0);
    }

    /// <summary>Legacy's <c>new DateTime(d.Year, d.Month, DateTime.DaysInMonth(d.Year, d.Month))</c>.</summary>
    public static DateTime MonthEnd(DateTime date)
    {
        return new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));
    }
}
