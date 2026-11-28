using System;
using System.Collections.Generic;
using System.Linq;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// The fund's three totals worked out from its transaction rows -- legacy <c>EmployeeFundDetail.UpdateDueBalance</c>
/// and the payroll engine's re-totals (<c>EmployeeCalculatedPayService.GetFundDed</c>):
/// <list type="bullet">
/// <item>Amount = Opening + Deduction + External Deduction rows,</item>
/// <item>Paid Amount = Payment rows,</item>
/// <item>Balance Amount = Amount - Paid Amount,</item>
/// </list>
/// each rounded to 2 decimals.
/// </summary>
public static class EmployeeFundTotals
{
    /// <summary>Sets <paramref name="fund"/>'s Amount, PaidAmount and BalanceAmount from <paramref name="details"/>.</summary>
    public static void Apply(EmployeeFund fund, IEnumerable<EmployeeFundDetail> details)
    {
        var rows = details.ToList();

        fund.Amount = Math.Round(rows
            .Where(r => r.FundPayType == FundPayType.Opening
                        || r.FundPayType == FundPayType.Deduction
                        || r.FundPayType == FundPayType.ExternalDeduction)
            .Sum(r => r.Amount), 2);

        fund.PaidAmount = Math.Round(rows
            .Where(r => r.FundPayType == FundPayType.Payment)
            .Sum(r => r.Amount), 2);

        fund.BalanceAmount = Math.Round(fund.Amount - fund.PaidAmount, 2);
    }
}
