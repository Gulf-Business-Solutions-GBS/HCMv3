using System;
using System.Collections.Generic;
using System.Linq;
using HCMv3.PayrollManagement;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// SHARED "per cost-center-bucket summary" section of legacy <c>uscRptPayrollJV_Custom016.ascx.cs</c>,
/// <c>Table1()</c>, BMC branch (lines 1176-1445). This exact block runs UNCHANGED regardless of whether
/// <c>cmbType</c> is "Main Format", "Main Format-New" or "Detail" (the <c>IsDetail</c> split only affects
/// the "CostCenter Wise Debit" section that follows it, lines 1448-1572) -- so
/// <see cref="PayrollJVCustom016MainFormatBmcReportAppService"/> and
/// <see cref="PayrollJVCustom016DetailReportAppService"/> both call this instead of duplicating it.
///
/// Also reused, with a different <paramref name="ds"/> source (Provision-difference amounts instead of
/// salary-register lines), by legacy's "Prov Difference" BMC branch (lines 2705-2954) -- NOT PORTED in
/// this wave (see porting-agent final report), but kept reusable here for a future pass.
/// </summary>
internal static class PayrollJVCustom016BmcSummaryHelper
{
    /// <summary>Legacy lines 1207-1216 / 2736-2745: the nine cost-center buckets iterated in order.</summary>
    public static readonly string[] CostCenterBuckets =
        { "1000", "2000", "3000", "4000", "5000", "6000", "7000", "8000", "0900" };

    private static readonly string[] PaidLeaveWageTypeCodes = { "1003", "7003", "1052", "7052", "1061", "7061" };
    private static readonly string[] LeaveProvisionWageTypeCodes = { "3001", "6001" };
    private static readonly string[] IndemnityPayAdjustWageTypeCodes = { "15", "16" };
    private static readonly string[] IndemnityProvisionWageTypeCodes = { "3000", "6000" };
    private static readonly string[] IndemnityPayWageTypeCodes = { "1010", "7010" };

    public record SummaryLine(string Account, double Amount, string Narrative);

    /// <summary>Legacy lines 1177-1231 / 2706-2759: first-digit cost-center bucketing.</summary>
    public static string BucketCostCenter(string code)
    {
        if (string.IsNullOrEmpty(code))
        {
            return code ?? string.Empty;
        }

        return code[0] switch
        {
            '1' => "1000",
            '2' => "2000",
            '3' => "3000",
            '4' => "4000",
            '5' => "5000",
            '6' => "6000",
            '7' => "7000",
            '8' => "8000",
            '9' => "0900",
            _ => code
        };
    }

