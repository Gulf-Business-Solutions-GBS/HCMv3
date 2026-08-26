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
/// Payroll JV (Custom 042) "EmployeeWise BreakDown" analysis grid report -- port of legacy
/// <c>uscRptPayrollJV_Custom042.ascx.cs</c>, <c>cmbType == "EmployeeWise BreakDown"</c> branch of
/// <c>Table1()</c> (lines 3140-3236): the raw wage-account lines grouped by employee/wage type/account,
/// with NO billing-type or wage-code filter at all (legacy's own filter comment is commented out, line
/// 3186).
///
/// LEGACY QUIRK REPRODUCED: unlike every other Custom 042 posting format (which writes
/// <c>Math.Abs(amount)</c> into Fc-amount/Net-amount), this branch writes the RAW amount when it is
/// non-negative and ZERO when it is negative (legacy lines 3228-3229) -- a negative-amount line still
/// produces a row (it survived the <c>!= 0</c> filter) but with a visibly wrong 0.00 amount. Reproduced
/// verbatim, not "fixed" to Math.Abs.
///
/// DIVERGENCE (agreed for this wave): RLS is applied -- see <see cref="PayrollJVPostingLineLoader"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_PayrollJVCustom042EmployeeWiseBreakDown)]
public class PayrollJVCustom042EmployeeWiseBreakDownReportAppService
    : PayrollJVCustom042ReportAppServiceBase, IPayrollJVCustom042EmployeeWiseBreakDownReportAppService
{
    private readonly PayrollJVPostingLineLoader _postingLineLoader;

    public PayrollJVCustom042EmployeeWiseBreakDownReportAppService(PayrollJVPostingLineLoader postingLineLoader)
    {
        _postingLineLoader = postingLineLoader;
    }

    protected override string LegacyScreenName => "uscRptPayrollJV_Custom042";

    protected override string ScreenCaption => "Payroll JV (Custom 042) - EmployeeWise BreakDown";

    [HttpPost]
    public async Task<PagedResultDto<PayrollJVCustom042ReportRowDto>> GetPayrollJVCustom042EmployeeWiseBreakDownReport(
        GetPayrollJVCustom042EmployeeWiseBreakDownReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);

        var paged = rows.AsQueryable().OrderBy(input.Sorting).PageBy(input).ToList();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildJvParameterString(input), rows.Count);

        return new PagedResultDto<PayrollJVCustom042ReportRowDto>(rows.Count, paged);
    }

    [HttpPost]
    public async Task<FileDto> GetPayrollJVCustom042EmployeeWiseBreakDownReportToExcel(
        GetPayrollJVCustom042EmployeeWiseBreakDownReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);
        var ordered = rows.AsQueryable().OrderBy(input.Sorting).ToList();

        return await ExportToFileAsync(
            "PayrollJVCustom042EmployeeWiseBreakDown.xlsx",
            ordered,
            GetPostingExportColumns(),
            BuildJvParameterString(input));
    }

    private async Task<List<PayrollJVCustom042ReportRowDto>> BuildRowsAsync(
        GetPayrollJVCustom042EmployeeWiseBreakDownReportInput input)
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

        // Legacy lines 3185-3233 -- no billing-type/wage-code filter at all.
        foreach (var group in lines.GroupBy(l => (l.EmployeeId, l.WageTypeId, l.AccountInfoId,
                     l.OffsetAccountInfoId, l.CostElement1Id, l.CostElement2Id, l.CostElement3Id)))
        {
            var amount = group.Sum(l => l.Amount);
            if (amount == 0)
            {
                continue;
            }

            var sample = group.First();

            rows.Add(new PayrollJVCustom042ReportRowDto
            {
                Id = rows.Count + 1,
                LineNumber = rows.Count + 1,
                EmployeeCode = sample.EmployeeCode,
                MainAccountCode = sample.AccountInfoCode ?? string.Empty,
                MainAccountName = sample.AccountInfoName ?? string.Empty,
                SubAccountCode = sample.OffsetAccountInfoCode ?? string.Empty,
                SubAccountName = sample.OffsetAccountInfoName ?? string.Empty,
                Description = sample.BillingType + "-" + sample.WageTypeName + "-For the month of " + voucherDate,
                Division = sample.CostElement1Code ?? "NULL",
                Department = sample.CostElement2Code ?? "NULL",
                Analysis1 = sample.CostElement3Code ?? "NULL",
                Analysis2 = "NULL",
                Activity1 = "NULL",
                Activity2 = "NULL",
                Currency = "KD",
                // Legacy lines 3228-3229: NOT Math.Abs -- a negative amount is written as 0.00.
                FcAmount = amount >= 0 ? amount : 0,
                NetAmount = amount >= 0 ? amount : 0,
                DrCr = sample.BillingType == BillingType.Earning || sample.BillingType == BillingType.Provision
                    ? "D" : "C"
            });
        }

        return rows;
    }
}
