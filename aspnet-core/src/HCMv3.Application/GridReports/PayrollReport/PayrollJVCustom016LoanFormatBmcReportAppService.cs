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
/// Payroll JV (Custom 016) "Loan Format - BMC" analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPayrollJV_Custom016.ascx(.cs)</c>,
/// method <c>Table1()</c>, <c>cmbType == "Loan Format"</c> branch, <c>emppayrollArea.Name == "BMC"</c>
/// column set (lines 2091-2191).
///
/// SHAPE: filters + left-joins via <see cref="PayrollJVCustom016PostingLineLoader"/> (the common
/// preprocessing pipeline shared by nearly every <c>cmbType</c> variant of this legacy screen -- see its
/// remarks), narrows to the six Loan wage types, groups by (Employee, WageType, CostCenter, Account,
/// OffsetAccount) and emits ONE row per non-zero group.
///
/// PAYROLL AREA IS NOT A USER FILTER HERE: legacy's <c>cmbPayrollArea</c> is a full picker, but this
/// format only ever produces output for the payroll area literally named "BMC" (legacy line 2092) -- any
/// other selection left the DataTable with no columns and no rows. Rather than expose a picker that is
/// silently empty for every value but one, this report locks the payroll area by name server-side, the
/// same convention <c>PayrollJVCustom019PayrollAreaScope</c> already uses for its literal-name lock.
///
/// TWO REPORTS, NOT ONE PARAMETERIZED REPORT: legacy's MYB column set for this same <c>cmbType</c> value
/// (lines 2194-2298) is structurally different (15 columns with separate Debit/Credit, vs. this format's
/// 10 columns with one signed Amount-shaped pair) -- see
/// <see cref="PayrollJVCustom016LoanFormatMybReportAppService"/> for that report.
///
/// DIVERGENCE (agreed for this wave): RLS is applied -- see
/// <see cref="PayrollJVCustom016PostingLineLoader"/> remarks.
///
/// NOT PORTED in this wave (see the loader's class remarks and the porting-agent final report): the other
/// ~9 <c>cmbType</c> variants of this legacy screen ("Main Format", "Main Format-New", "Detail", "NetPay
/// Difference", "Leave Provision", "Indemnity Provision", "Prov Difference-ALL/-Active/-Inactive",
/// "Indemnity Pay Adjust", "Indemnity Pay Adjust-Detail").
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_PayrollJVCustom016LoanFormatBmc)]
public class PayrollJVCustom016LoanFormatBmcReportAppService
    : GridReportAppServiceBase, IPayrollJVCustom016LoanFormatBmcReportAppService
{
    /// <summary>Legacy lines 579-584: the six wage types this format admits.</summary>
    private static readonly string[] LoanWageTypeCodes = { "2001", "8001", "2028", "8028", "2014", "8014" };

    /// <summary>Legacy line 2092: this format only fires for the payroll area literally named this.</summary>
    private const string BmcPayrollAreaName = "BMC";

    private readonly PayrollJVCustom016PostingLineLoader _loader;
    private readonly IRepository<PayrollArea> _payrollAreaRepository;

    public PayrollJVCustom016LoanFormatBmcReportAppService(
        PayrollJVCustom016PostingLineLoader loader,
        IRepository<PayrollArea> payrollAreaRepository)
    {
        _loader = loader;
        _payrollAreaRepository = payrollAreaRepository;
    }

    protected override string LegacyScreenName => "uscRptPayrollJV_Custom016";

    protected override string ScreenCaption => "Payroll JV (Custom 016) - Loan Format - BMC";

    [HttpPost]
    public async Task<PagedResultDto<PayrollJVCustom016LoanFormatBmcRowDto>>
        GetPayrollJVCustom016LoanFormatBmcReport(GetPayrollJVCustom016LoanFormatReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);

        var paged = rows.AsQueryable().OrderBy(input.Sorting).PageBy(input).ToList();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildJvParameterString(input), rows.Count);

        return new PagedResultDto<PayrollJVCustom016LoanFormatBmcRowDto>(rows.Count, paged);
    }

    [HttpPost]
    public async Task<FileDto> GetPayrollJVCustom016LoanFormatBmcReportToExcel(
        GetPayrollJVCustom016LoanFormatReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the whole filtered data set.
        var ordered = rows.AsQueryable().OrderBy(input.Sorting).ToList();

        return await ExportToFileAsync(
            "PayrollJVCustom016LoanFormatBmc.xlsx",
            ordered,
            ExportColumns,
            BuildJvParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND row construction, shared by the paged read and the
    /// export. Ports legacy lines 2083-2191.
    /// </summary>
    private async Task<List<PayrollJVCustom016LoanFormatBmcRowDto>> BuildRowsAsync(
        GetPayrollJVCustom016LoanFormatReportInput input)
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
            return new List<PayrollJVCustom016LoanFormatBmcRowDto>();
        }

        var lines = await _loader.LoadAsync(endDate, bmcAreaId.Value, input.PayrollRunType, employeeCodes, rlsEmployeeIds);

        // Legacy line 2086: take ONLY the Loan wage types.
        var loanLines = lines.Where(l => LoanWageTypeCodes.Contains(l.WageTypeCode)).ToList();

        // Legacy lines 2135-2145.
        var groups = loanLines
            .GroupBy(l => (l.EmployeeId, l.WageTypeId, l.CostCenterId, l.AccountInfoId, l.OffsetAccountInfoId))
            .Select(g => new { Sample = g.First(), CostAmount = g.Sum(l => l.CostAmount) })
            .Where(g => g.CostAmount != 0)
            .ToList();

        var rows = new List<PayrollJVCustom016LoanFormatBmcRowDto>();
        var rowNumber = 0;

        foreach (var group in groups)
        {
            rowNumber++;
            var sample = group.Sample;

            // Legacy lines 2152-2163: cost-center bucketing and branch code, by the FIRST digit of the code.
            var costCenterCode = sample.CostCenterCode ?? string.Empty;
            var bucket = BucketCostCenter(costCenterCode);
            var branch = costCenterCode.StartsWith("2") ? "2900" : "1900";

            var slPlAccount = "E8" + sample.EmployeeCode;
            var narrative = endDate.ToString("MMM/yyyy").ToUpperInvariant() + " SAL " + sample.NarrativeWageTypeName;

            rows.Add(new PayrollJVCustom016LoanFormatBmcRowDto
            {
                Id = rowNumber,
                LineNumber = rowNumber,
                SlPlAccount = slPlAccount,
                Narrative = narrative,
                DocumentDate = endDate,
                DueDate = endDate,
                Vat = 0,
                BaseValue = -1 * group.CostAmount,
                NlCode = bucket + (sample.OffsetAccountInfoCode ?? string.Empty).PadLeft(8, '0'),
                Suffix = slPlAccount,
                GoodsValue = group.CostAmount,
                Branch = branch
            });
        }

        return rows;
    }

    /// <summary>Legacy lines 2152-2161: first-digit cost-center bucketing, identical across the family.</summary>
    private static string BucketCostCenter(string code)
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

    /// <summary>Legacy <c>GetTable()</c>: blank month/year mean "now"; an invalid pair fell back to 1900.</summary>
    private DateTime ResolveEndDate(GetPayrollJVCustom016LoanFormatReportInput input)
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
    private string BuildJvParameterString(GetPayrollJVCustom016LoanFormatReportInput input)
    {
        var parts = new List<string> { "Payroll Area : " + BmcPayrollAreaName };

        if (input.PayrollRunType.HasValue)
        {
            parts.Add("Run Type : " + input.PayrollRunType.Value);
        }

        parts.Add("Month : " + (input.Month ?? Today.Month));
        parts.Add("Year : " + (input.Year ?? Today.Year));
        parts.Add("Type : Loan Format");

        if (!string.IsNullOrWhiteSpace(input.EmployeeCode))
        {
            parts.Add("Employee Code : " + input.EmployeeCode);
        }

        return string.Join(" | ", parts);
    }

    private static readonly List<GridReportColumn<PayrollJVCustom016LoanFormatBmcRowDto>> ExportColumns = new()
    {
        new("Oid", r => r.LineNumber),
        new("SLPLacc", r => r.SlPlAccount),
        new("Narrative", r => r.Narrative),
        new("MCSR", r => r.Mcsr),
        new("DocumntDate", r => r.DocumentDate),
        new("Duedate", r => r.DueDate),
        new("VAT", r => r.Vat),
        new("BaseValue", r => r.BaseValue),
        new("ECol", r => r.EColumn),
        new("NLC0DE", r => r.NlCode),
        new("Suffix", r => r.Suffix),
        new("VAT2", r => r.VatCode),
        new("GoodsValue", r => r.GoodsValue),
        new("Narrative2", r => r.Narrative),
        new("Branch", r => r.Branch)
    };
}
