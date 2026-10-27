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
/// Payroll JV (Custom 016) "Main Format - BMC" analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPayrollJV_Custom016.ascx(.cs)</c>,
/// method <c>Table1()</c>, <c>(cmbType.Text.Contains("Main Format") || cmbType.Text.Contains("Detail")) &amp;&amp;
/// !cmbType.Text.Contains("Indemnity Pay Adjust")</c> branch, <c>emppayrollArea.Name == "BMC"</c>,
/// <c>IsDetail == false</c> column set (lines 1176-1524).
///
/// COVERS BOTH "Main Format" AND "Main Format-New" FOR BMC: nothing in the BMC branch tests for the
/// "-New" suffix (only the MYB branch does, legacy line 1579), so for the BMC payroll area those two
/// <c>cmbType</c> selections produce IDENTICAL output. Rather than publish two reports that always agree,
/// this single report covers both -- see <see cref="PayrollJVCustom016MainFormatMybReportAppService"/> for
/// the MYB counterpart (which DOES require "-New") and <see cref="PayrollJVCustom016DetailReportAppService"/>
/// for the <c>IsDetail == true</c> sibling that shares this report's cost-center summary section verbatim
/// (factored into <see cref="PayrollJVCustom016BmcSummaryHelper"/>).
///
/// PAYROLL AREA IS NOT A USER FILTER HERE -- same convention as the already-ported Loan Format (see
/// <see cref="PayrollJVCustom016LoanFormatBmcReportAppService"/> remarks).
///
/// DIVERGENCE (agreed for this wave): RLS is applied -- see <see cref="PayrollJVCustom016PostingLineLoader"/>
/// remarks.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_PayrollJVCustom016MainFormatBmc)]
public class PayrollJVCustom016MainFormatBmcReportAppService
    : GridReportAppServiceBase, IPayrollJVCustom016MainFormatBmcReportAppService
{
    private const string BmcPayrollAreaName = "BMC";

    /// <summary>Legacy lines 1456-1473: wage types excluded from the "CostCenter Wise Debit" (non-Detail) section.</summary>
    private static readonly string[] NotInDebitWageTypeCodes =
        { "1003", "7003", "1052", "7052", "1061", "7061", "1010", "7010", "15", "16", "9000" };

    private readonly PayrollJVCustom016PostingLineLoader _loader;
    private readonly IRepository<PayrollArea> _payrollAreaRepository;
    private readonly IRepository<WageType> _wageTypeRepository;

    public PayrollJVCustom016MainFormatBmcReportAppService(
        PayrollJVCustom016PostingLineLoader loader,
        IRepository<PayrollArea> payrollAreaRepository,
        IRepository<WageType> wageTypeRepository)
    {
        _loader = loader;
        _payrollAreaRepository = payrollAreaRepository;
        _wageTypeRepository = wageTypeRepository;
    }

    protected override string LegacyScreenName => "uscRptPayrollJV_Custom016";

    protected override string ScreenCaption => "Payroll JV (Custom 016) - Main Format - BMC";

    [HttpPost]
    public async Task<PagedResultDto<PayrollJVCustom016MainFormatBmcRowDto>>
        GetPayrollJVCustom016MainFormatBmcReport(GetPayrollJVCustom016MainFormatReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);

        var paged = rows.AsQueryable().OrderBy(input.Sorting).PageBy(input).ToList();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildJvParameterString(input), rows.Count);

        return new PagedResultDto<PayrollJVCustom016MainFormatBmcRowDto>(rows.Count, paged);
    }

    [HttpPost]
    public async Task<FileDto> GetPayrollJVCustom016MainFormatBmcReportToExcel(
        GetPayrollJVCustom016MainFormatReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the whole filtered data set.
        var ordered = rows.AsQueryable().OrderBy(input.Sorting).ToList();

        return await ExportToFileAsync(
            "PayrollJVCustom016MainFormatBmc.xlsx", ordered, ExportColumns, BuildJvParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND row construction, shared by the paged read and the
    /// export. Ports legacy lines 1176-1524.
    /// </summary>
    private async Task<List<PayrollJVCustom016MainFormatBmcRowDto>> BuildRowsAsync(
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
            return new List<PayrollJVCustom016MainFormatBmcRowDto>();
        }

        var ds = await _loader.LoadAsync(endDate, bmcAreaId.Value, input.PayrollRunType, employeeCodes, rlsEmployeeIds);

        var fixedName = await ResolveFixedWageTypeNameAsync();
        var suffix = endDate.ToString("MMM/yyyy").ToUpperInvariant();

        var rows = new List<PayrollJVCustom016MainFormatBmcRowDto>();
        var rowNumber = 0;

        // Legacy lines 1218-1445.
        foreach (var summary in PayrollJVCustom016BmcSummaryHelper.BuildSummary(ds, fixedName))
        {
            rowNumber++;
            rows.Add(new PayrollJVCustom016MainFormatBmcRowDto
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

        // Legacy lines 1453-1524 (IsDetail == false): grouped CostCenter-wise Debit.
        var debitGroups = ds
            .Where(l => !NotInDebitWageTypeCodes.Contains(l.WageTypeCode))
            .GroupBy(l => (l.CostCenterId, l.AccountInfoId, l.OffsetAccountInfoId))
            .Select(g => new
            {
                Sample = g.First(),
                CostAmount = g.Sum(l => l.BillingType == BillingType.Deduction ? -1 * l.CostAmount : l.CostAmount)
            })
            .Where(g => g.CostAmount != 0)
            .ToList();

        foreach (var group in debitGroups)
        {
            rowNumber++;
            rows.Add(new PayrollJVCustom016MainFormatBmcRowDto
            {
                Id = rowNumber,
                LineNumber = rowNumber,
                Date = endDate,
                Account = (group.Sample.CostCenterCode ?? string.Empty).PadLeft(4, '0') + " "
                          + (group.Sample.AccountInfoCode ?? string.Empty).PadLeft(8, '0'),
                Suffix = suffix,
                Amount = group.CostAmount,
                Narrative = "SAL " + (group.Sample.AccountInfoName ?? string.Empty)
            });
        }

        return rows;
    }

    /// <summary>
    /// Legacy lines 1265, 1735, 1855: three narratives use the NAME OF A FIXED WAGE-TYPE CODE (looked up
    /// independently), not the name of whichever matching line happened to be found -- e.g. the Annual
    /// Leave Pay narrative always says wage type "1003"'s name even when only its arrear "7003" matched.
    /// </summary>
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
        parts.Add("Type : Main Format");

        if (!string.IsNullOrWhiteSpace(input.EmployeeCode))
        {
            parts.Add("Employee Code : " + input.EmployeeCode);
        }

        return string.Join(" | ", parts);
    }

    private static readonly List<GridReportColumn<PayrollJVCustom016MainFormatBmcRowDto>> ExportColumns = new()
    {
        new("Oid", r => r.LineNumber),
        new("Journal Ref", r => r.JournalRef),
        new("Date", r => r.Date),
        new("Account", r => r.Account),
        new("Suffix", r => r.Suffix),
        new("Amount", r => r.Amount),
        new("Narrative", r => r.Narrative)
    };
}
