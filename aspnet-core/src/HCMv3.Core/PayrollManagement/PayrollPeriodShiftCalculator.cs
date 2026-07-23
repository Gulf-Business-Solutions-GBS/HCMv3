using System;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Pure, self-contained fiscal-period-shift date arithmetic (ported faithfully from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.PayrollControlRecord.GetStartEndDateFromPayrollPeriod</c>
/// / the equivalent inline logic in the legacy <c>OnSaving</c>). Given a <see cref="PayrollPeriod.Code"/>
/// fiscal-shift key (one of the 12 literal strings below), and a PayrollMonth/PayrollYear, computes the
/// actual calendar month (CalendarMonthStart/CalendarMonthEnd) that PayrollMonth/PayrollYear maps to.
///
/// The 12 legacy <see cref="PayrollPeriod.Code"/> values this switches on, verified from the legacy source,
/// are literal strings (not enum members): "Jan-Dec", "Feb-Jan", "Mar-Feb", "Apr-Mar", "May-Apr", "Jun-May",
/// "Jul-Jun", "Aug-Jul", "Sep-Aug", "Oct-Sep", "Nov-Oct", "Dec-Nov".
/// </summary>
public static class PayrollPeriodShiftCalculator
{
    /// <summary>
    /// Computes (CalendarMonthStart, CalendarMonthEnd) for the given fiscal-shift <paramref name="payrollPeriodCode"/>
    /// and PayrollMonth/PayrollYear. Throws <see cref="ArgumentException"/> for an unrecognized code, same as
    /// the legacy "Incorrect Payroll Period" exception.
    /// </summary>
    public static (DateTime CalendarMonthStart, DateTime CalendarMonthEnd) GetCalendarMonthRange(
        string payrollPeriodCode, int payrollMonth, int payrollYear)
    {
        int newYear;
        int newMonth;

        switch (payrollPeriodCode)
        {
            case "Jan-Dec": // 01/2015 is 01.01.2015-31.01.2015
                newYear = payrollYear;
                newMonth = payrollMonth;
                break;

            case "Feb-Jan": // 01/2015 is 01.02.2015 - 31.01.2016
                newYear = payrollMonth == 12 ? payrollYear + 1 : payrollYear;
                newMonth = payrollMonth == 12 ? 1 : payrollMonth + 1;
                break;

            case "Mar-Feb": // 01/2015 is 01.03.2015 - 29.02.2016
                newYear = payrollMonth >= 11 ? payrollYear + 1 : payrollYear;
                newMonth = ShiftMonth(payrollMonth, 11, 2);
                break;

            case "Apr-Mar":
                newYear = payrollMonth >= 10 ? payrollYear + 1 : payrollYear;
                newMonth = ShiftMonth(payrollMonth, 10, 3);
                break;

            case "May-Apr":
                newYear = payrollMonth >= 9 ? payrollYear + 1 : payrollYear;
                newMonth = ShiftMonth(payrollMonth, 9, 4);
                break;

            case "Jun-May":
                newYear = payrollMonth >= 8 ? payrollYear + 1 : payrollYear;
                newMonth = ShiftMonth(payrollMonth, 8, 5);
                break;

            case "Jul-Jun":
                newYear = payrollMonth >= 7 ? payrollYear + 1 : payrollYear;
                newMonth = ShiftMonth(payrollMonth, 7, 6);
                break;

            case "Aug-Jul":
                newYear = payrollMonth >= 6 ? payrollYear + 1 : payrollYear;
                newMonth = ShiftMonth(payrollMonth, 6, 7);
                break;

            case "Sep-Aug":
                newYear = payrollMonth >= 5 ? payrollYear + 1 : payrollYear;
                newMonth = ShiftMonth(payrollMonth, 5, 8);
                break;

            case "Oct-Sep":
                newYear = payrollMonth >= 4 ? payrollYear + 1 : payrollYear;
                newMonth = ShiftMonth(payrollMonth, 4, 9);
                break;

            case "Nov-Oct":
                newYear = payrollMonth >= 3 ? payrollYear + 1 : payrollYear;
                newMonth = ShiftMonth(payrollMonth, 3, 10);
                break;

            case "Dec-Nov": // 01/15 is 01.12.2015
                newYear = payrollMonth >= 2 ? payrollYear + 1 : payrollYear;
                newMonth = ShiftMonth(payrollMonth, 2, 11);
                break;

            default:
                throw new ArgumentException("Incorrect Payroll Period", nameof(payrollPeriodCode));
        }

        var start = new DateTime(newYear, newMonth, 1);
        var end = new DateTime(newYear, newMonth, DateTime.DaysInMonth(newYear, newMonth));
        return (start, end);
    }

    /// <summary>
    /// Legacy per-branch month-shift parity: months from <paramref name="wrapFromMonth"/>..12 wrap to
    /// 1..(12 - wrapFromMonth + 1), otherwise the month advances by <paramref name="shiftAmount"/>.
    /// </summary>
    private static int ShiftMonth(int payrollMonth, int wrapFromMonth, int shiftAmount)
    {
        if (payrollMonth >= wrapFromMonth)
        {
            return payrollMonth - wrapFromMonth + 1;
        }

        return payrollMonth + shiftAmount;
    }
}
