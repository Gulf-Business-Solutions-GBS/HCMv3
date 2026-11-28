using System.Collections.Generic;
using HCMv3.PersonnelAdministration;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// Turns an employee's Remuneration rows into paid days for the Daily and Hourly basic-pay arms (legacy
/// <c>EmployeeBasicPayService.cs</c> lines 1069-1075 / 1123-1129, repeated in the simulation copy):
/// a <see cref="PaymentUnit.Days"/> row counts its <c>PaymentNumber</c> as days; a <see cref="PaymentUnit.Hours"/>
/// row counts <c>PaymentNumber / totalHoursInDay</c> days, and is skipped when <paramref name="totalHoursInDay"/> is 0.
/// </summary>
public static class RemunerationPayDaysCalculator
{
    public static double SumPayDays(IEnumerable<EmployeeRemuneration> remunerations, double totalHoursInDay)
    {
        double payWorkingdays = 0;

        foreach (var remuneration in remunerations)
        {
            if (remuneration.PaymentUnit == PaymentUnit.Days)
            {
                payWorkingdays += remuneration.PaymentNumber;
            }
            else if (remuneration.PaymentUnit == PaymentUnit.Hours && totalHoursInDay != 0)
            {
                payWorkingdays += remuneration.PaymentNumber / totalHoursInDay;
            }
        }

        return payWorkingdays;
    }
}
