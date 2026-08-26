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
using HCMv3.GridReports.Exporting;
using HCMv3.GridReports.PayrollReport.Dto;
using HCMv3.PayrollManagement;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// Payroll JV (Custom 016) "Detail" analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPayrollJV_Custom016.ascx(.cs)</c>,
/// method <c>Table1()</c>, <c>cmbType.Text.Contains("Detail")</c> branch, <c>emppayrollArea.Name == "BMC"</c>,
/// <c>IsDetail == true</c> column set (lines 1176-1445 shared summary + 1527-1572 detail rows).
///
/// SUPERADMIN-ONLY (legacy <c>BindType()</c>, lines 250-256): only reachable when the logged-in user is
/// the built-in SuperAdmin, but still a LIVE, real format -- ported like any other.
///
/// BMC ONLY: legacy's MYB branch (line 1579) requires <c>cmbType.Text.Contains("New")</c> or
/// <c>Contains("Indemnity Pay Adjust")</c> -- "Detail" satisfies neither, so selecting "Detail" while on
/// the MYB payroll area produces NO output in legacy. That combination is a dead end, not ported.
///
/// SHARES the cost-center summary section verbatim with
/// <see cref="PayrollJVCustom016MainFormatBmcReportAppService"/> (factored into
/// <see cref="PayrollJVCustom016BmcSummaryHelper"/>) -- only the trailing "CostCenter Wise Debit" section
/// differs: this format emits ONE row per un-grouped posting line (minus Net Pay) instead of grouping by
/// (CostCenter, Account, OffsetAccount) and excluding ten more wage types.
///
/// DIVERGENCE (agreed for this wave): RLS is applied -- see <see cref="PayrollJVCustom016PostingLineLoader"/>
/// remarks.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_PayrollJVCustom016Detail)]
public class PayrollJVCustom016DetailReportAppService
    : GridReportAppServiceBase, IPayrollJVCustom016DetailReportAppService
{
    private const string BmcPayrollAreaName = "BMC";

    /// <summary>Legacy line 1531: only Net Pay is excluded from the per-line detail section.</summary>
    private const string NetPayWageTypeCode = "9000";

    private readonly PayrollJVCustom016PostingLineLoader _loader;
    private readonly IRepository<PayrollArea> _payrollAreaRepository;
    private readonly IRepository<WageType> _wageTypeRepository;

    public PayrollJVCustom016DetailReportAppService(
        PayrollJVCustom016PostingLineLoader loader,
        IRepository<PayrollArea> payrollAreaRepository,
        IRepository<WageType> wageTypeRepository)
    {
        _loader = loader;
        _payrollAreaRepository = payrollAreaRepository;
        _wageTypeRepository = wageTypeRepository;
    }

    protected override string LegacyScreenName => "uscRptPayrollJV_Custom016";

    protected override string ScreenCaption => "Payroll JV (Custom 016) - Detail";

    [HttpPost]
    public async Task<PagedResultDto<PayrollJVCustom016DetailRowDto>> GetPayrollJVCustom016DetailReport(
        GetPayrollJVCustom016MainFormatReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);

        var paged = rows.AsQueryable().OrderBy(input.Sorting).PageBy(input).ToList();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildJvParameterString(input), rows.Count);

        return new PagedResultDto<PayrollJVCustom016DetailRowDto>(rows.Count, paged);
    }

    [HttpPost]
    public async Task<FileDto> GetPayrollJVCustom016DetailReportToExcel(
        GetPayrollJVCustom016MainFormatReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the whole filtered data set.
        var ordered = rows.AsQueryable().OrderBy(input.Sorting).ToList();

        return await ExportToFileAsync(
            "PayrollJVCustom016Detail.xlsx", ordered, ExportColumns, BuildJvParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND row construction, shared by the paged read and the
    /// export. Ports legacy lines 1176-1445 + 1527-1572.
    /// </summary>
    private async Task<List<PayrollJVCustom016DetailRowDto>> BuildRowsAsync(
        GetPayrollJVCustom016MainFormatReportInput input)
    {
        var endDate = ResolveEndDate(input);
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();

        var bmcAreaId = await _payrollAreaRepository.GetAll()
            .Where(a => a.Name == BmcPayrollAreaName)
            .Select(a => (int?)a.Id)
            .FirstOrDefaultAsync();
        if (bmcAreaId == null)
        {
            return new List<PayrollJVCustom016DetailRowDto>();
        }

        var ds = await _loader.LoadAsync(endDate, bmcAreaId.Value, input.PayrollRunType, employeeCodes, rlsEmployeeIds);

        var fixedName = await ResolveFixedWageTypeNameAsync();
        var suffix = endDate.ToString("MMM/yyyy").ToUpperInvariant();

        var rows = new List<PayrollJVCustom016DetailRowDto>();
        var rowNumber = 0;

        // Legacy lines 1218-1445 (shared verbatim with Main Format BMC).
        foreach (var summary in PayrollJVCustom016BmcSummaryHelper.BuildSummary(ds, fixedName))
        {
            rowNumber++;
            rows.Add(new PayrollJVCustom016DetailRowDto
            {
                Id = rowNumber,
                LineNumber = rowNumber,
                Date = endDate,
                Account = summary.Account,
                Suffix = suffix,
                Amount = summary.Amount,
                Narrative = summary.Narrative
            });
        }

        // Legacy lines 1527-1572 (IsDetail == true): one row per un-grouped posting line, minus Net Pay.
        var detailLines = ds
            .Where(l => l.WageTypeCode != NetPayWageTypeCode)
            .Select(l => new
            {
                Line = l,
                CostAmount = l.BillingType == BillingType.Deduction ? -1 * l.CostAmount : l.CostAmount
            })
            .Where(l => l.CostAmount != 0)
            .ToList();

        foreach (var detail in detailLines)
        {
            rowNumber++;
            rows.Add(new PayrollJVCustom016DetailRowDto
            {
                Id = rowNumber,
                LineNumber = rowNumber,
                EmployeeCode = detail.Line.EmployeeCode,
                WageTypeName = detail.Line.WageTypeName,
                CostCenterCode = detail.Line.CostCenterCode,
                Date = endDate,
                Account = (detail.Line.CostCenterCode ?? string.Empty).PadLeft(4, '0') + " "
                          + (detail.Line.AccountInfoCode ?? string.Empty).PadLeft(8, '0'),
                Suffix = suffix,
                Amount = detail.CostAmount,
                Narrative = "SAL " + (detail.Line.AccountInfoName ?? string.Empty)
            });
        }

        return rows;
    }

    /// <summary>Legacy lines 1265, 1735, 1855: narratives use a FIXED wage-type code's name -- see
    /// <see cref="PayrollJVCustom016MainFormatBmcReportAppService.ResolveFixedWageTypeNameAsync"/>.</summary>
    private async Task<Func<string, string>> ResolveFixedWageTypeNameAsync()
    {
        var names = await _wageTypeRepository.GetAll()
            .Where(w => w.Code == "1003" || w.Code == "15" || w.Code == "1010")
            .Select(w => new { w.Code, w.NameEn })
            .ToListAsync();

        var lookup = names.ToDictionary(w => w.Code, w => w.NameEn);
        return code => lookup.TryGetValue(code, out var name) ? name : string.Empty;
    }

    /// <summary>Legacy <c>GetTable()</c>: blank month/year mean "now"; an invalid pair fell back to 1900.</summary>
    private DateTime ResolveEndDate(GetPayrollJVCustom016MainFormatReportInput input)
    {
        var month = input.Month ?? Today.Month;
        var year = input.Year ?? Today.Year;

        try
        {
            return new DateTime(year, month, DateTime.DaysInMonth(year, month)).Date;
        }
        catch (ArgumentOutOfRangeException)
        {
            return new DateTime(1900, 1, 31).Date;
        }
    }

    /// <summary>Legacy <c>GetParameterStr()</c>: Payroll Area | Run Type | Month | Year | Type | Emp Code.</summary>
    private string BuildJvParameterString(GetPayrollJVCustom016MainFormatReportInput input)
    {
        var parts = new List<string> { "Payroll Area : " + BmcPayrollAreaName };

        if (input.PayrollRunType.HasValue)
        {
            parts.Add("Run Type : " + input.PayrollRunType.Value);
        }

        parts.Add("Month : " + (input.Month ?? Today.Month));
        parts.Add("Year : " + (input.Year ?? Today.Year));
        parts.Add("Type : Detail");

        if (!string.IsNullOrWhiteSpace(input.EmployeeCode))
        {
            parts.Add("Employee Code : " + input.EmployeeCode);
        }

        return string.Join(" | ", parts);
    }

    private static readonly List<GridReportColumn<PayrollJVCustom016DetailRowDto>> ExportColumns = new()
    {
        new("Oid", r => r.LineNumber),
        new("EmpCode", r => r.EmployeeCode),
        new("WageType", r => r.WageTypeName),
        new("CostCenter", r => r.CostCenterCode),
        new("Journal Ref", r => r.JournalRef),
        new("Date", r => r.Date),
        new("Account", r => r.Account),
        new("Suffix", r => r.Suffix),
        new("Amount", r => r.Amount),
        new("Narrative", r => r.Narrative)
    };
}
