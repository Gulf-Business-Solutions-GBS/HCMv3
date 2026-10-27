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
/// Payroll JV (Custom 042) "Lieu Day Leave Provision JV" analysis grid report -- port of legacy
/// <c>uscRptPayrollJV_Custom042.ascx.cs</c>, <c>cmbType == "Lieu Day Leave Provision JV"</c> branch
/// of <c>Table1()</c> (lines 2505-2791). The quota-accrual-difference algorithm is shared with the
/// "Ramadan Time Back" and "Lieu Day" variants through <see cref="LeaveProvisionQuotaCalculator"/> -- see
/// its remarks, including the flagged <c>WorkingDaysPerMonth</c> simplification. This service supplies the
/// three constants that make it THIS branch: AbsenceType code "9" (Lieu Day), provision wage type
/// "1088" and total wage type "3008" (legacy lines 2558-2560).
///
/// DIVERGENCE (agreed for this wave): RLS is applied -- see <see cref="PayrollJVPostingLineLoader"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_PayrollJVCustom042LieuDayLeaveProvision)]
public class PayrollJVCustom042LieuDayLeaveProvisionReportAppService
    : PayrollJVCustom042ReportAppServiceBase, IPayrollJVCustom042LieuDayLeaveProvisionReportAppService
{
    private const string LeaveAbsenceTypeCode = "9";
    private const string ProvisionWageTypeCode = "1088";
    private const string TotalProvisionWageTypeCode = "3008";
    private const string ClosingAccountCode = "614000";

    private readonly LeaveProvisionQuotaCalculator _quotaCalculator;
    private readonly PayrollJVPostingLineLoader _postingLineLoader;
    private readonly IRepository<AccountInfo> _accountInfoRepository;

    public PayrollJVCustom042LieuDayLeaveProvisionReportAppService(
        LeaveProvisionQuotaCalculator quotaCalculator,
        PayrollJVPostingLineLoader postingLineLoader,
        IRepository<AccountInfo> accountInfoRepository)
    {
        _quotaCalculator = quotaCalculator;
        _postingLineLoader = postingLineLoader;
        _accountInfoRepository = accountInfoRepository;
    }

    protected override string LegacyScreenName => "uscRptPayrollJV_Custom042";

    protected override string ScreenCaption => "Payroll JV (Custom 042) - Lieu Day Leave Provision JV";

    [HttpPost]
    public async Task<PagedResultDto<PayrollJVCustom042ReportRowDto>> GetPayrollJVCustom042LieuDayLeaveProvisionReport(
        GetPayrollJVCustom042LieuDayLeaveProvisionReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);

        var paged = rows.AsQueryable().OrderBy(input.Sorting).PageBy(input).ToList();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildJvParameterString(input), rows.Count);

        return new PagedResultDto<PayrollJVCustom042ReportRowDto>(rows.Count, paged);
    }

    [HttpPost]
    public async Task<FileDto> GetPayrollJVCustom042LieuDayLeaveProvisionReportToExcel(
        GetPayrollJVCustom042LieuDayLeaveProvisionReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);
        var ordered = rows.AsQueryable().OrderBy(input.Sorting).ToList();

        return await ExportToFileAsync(
            "PayrollJVCustom042LieuDayLeaveProvision.xlsx",
            ordered,
            GetPostingExportColumns(),
            BuildJvParameterString(input));
    }

    private async Task<List<PayrollJVCustom042ReportRowDto>> BuildRowsAsync(
        GetPayrollJVCustom042LieuDayLeaveProvisionReportInput input)
    {
        var endDate = ResolveEndDate(input.Month, input.Year);
        var voucherDate = VoucherDate(endDate);
        var timeSettings = await SysSettingsAccessor.GetTimeSettingsAsync();
        var earliestEvaluationDate = timeSettings?.EarliestEvaluationDate ?? default;

        var context = await _quotaCalculator.ResolveContextAsync(
            endDate, LeaveAbsenceTypeCode, ProvisionWageTypeCode, TotalProvisionWageTypeCode);

        var amounts = await _quotaCalculator.CalculateAmountsAsync(
            context, endDate, earliestEvaluationDate, await GetRlsEmployeeIdsAsync(), input.ResolveEmployeeCodes());

        var rows = new List<PayrollJVCustom042ReportRowDto>();
        if (context.ProvisionWageTypeId == null || amounts.Count == 0)
        {
            return rows;
        }

        var lookup = await _postingLineLoader.LoadWageAccountLookupAsync(
            endDate,
            input.ResolveEmployeeCodes(),
            input.PayrollAreaId.HasValue ? new[] { input.PayrollAreaId.Value } : Array.Empty<int>(),
            await ResolveWageAccountSourceAsync());

        // Legacy lines 2131-2145 -- join each employee's provision amount to their wage account for the
        // provision wage type, then group by account/offset-account/cost elements.
        var joined = amounts.SelectMany(a => lookup.MatchOrNull(a.EmployeeId, context.ProvisionWageTypeId.Value)
            .Select(acc => new { a.Amount, Account = acc }));

        foreach (var group in joined.GroupBy(j => (j.Account?.AccountInfoId, j.Account?.OffsetAccountInfoId,
                     j.Account?.CostElement1Id, j.Account?.CostElement2Id, j.Account?.CostElement3Id)))
        {
            var amount = group.Sum(j => j.Amount);
            if (amount == 0)
            {
                continue;
            }

            var sample = group.First().Account;
            rows.Add(NewRowNullLiteral(rows,
                sample?.AccountInfoCode, sample?.AccountInfoName, sample?.OffsetAccountInfoCode, sample?.OffsetAccountInfoName,
                context.ProvisionWageTypeBillingType + "-" + context.ProvisionWageTypeName + "-For the month of " + voucherDate,
                sample?.CostElement1Code, sample?.CostElement2Code, sample?.CostElement3Code,
                "KD", amount, amount > 0 ? "D" : "C"));
        }

        // Legacy lines 2176-2212 -- Closing entry, booked to the hard-coded account "614000".
        var totalAmount = amounts.Sum(a => a.Amount);
        if (totalAmount != 0)
        {
            var closingAccount = await _accountInfoRepository.FirstOrDefaultAsync(a => a.Code == ClosingAccountCode);

            rows.Add(NewRowNullLiteral(rows,
                closingAccount?.Code, closingAccount?.NameEn, null, null,
                "Provision-" + context.TotalWageTypeName + "-For the month of " + voucherDate,
                null, null, null,
                "KD", totalAmount, totalAmount > 0 ? "C" : "D"));
        }

        return rows;
    }
}
