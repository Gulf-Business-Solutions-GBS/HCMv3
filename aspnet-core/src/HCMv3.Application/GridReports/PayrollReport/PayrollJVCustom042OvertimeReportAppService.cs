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
using HCMv3.OrganizationManagement;
using HCMv3.PayrollManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// Payroll JV (Custom 042) "Overtime JV" analysis grid report -- port of legacy
/// <c>uscRptPayrollJV_Custom042.ascx.cs</c>, <c>cmbType == "Overtime JV"</c> branch of <c>Table1()</c>
/// (lines 2927-3065).
///
/// Two branches over the six Overtime wage-type codes (1007/7007/1008/7008/1009/7009):
/// <list type="number">
/// <item><description>Division/Dept Entry (lines 2969-3015): grouped by account/offset-account/cost
/// elements only (NOT by wage type or employee) -- legacy's own comment "Being Overtime Radiology" is the
/// literal narration text on EVERY row, reproduced verbatim even though it names a specific
/// department.</description></item>
/// <item><description>Employee/OT Entry (lines 3017-3064): grouped BY EMPLOYEE, with a hard-coded Main
/// account (516000), the employee's own AccountInfo (matched by employee CODE) as the offset, and a
/// hard-coded Analysis1 cost element (SP1).</description></item>
/// </list>
/// Unlike the other posting formats, Div/Dept/Anly1 are written as EMPTY STRING when unmapped here, not
/// the literal "NULL" (legacy lines 3002-3004 vs e.g. 1063-1065).
///
/// DIVERGENCE (agreed for this wave): RLS is applied -- see <see cref="PayrollJVPostingLineLoader"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_PayrollJVCustom042Overtime)]
public class PayrollJVCustom042OvertimeReportAppService
    : PayrollJVCustom042ReportAppServiceBase, IPayrollJVCustom042OvertimeReportAppService
{
    /// <summary>Legacy lines 585-591.</summary>
    private static readonly string[] OvertimeCodes = { "1007", "7007", "1008", "7008", "1009", "7009" };

    /// <summary>Legacy line 3018: the hard-coded Overtime GL account for the Employee/OT entry.</summary>
    private const string OvertimeAccountCode = "516000";

    /// <summary>Legacy line 3019: the hard-coded Analysis1 cost element for the Employee/OT entry.</summary>
    private const string OvertimeAnalysisCode = "SP1";

    /// <summary>Legacy lines 2998/3047: the fixed narration -- kept verbatim even though it names a
    /// specific department; this is what legacy actually books.</summary>
    private const string Narration = "Being Overtime Radiology for the month of ";

    private readonly PayrollJVPostingLineLoader _postingLineLoader;
    private readonly IRepository<AccountInfo> _accountInfoRepository;
    private readonly IRepository<CostElement> _costElementRepository;

    public PayrollJVCustom042OvertimeReportAppService(
        PayrollJVPostingLineLoader postingLineLoader,
        IRepository<AccountInfo> accountInfoRepository,
        IRepository<CostElement> costElementRepository)
    {
        _postingLineLoader = postingLineLoader;
        _accountInfoRepository = accountInfoRepository;
        _costElementRepository = costElementRepository;
    }

    protected override string LegacyScreenName => "uscRptPayrollJV_Custom042";

    protected override string ScreenCaption => "Payroll JV (Custom 042) - Overtime JV";

    [HttpPost]
    public async Task<PagedResultDto<PayrollJVCustom042ReportRowDto>> GetPayrollJVCustom042OvertimeReport(
        GetPayrollJVCustom042OvertimeReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);

        var paged = rows.AsQueryable().OrderBy(input.Sorting).PageBy(input).ToList();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildJvParameterString(input), rows.Count);

        return new PagedResultDto<PayrollJVCustom042ReportRowDto>(rows.Count, paged);
    }

    [HttpPost]
    public async Task<FileDto> GetPayrollJVCustom042OvertimeReportToExcel(GetPayrollJVCustom042OvertimeReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);
        var ordered = rows.AsQueryable().OrderBy(input.Sorting).ToList();

        return await ExportToFileAsync(
            "PayrollJVCustom042Overtime.xlsx",
            ordered,
            GetPostingExportColumns(),
            BuildJvParameterString(input));
    }

    private async Task<List<PayrollJVCustom042ReportRowDto>> BuildRowsAsync(GetPayrollJVCustom042OvertimeReportInput input)
    {
        var endDate = ResolveEndDate(input.Month, input.Year);
        var voucherDate = VoucherDate(endDate);
        var narration = Narration + voucherDate;

        var lines = await _postingLineLoader.LoadAsync(
            endDate,
            Array.Empty<string>(),
            input.PayrollRunType,
            input.PayrollAreaId.HasValue ? new[] { input.PayrollAreaId.Value } : Array.Empty<int>(),
            input.ResolveEmployeeCodes(),
            await GetRlsEmployeeIdsAsync(),
            PayrollJVWageTypeScope.EarningDeductionAndAllProvision);

        var otLines = lines
            .Where(l => l.BillingType == BillingType.Earning && OvertimeCodes.Contains(l.WageTypeCode))
            .ToList();

        var rows = new List<PayrollJVCustom042ReportRowDto>();

        // Legacy lines 2969-3015 -- Division/Dept Entry.
        foreach (var group in otLines.GroupBy(l => (l.AccountInfoId, l.OffsetAccountInfoId, l.CostElement1Id,
                     l.CostElement2Id, l.CostElement3Id)))
        {
            var amount = group.Sum(l => l.Amount);
            if (amount == 0)
            {
                continue;
            }

            var sample = group.First();
            rows.Add(NewRow(rows,
                sample.AccountInfoCode, sample.AccountInfoName, sample.OffsetAccountInfoCode, sample.OffsetAccountInfoName,
                narration,
                sample.CostElement1Code, sample.CostElement2Code, sample.CostElement3Code,
                "KD", amount, amount > 0 ? "D" : "C"));
        }

        // Legacy lines 3017-3064 -- Employee/OT Entry.
        var overtimeAccount = await _accountInfoRepository.FirstOrDefaultAsync(a => a.Code == OvertimeAccountCode);
        var analysisElement = await _costElementRepository.FirstOrDefaultAsync(c => c.Code == OvertimeAnalysisCode);

        var employeeCodes = otLines.Select(l => l.EmployeeCode).Distinct().ToList();
        var employeeAccounts = await _accountInfoRepository.GetAll()
            .Where(a => employeeCodes.Contains(a.Code))
            .ToDictionaryAsync(a => a.Code, a => a);

        foreach (var group in otLines.GroupBy(l => l.EmployeeCode))
        {
            var amount = group.Sum(l => l.Amount);
            if (amount == 0)
            {
                continue;
            }

            employeeAccounts.TryGetValue(group.Key, out var empAccount);

            rows.Add(NewRow(rows,
                overtimeAccount?.Code, overtimeAccount?.NameEn, empAccount?.Code, empAccount?.NameEn,
                narration,
                null, null, analysisElement?.Code,
                "KD", amount, amount > 0 ? "C" : "D"));
        }

        return rows;
    }
}