    /// <summary>
    /// Ports legacy lines 1218-1445: builds bucketed (CostCenter-bucket, WageType, Account, OffsetAccount)
    /// totals, then emits up to six summary rows per non-empty bucket (Annual Leave Pay/Provision,
    /// Indemnity Pay Adjust, Indemnity Provision, Indemnity Pay, Net Pay).
    /// </summary>
    public static List<SummaryLine> BuildSummary(
        IReadOnlyCollection<PayrollJVCustom016Line> ds, Func<string, string> fixedWageTypeName)
    {
        var buckets = ds
            .Select(l => new { Bucket = BucketCostCenter(l.CostCenterCode), Line = l })
            .GroupBy(x => (x.Bucket, x.Line.WageTypeId, x.Line.AccountInfoId, x.Line.OffsetAccountInfoId))
            .Select(g => new { g.Key.Bucket, Sample = g.First().Line, CostAmount = g.Sum(x => x.Line.CostAmount) })
            .Where(g => g.CostAmount != 0)
            .ToList();

        var result = new List<SummaryLine>();

        foreach (var cc in CostCenterBuckets)
        {
            // Legacy lines 1256-1285: Debit - Annual Leave Pay.
            var paidLeave = buckets.Where(b => b.Bucket == cc && PaidLeaveWageTypeCodes.Contains(b.Sample.WageTypeCode)).ToList();
            if (paidLeave.Count > 0)
            {
                var sample = paidLeave[0].Sample;
                result.Add(new SummaryLine(
                    cc + " " + (sample.OffsetAccountInfoCode ?? string.Empty).PadLeft(8, '0'),
                    paidLeave.Sum(b => b.CostAmount),
                    "SAL " + fixedWageTypeName("1003")));
            }

            // Legacy lines 1287-1309: Credit - Annual Leave Provision.
            var leaveProvision = buckets.Where(b => b.Bucket == cc && LeaveProvisionWageTypeCodes.Contains(b.Sample.WageTypeCode)).ToList();
            if (leaveProvision.Count > 0)
            {
                var sample = leaveProvision[0].Sample;
                result.Add(new SummaryLine(
                    cc + " " + (sample.OffsetAccountInfoCode ?? string.Empty).PadLeft(8, '0'),
                    -1 * leaveProvision.Sum(b => b.CostAmount),
                    "SAL " + (sample.OffsetAccountInfoName ?? string.Empty)));
            }

            // Legacy lines 1311-1357: Debit - Indemnity Pay Adjust.
            var indemnityPayAdjust = buckets.Where(b => b.Bucket == cc && IndemnityPayAdjustWageTypeCodes.Contains(b.Sample.WageTypeCode)).ToList();
            if (indemnityPayAdjust.Count > 0)
            {
                var sample = indemnityPayAdjust[0].Sample;
                var amount = indemnityPayAdjust.Sum(b => b.CostAmount);

                // Legacy lines 1320-1323: when the net amount is negative, the row's cost center comes from
                // the FIRST 15/16 line in the WHOLE dataset (not scoped to this bucket).
                var costCenter = amount < 0
                    ? ds.Where(l => IndemnityPayAdjustWageTypeCodes.Contains(l.WageTypeCode)).Select(l => l.CostCenterCode).FirstOrDefault()
                    : sample.CostCenterCode;

                var account = amount < 0
                    ? costCenter + " " + (sample.AccountInfoCode ?? string.Empty).PadLeft(8, '0')
                    : costCenter + " " + (sample.OffsetAccountInfoCode ?? string.Empty).PadLeft(8, '0');
                var narrative = amount < 0
                    ? "SAL " + (sample.AccountInfoName ?? string.Empty)
                    : "SAL " + (sample.OffsetAccountInfoName ?? string.Empty);

                result.Add(new SummaryLine(account, amount, narrative));
            }

            // Legacy lines 1359-1380: Credit - Indemnity Provision.
            var indemnityProvision = buckets.Where(b => b.Bucket == cc && IndemnityProvisionWageTypeCodes.Contains(b.Sample.WageTypeCode)).ToList();
            if (indemnityProvision.Count > 0)
            {
                var sample = indemnityProvision[0].Sample;
                result.Add(new SummaryLine(
                    cc + " " + (sample.OffsetAccountInfoCode ?? string.Empty).PadLeft(8, '0'),
                    -1 * indemnityProvision.Sum(b => b.CostAmount),
                    "SAL " + (sample.AccountInfoName ?? string.Empty)));
            }

            // Legacy lines 1382-1403: Debit - Indemnity Pay.
            var indemnityPay = buckets.Where(b => b.Bucket == cc && IndemnityPayWageTypeCodes.Contains(b.Sample.WageTypeCode)).ToList();
            if (indemnityPay.Count > 0)
            {
                var sample = indemnityPay[0].Sample;
                result.Add(new SummaryLine(
                    cc + " " + (sample.OffsetAccountInfoCode ?? string.Empty).PadLeft(8, '0'),
                    indemnityPay.Sum(b => b.CostAmount),
                    "SAL " + (sample.AccountInfoName ?? string.Empty)));
            }

            // Legacy lines 1405-1444: Credit - Net Pay.
            var netPaySample = buckets.FirstOrDefault(b => b.Bucket == cc && b.Sample.WageTypeCode == "9000");
            if (netPaySample != null)
            {
                var netPay = buckets
                    .Where(b => b.Bucket == cc && b.Sample.WageTypeCode != "9000"
                                && (b.Sample.BillingType == BillingType.Earning || b.Sample.BillingType == BillingType.Deduction))
                    .Sum(b => b.Sample.BillingType == BillingType.Earning ? b.CostAmount : -1 * b.CostAmount);

                result.Add(new SummaryLine(
                    cc + " " + (netPaySample.Sample.OffsetAccountInfoCode ?? string.Empty).PadLeft(8, '0'),
                    -1 * netPay,
                    "SAL " + (netPaySample.Sample.AccountInfoName ?? string.Empty)));
            }
        }

        return result;
    }
}
