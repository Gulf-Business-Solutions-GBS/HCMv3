using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.Dto;
using HCMv3.GridReports.PayrollReport.Dto;
using HCMv3.SystemSecurity;
using HCMv3.PayrollManagement;
using Microsoft.AspNetCore.Mvc;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// Payroll JV (Custom 042) "AnnualLeave Provision JV" analysis grid report -- port of legacy
/// <c>uscRptPayrollJV_Custom042.ascx.cs</c>, <c>cmbType == "AnnualLeave Provision JV"</c> branch of
/// <c>Table1()</c> (lines 1794-1925).
///
/// Two branches: a Debit/Credit Entry over every Annual Leave Provision line (wage codes 3001/6001/3004/
/// 6004), then a single Closing/NetPay Entry booking the total against the hard-coded account "614000"
/// (legacy line 1887).
///
/// DIVERGENCE (agreed for this wave): RLS is applied -- see <see cref="PayrollJVPostingLineLoader"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_PayrollJVCustom042AnnualLeaveProvision)]
public class PayrollJVCustom042AnnualLeaveProvisionReportAppService
    : PayrollJVCustom042ReportAppServiceBase, IPayrollJVCustom042AnnualLeaveProvisionReportAppService
{
    /// <summary>Legacy lines 573-578.</summary>
    private static readonly string[] LeaveProvisionCodes = { "3001", "6001", "3004", "6004" };

    /// <summary>Legacy line 1887: the hard-coded closing account.</summary>
    private const string ClosingAccountCode = "614000";

    private readonly PayrollJVPostingLineLoader _postingLineLoader;
    private readonly IRepository<AccountInfo> _accountInfoRepository;

    public PayrollJVCustom042AnnualLeaveProvisionReportAppService(
        PayrollJVPostingLineLoader postingLineLoader,
        IRepository<AccountInfo> accountInfoRepository)
    {
        _postingLineLoader = postingLineLoader;
        _accountInfoRepository = accountInfoRepository;
    }

    protected override string LegacyScreenName => "uscRptPayrollJV_Custom042";

    protected override string ScreenCaption => "Payroll JV (Custom 042) - AnnualLeave Provision JV";

    [HttpPost]
    public async Task<PagedResultDto<PayrollJVCustom042ReportRowDto>> GetPayrollJVCustom042AnnualLeaveProvisionReport(
        GetPayrollJVCustom042AnnualLeaveProvisionReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);

        var paged = rows.AsQueryable().OrderBy(input.Sorting).PageBy(input).ToList();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildJvParameterString(input), rows.Count);

        return new PagedResultDto<PayrollJVCustom042ReportRowDto>(rows.Count, paged);
    }

    [HttpPost]
    public async Task<FileDto> GetPayrollJVCustom042AnnualLeaveProvisionReportToExcel(
        GetPayrollJVCustom042AnnualLeaveProvisionReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);
        var ordered = rows.AsQueryable().OrderBy(input.Sorting).ToList();

        return await ExportToFileAsync(
            "PayrollJVCustom042AnnualLeaveProvision.xlsx",
            ordered,
            GetPostingExportColumns(),
            BuildJvParameterString(input));
    }

    private async Task<List<PayrollJVCustom042ReportRowDto>> BuildRowsAsync(
        GetPayrollJVCustom042AnnualLeaveProvisionReportInput input)
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
            PayrollJVWageTypeScope.EarningDeductionAndAllProvision,
            await ResolveWageAccountSourceAsync());

        var rows = new List<PayrollJVCustom042ReportRowDto>();

        // Legacy lines 1836-1884 -- Debit/Credit Entry.
        var provisionLines = lines
            .Where(l => l.BillingType == BillingType.Provision && LeaveProvisionCodes.Contains(l.WageTypeCode))
            .ToList();

        foreach (var group in GroupByAccountAndCostElements(provisionLines))
        {
            var amount = group.Sum(l => l.Amount);
            if (amount == 0)
            {
                continue;
            }

            var sample = group.First();
            rows.Add(NewRowNullLiteral(rows,
                sample.AccountInfoCode, sample.AccountInfoName, sample.OffsetAccountInfoCode, sample.OffsetAccountInfoName,
                sample.BillingType + "-" + sample.WageTypeName + "-For the month of " + voucherDate,
                sample.CostElement1Code, sample.CostElement2Code, sample.CostElement3Code,
                "KD", amount, amount > 0 ? "D" : "C"));
        }

        // Legacy lines 1886-1923 -- Closing/NetPay Entry.
        var totalAmount = provisionLines.Sum(l => l.Amount);
        if (totalAmount != 0)
        {
            var closingAccount = await _accountInfoRepository.FirstOrDefaultAsync(a => a.Code == ClosingAccountCode);

            rows.Add(NewRowNullLiteral(rows,
                closingAccount?.Code, closingAccount?.NameEn, null, null,
                "Provision-Total Annual Leave Provision-For the month of " + voucherDate,
                null, null, null,
                "KD", totalAmount, totalAmount > 0 ? "C" : "D"));
        }

        return rows;
    }
}
