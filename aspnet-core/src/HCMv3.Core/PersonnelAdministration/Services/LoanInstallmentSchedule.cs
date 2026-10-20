using System;
using System.Collections.Generic;
using System.Linq;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>
/// The arithmetic of a loan's instalment schedule, as legacy has it, with no database behind it so each rule can
/// be tested on its own. Every method names the legacy member it reproduces.
///
/// <para>
/// <b>Two lookups, deliberately.</b> Legacy has <c>EmployeeLoan.GetInstallmentAmount</c>, which looks up the
/// first day of the payroll month, and a separate inline lookup in the payroll engine
/// (<c>EmployeeCalculatedPayService.GetLoanRecoveryDed</c>, line 1626), which looks up the pay date itself. On a
/// schedule whose rows all start on the first of a month and close the day before the next one -- which is all
/// that validation lets through -- the two agree. They are kept apart anyway so that payroll reads exactly what
/// legacy payroll reads; see <see cref="GetPayrollInstallmentAmount"/>.
/// </para>
/// </summary>
public static class LoanInstallmentSchedule
{
    /// <summary>Legacy's open end for the last row: <c>new DateTime(9999, 12, 31)</c>.</summary>
    public static readonly DateTime OpenEnded = new DateTime(9999, 12, 31);

    /// <summary>Legacy <c>CalculateRecoveryDate</c>'s safety limit: 1200 months, a hundred years.</summary>
    public const int MaxRecoveryMonths = 1200;

    /// <summary>
    /// Legacy <c>EmployeeLoan.GetInstallmentAmount</c>: the row in force on the first of
    /// <paramref name="payrollDate"/>'s month, latest start first; the loan's fixed amount when there is none.
    /// </summary>
    public static double GetInstallmentAmount(
        double fixedAmount,
        IEnumerable<EmployeeLoanInstallment> installments,
        DateTime payrollDate,
        int? excludeInstallmentId = null)
    {
        var payrollMonth = new DateTime(payrollDate.Year, payrollDate.Month, 1);

        var installment = installments
            .Where(r => (!excludeInstallmentId.HasValue || r.Id != excludeInstallmentId.Value)
                        && r.StartDate <= payrollMonth
                        && r.EndDate >= payrollMonth)
            .OrderByDescending(r => r.StartDate)
            .FirstOrDefault();

        // Backward compatibility for old loans.
        return installment?.Amount ?? fixedAmount;
    }

    /// <summary>
    /// The payroll engine's own lookup (legacy <c>EmployeeCalculatedPayService.cs</c> lines 1626-1632 and the
    /// simulation copy at 5881-5887): <c>StartDate &lt;= payDate &amp;&amp; payDate &lt;= EndDate</c>, latest
    /// start first, else <c>FixedAmount</c>. The pay date as it is, not its month's first day.
    /// </summary>
    public static double GetPayrollInstallmentAmount(
        double fixedAmount,
        IEnumerable<EmployeeLoanInstallment> installments,
        DateTime payDate)
    {
        var installment = installments
            .Where(r => r.StartDate <= payDate && payDate <= r.EndDate)
            .OrderByDescending(r => r.StartDate)
            .FirstOrDefault();

        // Existing loans without an installment schedule continue to use FixedAmount.
        return installment == null ? fixedAmount : installment.Amount;
    }

    /// <summary>
    /// Legacy <c>EmployeeLoanInstallment.SetEndDate</c>: in start-date order, each row ends the day before the
    /// next later start, and the last row is open-ended. Returns the rows whose end date it changed.
    /// </summary>
    public static IReadOnlyList<EmployeeLoanInstallment> ChainEndDates(IEnumerable<EmployeeLoanInstallment> installments)
    {
        var ordered = installments.OrderBy(r => r.StartDate).ToList();
        var changed = new List<EmployeeLoanInstallment>();

        foreach (var installment in ordered)
        {
            var calculatedEndDate = OpenEnded;

            var next = ordered
                .Where(r => r.StartDate > installment.StartDate)
                .OrderBy(r => r.StartDate)
                .FirstOrDefault();

            if (next != null)
            {
                calculatedEndDate = next.StartDate.AddDays(-1);
            }

            if (installment.EndDate != calculatedEndDate)
            {
                installment.EndDate = calculatedEndDate;
                changed.Add(installment);
            }
        }

        return changed;
    }

    /// <summary>
    /// Legacy <c>EmployeeLoan.CalculateRecoveryDate</c>: walks the months from the recovery start, recovering
    /// each month's instalment from the DUE amount (not the balance), skipping exempted months, and returns the
    /// last day of the month that clears it. Returns <paramref name="currentEndDate"/> unchanged when there is
    /// nothing to walk -- no due amount, no recovery start, a month whose instalment is zero, or a hundred years
    /// without clearing it. The payroll setting check is the caller's.
    /// </summary>
    public static DateTime CalculateRecoveryEndDate(
        double dueAmount,
        DateTime recoveryStartDate,
        double fixedAmount,
        DateTime currentEndDate,
        IReadOnlyCollection<EmployeeLoanInstallment> installments,
        IReadOnlyCollection<DateTime> exemptionDates,
        int? excludeInstallmentId = null)
    {
        if (dueAmount <= 0 || recoveryStartDate == default)
        {
            return currentEndDate;
        }

        var remainingAmount = dueAmount;
        var currentMonth = new DateTime(recoveryStartDate.Year, recoveryStartDate.Month, 1);

        for (var monthCount = 0; monthCount < MaxRecoveryMonths; monthCount++)
        {
            // If the month is exempted, payroll will not recover an installment.
            var isExempted = exemptionDates.Any(d => d.Year == currentMonth.Year && d.Month == currentMonth.Month);

            if (!isExempted)
            {
                var installmentAmount = GetInstallmentAmount(fixedAmount, installments, currentMonth, excludeInstallmentId);

                if (installmentAmount <= 0)
                {
                    return currentEndDate;
                }

                remainingAmount -= installmentAmount;

                if (remainingAmount <= 0)
                {
                    return new DateTime(
                        currentMonth.Year,
                        currentMonth.Month,
                        DateTime.DaysInMonth(currentMonth.Year, currentMonth.Month));
                }
            }

            currentMonth = currentMonth.AddMonths(1);
        }

        return currentEndDate;
    }

    /// <summary>
    /// Legacy <c>EmployeeRejoin</c> lines 724-736: the effective dates carried onto the merged loan -- every
    /// old row starting after the new recovery start month, plus that month itself, distinct and in order.
    /// </summary>
    public static IReadOnlyList<DateTime> CarryForwardEffectiveDates(
        DateTime recoveryStartDate,
        IEnumerable<EmployeeLoanInstallment> oldInstallments)
    {
        var recoveryStartMonth = new DateTime(recoveryStartDate.Year, recoveryStartDate.Month, 1);

        var dates = oldInstallments
            .Where(r => r.StartDate > recoveryStartMonth)
            .Select(r => r.StartDate)
            .ToList();
        dates.Add(recoveryStartMonth);

        return dates.Distinct().OrderBy(r => r).ToList();
    }
}
