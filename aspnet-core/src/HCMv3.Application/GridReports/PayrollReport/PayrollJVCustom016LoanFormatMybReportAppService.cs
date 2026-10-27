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
/// Payroll JV (Custom 016) "Loan Format - MYB" analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPayrollJV_Custom016.ascx(.cs)</c>,
/// method <c>Table1()</c>, <c>cmbType == "Loan Format"</c> branch, payroll area whose name contains "MYB"
/// column set (lines 2194-2298).
///
/// SHAPE: same <see cref="PayrollJVCustom016PostingLineLoader"/> preprocessing and the same six Loan wage
/// types as the BMC report, but grouped WITHOUT CostCenter (legacy line 2233 groups by Employee/WageType/
/// Account/OffsetAccount only -- CostCenter is fixed to the literal "0000" on this format) and each group
/// emits a DEBIT row (against the Offset account) and a mirrored CREDIT row (against the account),
/// instead of BMC's single signed row.
///
/// PAYROLL AREA IS NOT A USER FILTER HERE -- see <see cref="PayrollJVCustom016LoanFormatBmcReportAppService"/>
/// remarks for why; this report locks to every payroll area whose Name contains "MYB" (legacy line 2195:
/// <c>emppayrollArea.Name.ToUpper().Contains("MYB")</c> -- note legacy resolves a SINGLE selected payroll
/// area from the combo, so in practice this matches at most one row, but the containment test is kept
/// faithful rather than an exact-name lock).
///
/// DIVERGENCE (agreed for this wave): RLS is applied -- see
/// <see cref="PayrollJVCustom016PostingLineLoader"/> remarks.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_PayrollJVCustom016LoanFormatMyb)]
public class PayrollJVCustom016LoanFormatMybReportAppService
    : GridReportAppServiceBase, IPayrollJVCustom016LoanFormatMybReportAppService
{
    /// <summary>Legacy lines 579-584: the six wage types this format admits.</summary>
    private static readonly string[] LoanWageTypeCodes = { "2001", "8001", "2028", "8028", "2014", "8014" };

    /// <summary>Legacy line 2195: <c>emppayrollArea.Name.ToUpper().Contains("MYB")</c>.</summary>
    private const string MybPayrollAreaNameFragment = "MYB";

    private readonly PayrollJVCustom016PostingLineLoader _loader;
    private readonly IRepository<PayrollArea> _payrollAreaRepository;

    public PayrollJVCustom016LoanFormatMybReportAppService(
        PayrollJVCustom016PostingLineLoader loader,
        IRepository<PayrollArea> payrollAreaRepository)
    {
        _loader = loader;
        _payrollAreaRepository = payrollAreaRepository;
    }

    protected override string LegacyScreenName => "uscRptPayrollJV_Custom016";

    protected override string ScreenCaption => "Payroll JV (Custom 016) - Loan Format - MYB";

    [HttpPost]
    public async Task<PagedResultDto<PayrollJVCustom016LoanFormatMybRowDto>>
        GetPayrollJVCustom016LoanFormatMybReport(GetPayrollJVCustom016LoanFormatReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);

        var paged = rows.AsQueryable().OrderBy(input.Sorting).PageBy(input).ToList();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildJvParameterString(input), rows.Count);

        return new PagedResultDto<PayrollJVCustom016LoanFormatMybRowDto>(rows.Count, paged);
    }

    [HttpPost]
    public async Task<FileDto> GetPayrollJVCustom016LoanFormatMybReportToExcel(
        GetPayrollJVCustom016LoanFormatReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the whole filtered data set.
        var ordered = rows.AsQueryable().OrderBy(input.Sorting).ToList();

        return await ExportToFileAsync(
            "PayrollJVCustom016LoanFormatMyb.xlsx",
            ordered,
            ExportColumns,
            BuildJvParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND row construction, shared by the paged read and the
    /// export. Ports legacy lines 2083-2088 + 2194-2298.
    /// </summary>
    private async Task<List<PayrollJVCustom016LoanFormatMybRowDto>> BuildRowsAsync(
        GetPayrollJVCustom016LoanFormatReportInput input)
    {
        var endDate = ResolveEndDate(input);
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();

        var mybAreaId = await _payrollAreaRepository.GetAll()
            .Where(a => EF.Functions.Like(a.Name, "%" + MybPayrollAreaNameFragment + "%"))
            .Select(a => (int?)a.Id)
            .FirstOrDefaultAsync();
        if (mybAreaId == null)
        {
            return new List<PayrollJVCustom016LoanFormatMybRowDto>();
        }

        var lines = await _loader.LoadAsync(endDate, mybAreaId.Value, input.PayrollRunType, employeeCodes, rlsEmployeeIds);

        // Legacy line 2086: take ONLY the Loan wage types.
        var loanLines = lines.Where(l => LoanWageTypeCodes.Contains(l.WageTypeCode)).ToList();

        // Legacy lines 2232-2242: grouped WITHOUT CostCenter.
        var groups = loanLines
            .GroupBy(l => (l.EmployeeId, l.WageTypeId, l.AccountInfoId, l.OffsetAccountInfoId))
            .Select(g => new { Sample = g.First(), CostAmount = g.Sum(l => l.CostAmount) })
            .Where(g => g.CostAmount != 0)
            .ToList();

        var reference = endDate.ToString("MMM/yyyy").ToUpperInvariant() + " SAL";
        var rows = new List<PayrollJVCustom016LoanFormatMybRowDto>();
        var rowNumber = 0;

        foreach (var group in groups)
        {
            var sample = group.Sample;

            // Legacy lines 2246-2270: Debit row against the Offset account.
            rowNumber++;
            rows.Add(new PayrollJVCustom016LoanFormatMybRowDto
            {
                Id = rowNumber,
                LineNumber = rowNumber,
                Exp = (sample.OffsetAccountInfoCode ?? string.Empty).PadLeft(6, '0'),
                Date = endDate,
                DocDate = endDate,
                Suffix = sample.EmployeeCode,
                SlPlAccount = sample.EmployeeReferenceCode,
                Reference = reference,
                Narrative = sample.NarrativeWageTypeName,
                Debit = group.CostAmount > 0 ? group.CostAmount : 0,
                Credit = group.CostAmount < 0 ? -1 * group.CostAmount : 0
            });

            // Legacy lines 2272-2293: mirrored Credit row against the Account.
            rowNumber++;
            rows.Add(new PayrollJVCustom016LoanFormatMybRowDto
            {
                Id = rowNumber,
                LineNumber = rowNumber,
                Exp = (sample.AccountInfoCode ?? string.Empty).PadLeft(6, '0'),
                Date = endDate,
                DocDate = endDate,
                Suffix = sample.EmployeeCode,
                SlPlAccount = sample.EmployeeReferenceCode,
                Reference = reference,
                Narrative = reference + " LOAN",
                Debit = group.CostAmount < 0 ? -1 * group.CostAmount : 0,
                Credit = group.CostAmount > 0 ? group.CostAmount : 0
            });
        }

        return rows;
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
        var parts = new List<string> { "Payroll Area : " + MybPayrollAreaNameFragment };

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

    private static readonly List<GridReportColumn<PayrollJVCustom016LoanFormatMybRowDto>> ExportColumns = new()
    {
        new("Oid", r => r.LineNumber),
        new("CC", r => r.CostCenter),
        new("Exp", r => r.Exp),
        new("Date", r => r.Date),
        new("Docdate", r => r.DocDate),
        new("Srce", r => r.Source),
        new("Co", r => r.Company),
        new("Suffix", r => r.Suffix),
        new("SLPLAC", r => r.SlPlAccount),
        new("Reference", r => r.Reference),
        new("Narrative", r => r.Narrative),
        new("Reconciledreference", r => r.ReconciledReference),
        new("R", r => r.RColumn),
        new("Debit", r => r.Debit),
        new("Credit", r => r.Credit)
    };
}
