using System;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// Days of service counted towards end-of-service indemnity: calendar days from <c>from</c> to <c>to</c>
/// inclusive, <b>less every 29 February in between</b>.
///
/// <para><b>DELIBERATE DEVIATION FROM LEGACY</b>, on the user's instruction (2026-09-21): "handle leap year, it
/// should not increase". Legacy counted plain calendar days and divided by 365, so two full years spanning a
/// 29 February earned 731 x 15 / 365 = 30.041 days instead of 30. Without the leap day every full year is 365
/// days and earns exactly its rate.</para>
///
/// <para>Used by every indemnity day count -- the monthly provision (<c>AccrualService</c>), the payment's basis
/// lookup (<c>IndemnityCalculationService</c>) and the Full &amp; Final suggestion -- so the provision and the
/// settlement cannot disagree.</para>
///
/// <para>Known edge: unpaid days are deducted afterwards as a plain count, so an unpaid day that falls ON a
/// 29 February is taken off twice. Rare, and it errs towards the employer by one day's accrual.</para>
/// </summary>
public static class IndemnityServiceDays
{
    public static double Count(DateTime from, DateTime to)
    {
        var calendarDays = (to.Date - from.Date).TotalDays + 1;

        return calendarDays <= 0 ? calendarDays : calendarDays - LeapDaysBetween(from.Date, to.Date);
    }

    /// <summary>How many 29 Februaries fall within <paramref name="from"/>..<paramref name="to"/>, inclusive.</summary>
    public static int LeapDaysBetween(DateTime from, DateTime to)
    {
        var count = 0;

        for (var year = from.Year; year <= to.Year; year++)
        {
            if (!DateTime.IsLeapYear(year))
            {
                continue;
            }

            var leapDay = new DateTime(year, 2, 29);
            if (leapDay >= from && leapDay <= to)
            {
                count++;
            }
        }

        return count;
    }
}
