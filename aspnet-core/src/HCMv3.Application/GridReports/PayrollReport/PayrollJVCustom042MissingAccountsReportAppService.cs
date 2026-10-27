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
using HCMv3.GridReports.Exporting;
using HCMv3.GridReports.PayrollReport.Dto;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// Payroll JV (Custom 042) "Missing Accounts" analysis grid report -- port of legacy
/// <c>uscRptPayrollJV_Custom042.ascx.cs</c>, <c>cmbType == "Missing Accounts"</c> branch of
/// <c>Table1()</c> (lines 3068-3137): an exception listing, ONE ROW PER RAW salary-register/wage-account
/// line (no grouping, unlike every posting format), of every line whose wage-account mapping has no
/// AccountInfo.
///
/// DIVERGENCE (agreed for this wave): RLS is applied -- see <see cref="PayrollJVPostingLineLoader"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_PayrollJVCustom042MissingAccounts)]
public class PayrollJVCustom042MissingAccountsReportAppService
    : PayrollJVCustom042ReportAppServiceBase, IPayrollJVCustom042MissingAccountsReportAppService
{
    private readonly PayrollJVPostingLineLoader _postingLineLoader;

    public PayrollJVCustom042MissingAccountsReportAppService(PayrollJVPostingLineLoader postingLineLoader)
    {
        _postingLineLoader = postingLineLoader;
    }

    protected override string LegacyScreenName => "uscRptPayrollJV_Custom042";

    protected override string ScreenCaption => "Payroll JV (Custom 042) - Missing Accounts";

    [HttpPost]
    public async Task<PagedResultDto<PayrollJVCustom042MissingAccountsRowDto>> GetPayrollJVCustom042MissingAccountsReport(
        GetPayrollJVCustom042MissingAccountsReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);

        var paged = rows.AsQueryable().OrderBy(input.Sorting).PageBy(input).ToList();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildJvParameterString(input), rows.Count);

        return new PagedResultDto<PayrollJVCustom042MissingAccountsRowDto>(rows.Count, paged);
    }

    [HttpPost]
    public async Task<FileDto> GetPayrollJVCustom042MissingAccountsReportToExcel(
        GetPayrollJVCustom042MissingAccountsReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);
        var ordered = rows.AsQueryable().OrderBy(input.Sorting).ToList();

        return await ExportToFileAsync(
            "PayrollJVCustom042MissingAccounts.xlsx",
            ordered,
            GetExportColumns(),
            BuildJvParameterString(input));
    }

    private async Task<List<PayrollJVCustom042MissingAccountsRowDto>> BuildRowsAsync(
        GetPayrollJVCustom042MissingAccountsReportInput input)
    {
        var endDate = ResolveEndDate(input.Month, input.Year);

        var lines = await _postingLineLoader.LoadAsync(
            endDate,
            Array.Empty<string>(),
            input.PayrollRunType,
            input.PayrollAreaId.HasValue ? new[] { input.PayrollAreaId.Value } : Array.Empty<int>(),
            input.ResolveEmployeeCodes(),
            await GetRlsEmployeeIdsAsync(),
            PayrollJVWageTypeScope.EarningDeductionAndAllProvision,
            await ResolveWageAccountSourceAsync());

        var rows = new List<PayrollJVCustom042MissingAccountsRowDto>();

        // Legacy lines 3111-3134: ONE row per raw line (no grouping), for lines with no AccountInfo.
        foreach (var line in lines.Where(l => l.AccountInfoId == null))
        {
            rows.Add(new PayrollJVCustom042MissingAccountsRowDto
            {
                Id = rows.Count + 1,
                LineNumber = rows.Count + 1,
                EmployeeCode = line.EmployeeCode,
                ForPeriod = line.ForPeriod,
                WageTypeName = line.WageTypeName,
                AccountInfoCode = line.AccountInfoCode ?? string.Empty,
                AccountInfoName = line.AccountInfoName ?? string.Empty,
                OffsetAccountInfoCode = line.OffsetAccountInfoCode ?? string.Empty,
                OffsetAccountInfoName = line.OffsetAccountInfoName ?? string.Empty,
                Division = line.CostElement1Code ?? string.Empty,
                Department = line.CostElement2Code ?? string.Empty,
                Analysis = line.CostElement3Code ?? string.Empty,
                CostPercentage = line.CostPercentage,
                Amount = line.Amount
            });
        }

        return rows;
    }

    private static List<GridReportColumn<PayrollJVCustom042MissingAccountsRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<PayrollJVCustom042MissingAccountsRowDto>>
        {
            new("SrNo", r => r.LineNumber),
            new("EmpCode", r => r.EmployeeCode),
            new("ForPeriod", r => r.ForPeriod),
            new("WageType", r => r.WageTypeName),
            new("AccountInfoCode", r => r.AccountInfoCode),
            new("AccountInfoName", r => r.AccountInfoName),
            new("OffsetAccountInfoCode", r => r.OffsetAccountInfoCode),
            new("OffsetAccountInfoName", r => r.OffsetAccountInfoName),
            new("Division", r => r.Division),
            new("Department", r => r.Department),
            new("Analysis1", r => r.Analysis),
            new("CostPercentage", r => r.CostPercentage),
            new("Amount", r => r.Amount)
        };
    }
}
