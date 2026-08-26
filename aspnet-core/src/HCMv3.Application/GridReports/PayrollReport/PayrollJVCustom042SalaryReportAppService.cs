using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.Dto;
using HCMv3.GridReports.PayrollReport.Dto;
using HCMv3.SystemSecurity;
using HCMv3.PayrollManagement;
using Microsoft.AspNetCore.Mvc;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// Payroll JV (Custom 042) "Salary JV" analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPayrollJV_Custom042.ascx(.cs)</c>,
/// <c>cmbType == "Salary JV"</c> branch of <c>Table1()</c> (lines 980-1350).
///
/// SHAPE: the same wage-account-driven journal voucher shared with the rest of the JV family through
/// <see cref="PayrollJVPostingLineLoader"/> (see <see cref="PayrollJVWageTypeScope.EarningDeductionAndAllProvision"/>),
/// but with THIS SCREEN'S column shape: Main/Sub account pair, Division/Department/Analysis1 cost
/// dimensions, dual-currency amount and an explicit Debit/Credit flag rather than signed columns.
///
/// Two branches, in order:
/// <list type="number">
/// <item><description>Debit/Credit Entry (lines 1022-1081): every Earning/Deduction line EXCEPT the
/// leave-provision, indemnity-provision and overtime wage types (those are their own reports) and Net
/// Pay (code 9000), grouped by account/offset-account/cost elements.</description></item>
/// <item><description>NetPay Entry (lines 1305-1349): the Net Pay line, grouped by account/offset-
/// account/currency only (no cost-element split).</description></item>
/// </list>
///
/// NOT PORTED -- <c>Table_GetAdvanceJV</c> BIFURCATION (legacy lines 1083-1240): when an employee's
/// Advance Salary Reimbursement (wage code 2005) has a matching off-cycle <c>EmployeePayment</c> record,
/// legacy re-queries that OTHER off-cycle salary register and re-books the 2005 amount split across ITS
/// own postings instead of the current register's. This is a genuinely separate ~700-line sub-feature
/// (<c>Table_GetAdvanceJV</c>) triggered only for the narrow case of a linked off-cycle advance payment.
/// It is NOT implemented in this port; every wage-code-2005 line instead falls through legacy's own
/// "not found in Advance Off-Cycle Register" fallback (lines 1242-1303), which uses the EXACT SAME
/// grouping/row-construction formula as the main Debit/Credit Entry loop above -- i.e. this port omits the
/// `if (WageTypeCode == "2005") continue;` special case and its bifurcated replacement, which is
/// observably correct for every employee that has NO matching off-cycle EmployeePayment (the common
/// case) and only diverges from legacy in that specific bifurcation scenario. Flagged for a follow-up
/// wave if that scenario needs porting.
///
/// DIVERGENCE (agreed for this wave): RLS is applied -- see <see cref="PayrollJVPostingLineLoader"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_PayrollJVCustom042Salary)]
public class PayrollJVCustom042SalaryReportAppService
    : PayrollJVCustom042ReportAppServiceBase, IPayrollJVCustom042SalaryReportAppService
{
    /// <summary>Legacy line 1307: the Net Pay wage type.</summary>
    private const string NetPayWageTypeCode = "9000";

    /// <summary>Legacy line 1045/1048: the Advance Salary Reimbursement wage type (bifurcation trigger).</summary>
    private const string AdvanceSalaryWageTypeCode = "2005";

    /// <summary>Legacy lines 573-578: Annual Leave Provision codes, excluded from this format.</summary>
    private static readonly string[] LeaveProvisionCodes = { "3001", "6001", "3004", "6004" };

    /// <summary>Legacy lines 579-583: Indemnity Provision codes, excluded from this format.</summary>
    private static readonly string[] IndemnityProvisionCodes = { "3000", "6000", "3003", "6003" };

    /// <summary>Legacy lines 585-591: Overtime codes, excluded (they are their own "Overtime JV" report).</summary>
    private static readonly string[] OvertimeCodes = { "1007", "7007", "1008", "7008", "1009", "7009" };

    private readonly PayrollJVPostingLineLoader _postingLineLoader;

    public PayrollJVCustom042SalaryReportAppService(PayrollJVPostingLineLoader postingLineLoader)
    {
        _postingLineLoader = postingLineLoader;
    }

    protected override string LegacyScreenName => "uscRptPayrollJV_Custom042";

    protected override string ScreenCaption => "Payroll JV (Custom 042) - Salary JV";

    [HttpPost]
    public async Task<PagedResultDto<PayrollJVCustom042ReportRowDto>> GetPayrollJVCustom042SalaryReport(
        GetPayrollJVCustom042SalaryReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);

        var paged = rows.AsQueryable().OrderBy(input.Sorting).PageBy(input).ToList();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildJvParameterString(input), rows.Count);

        return new PagedResultDto<PayrollJVCustom042ReportRowDto>(rows.Count, paged);
    }

    [HttpPost]
    public async Task<FileDto> GetPayrollJVCustom042SalaryReportToExcel(GetPayrollJVCustom042SalaryReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the whole filtered data set.
        var ordered = rows.AsQueryable().OrderBy(input.Sorting).ToList();

        return await ExportToFileAsync(
            "PayrollJVCustom042Salary.xlsx",
            ordered,
            GetPostingExportColumns(),
            BuildJvParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND row construction, shared by the paged read and the
    /// export. Ports legacy "Salary JV" branch of <c>Table1()</c> (lines 980-1350).
    /// </summary>
    private async Task<List<PayrollJVCustom042ReportRowDto>> BuildRowsAsync(GetPayrollJVCustom042SalaryReportInput input)
    {
        var endDate = ResolveEndDate(input.Month, input.Year);
        var voucherDate = VoucherDate(endDate);

        var lines = await _postingLineLoader.LoadAsync(
            endDate,
            Array.Empty<string>(),
            input.PayrollRunType,
            input.PayrollAreaId.HasValue ? new[] { input.PayrollAreaId.Value } : Array.Empty<int>(),
            input.ResolveEmployeeCodes(),
            await GetRlsEmployeeIdsAsync(),
            PayrollJVWageTypeScope.EarningDeductionAndAllProvision);

        var rows = new List<PayrollJVCustom042ReportRowDto>();

        // Legacy lines 1023-1080 -- Debit/Credit Entry.
        var excluded = LeaveProvisionCodes.Concat(IndemnityProvisionCodes).Concat(OvertimeCodes).ToArray();
        var mainLines = lines
            .Where(l => (l.BillingType == BillingType.Earning || l.BillingType == BillingType.Deduction)
                        && !excluded.Contains(l.WageTypeCode))
            .ToList();

        // Legacy line 1045: dsDrCr excludes Net Pay (9000) only. The bifurcation special case for 2005
        // (line 1048) is not applied -- see the class remarks.
        foreach (var group in GroupByAccountAndCostElements(mainLines.Where(l => l.WageTypeCode != NetPayWageTypeCode)))
        {
            var amount = group.Sum(l => l.Amount);
            if (amount == 0)
            {
                continue;
            }

            var sample = group.First();
            var drCr = (sample.BillingType == BillingType.Earning && amount > 0)
                       || (sample.BillingType == BillingType.Deduction && amount < 0)
                ? "D" : "C";

            rows.Add(NewRowNullLiteral(rows,
                sample.AccountInfoCode, sample.AccountInfoName, sample.OffsetAccountInfoCode, sample.OffsetAccountInfoName,
                sample.BillingType + "-" + sample.WageTypeName + "-For the month of " + voucherDate,
                sample.CostElement1Code, sample.CostElement2Code, sample.CostElement3Code,
                "KD", amount, drCr));
        }

        // Legacy lines 1305-1349 -- NetPay Entry: grouped by account/offset-account/currency only.
        foreach (var group in mainLines.Where(l => l.WageTypeCode == NetPayWageTypeCode)
                     .GroupBy(l => (l.AccountInfoId, l.OffsetAccountInfoId)))
        {
            var amount = group.Sum(l => l.Amount);
            if (amount == 0)
            {
                continue;
            }

            var sample = group.First();
            rows.Add(NewRowNullLiteral(rows,
                sample.AccountInfoCode, sample.AccountInfoName, sample.OffsetAccountInfoCode, sample.OffsetAccountInfoName,
                "Being Net Salary Payable-For the month of " + voucherDate,
                null, null, null,
                "KD", amount, amount > 0 ? "C" : "D"));
        }

        return rows;
    }
}
