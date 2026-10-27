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
/// Payroll JV (Custom 016) "Main Format-New" analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPayrollJV_Custom016.ascx(.cs)</c>,
/// method <c>Table1()</c>, <c>emppayrollArea.Name.ToUpper().Contains("MYB") &amp;&amp;
/// (cmbType.Text.Contains("New") || cmbType.Text.Contains("Indemnity Pay Adjust"))</c> branch,
/// <c>IsDetail == false</c> column set (lines 1578-2016).
///
/// ONLY "Main Format-New" REACHES THIS FOR MYB: legacy's plain "Main Format" text does not contain "New",
/// so on the MYB payroll area it satisfies neither this branch's gate nor the BMC branch's (which is
/// unconditional on payroll-area name), leaving NO output -- a legacy quirk kept faithfully by simply not
/// publishing a "Main Format - MYB" report. "Indemnity Pay Adjust" also satisfies this branch's gate, but
/// never reaches it because the OUTER <c>if</c> at legacy line 1171 already requires
/// <c>Contains("Main Format") || Contains("Detail")</c>, which "Indemnity Pay Adjust" does not (that
/// cmbType value is NOT ported in this wave -- see the porting-agent final report).
///
/// SHAPE: unlike the BMC report, the nine-way cost-center bucketing collapses to a SINGLE literal
/// "0000" bucket (legacy line 1624: <c>CostCenterList.Add("0000")</c>) whose totals ignore the real cost
/// center entirely; each of the up-to-six summary categories routes its (unnegated) amount into a Debit or
/// Credit column by sign instead of one signed Amount column, and the trailing CostCenter-wise Debit
/// section (legacy lines 1959-2016) restores the real per-line cost center.
///
/// PAYROLL AREA IS NOT A USER FILTER HERE -- same convention as the already-ported Loan Format (see
/// <see cref="PayrollJVCustom016LoanFormatMybReportAppService"/> remarks, including the "Contains" match).
///
/// DIVERGENCE (agreed for this wave): RLS is applied -- see <see cref="PayrollJVCustom016PostingLineLoader"/>
/// remarks.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_PayrollJVCustom016MainFormatMyb)]
public class PayrollJVCustom016MainFormatMybReportAppService
    : GridReportAppServiceBase, IPayrollJVCustom016MainFormatMybReportAppService
{
    /// <summary>Legacy line 1579: <c>emppayrollArea.Name.ToUpper().Contains("MYB")</c>.</summary>
    private const string MybPayrollAreaNameFragment = "MYB";

    /// <summary>Legacy line 1624: the single literal bucket every summary total is grouped into.</summary>
    private const string SummaryCostCenterBucket = "0000";

    private static readonly string[] PaidLeaveWageTypeCodes = { "1003", "7003", "1052", "7052", "1061", "7061" };
    private static readonly string[] LeaveProvisionWageTypeCodes = { "3001", "6001" };
    private static readonly string[] IndemnityPayAdjustWageTypeCodes = { "15", "16" };
    private static readonly string[] IndemnityProvisionWageTypeCodes = { "3000", "6000" };
    private static readonly string[] IndemnityPayWageTypeCodes = { "1010", "7010" };

    /// <summary>Legacy lines 1940-1957: wage types excluded from the "CostCenter Wise Debit" (non-Detail) section.</summary>
    private static readonly string[] NotInDebitWageTypeCodes =
        { "1003", "7003", "1052", "7052", "1061", "7061", "1010", "7010", "15", "16", "9000" };

    private readonly PayrollJVCustom016PostingLineLoader _loader;
    private readonly IRepository<PayrollArea> _payrollAreaRepository;
    private readonly IRepository<WageType> _wageTypeRepository;

    public PayrollJVCustom016MainFormatMybReportAppService(
        PayrollJVCustom016PostingLineLoader loader,
        IRepository<PayrollArea> payrollAreaRepository,
        IRepository<WageType> wageTypeRepository)
    {
        _loader = loader;
        _payrollAreaRepository = payrollAreaRepository;
        _wageTypeRepository = wageTypeRepository;
    }

    protected override string LegacyScreenName => "uscRptPayrollJV_Custom016";

    protected override string ScreenCaption => "Payroll JV (Custom 016) - Main Format - MYB";

    [HttpPost]
    public async Task<PagedResultDto<PayrollJVCustom016MainFormatMybRowDto>>
        GetPayrollJVCustom016MainFormatMybReport(GetPayrollJVCustom016MainFormatReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);

        var paged = rows.AsQueryable().OrderBy(input.Sorting).PageBy(input).ToList();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildJvParameterString(input), rows.Count);

        return new PagedResultDto<PayrollJVCustom016MainFormatMybRowDto>(rows.Count, paged);
    }

    [HttpPost]
    public async Task<FileDto> GetPayrollJVCustom016MainFormatMybReportToExcel(
        GetPayrollJVCustom016MainFormatReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the whole filtered data set.
        var ordered = rows.AsQueryable().OrderBy(input.Sorting).ToList();

        return await ExportToFileAsync(
            "PayrollJVCustom016MainFormatMyb.xlsx", ordered, ExportColumns, BuildJvParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND row construction, shared by the paged read and the
    /// export. Ports legacy lines 1578-2016.
    /// </summary>
    private async Task<List<PayrollJVCustom016MainFormatMybRowDto>> BuildRowsAsync(
        GetPayrollJVCustom016MainFormatReportInput input)
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
            return new List<PayrollJVCustom016MainFormatMybRowDto>();
        }

        var ds = await _loader.LoadAsync(endDate, mybAreaId.Value, input.PayrollRunType, employeeCodes, rlsEmployeeIds);

        var fixedName = await ResolveFixedWageTypeNameAsync();
        var suffixSlplacReference = endDate.ToString("MMM/yyyy").ToUpperInvariant();
        var reference = suffixSlplacReference + " SAL";

        var rows = new List<PayrollJVCustom016MainFormatMybRowDto>();
        var rowNumber = 0;

        void AddSummaryRow(string exp, double signedAmount, string narrative)
        {
            rowNumber++;
            rows.Add(new PayrollJVCustom016MainFormatMybRowDto
            {
                Id = rowNumber,
                LineNumber = rowNumber,
                CostCenter = SummaryCostCenterBucket,
                Exp = exp,
                Date = endDate,
                DocDate = endDate,
                Suffix = suffixSlplacReference,
                SlPlAccount = suffixSlplacReference,
                Reference = reference,
                Narrative = narrative,
                Debit = signedAmount > 0 ? signedAmount : 0,
                Credit = signedAmount < 0 ? -1 * signedAmount : 0
            });
        }

        // Legacy lines 1626-1636: EVERY line's cost center is forced to the literal "0000" bucket.
        var buckets = ds
            .GroupBy(l => (l.WageTypeId, l.AccountInfoId, l.OffsetAccountInfoId))
            .Select(g => new { Sample = g.First(), CostAmount = g.Sum(l => l.CostAmount) })
            .Where(g => g.CostAmount != 0)
            .ToList();

        // Legacy lines 1651-1692: Debit-side - Annual Leave Pay.
        var paidLeave = buckets.Where(b => PaidLeaveWageTypeCodes.Contains(b.Sample.WageTypeCode)).ToList();
        if (paidLeave.Count > 0)
        {
            var sample = paidLeave[0].Sample;
            AddSummaryRow(
                (sample.OffsetAccountInfoCode ?? string.Empty).PadLeft(6, '0'),
                paidLeave.Sum(b => b.CostAmount),
                "SAL " + fixedName("1003"));
        }

        // Legacy lines 1694-1728: Credit-side - Annual Leave Provision (sign NOT pre-negated).
        var leaveProvision = buckets.Where(b => LeaveProvisionWageTypeCodes.Contains(b.Sample.WageTypeCode)).ToList();
        if (leaveProvision.Count > 0)
        {
            var sample = leaveProvision[0].Sample;
            var amount = leaveProvision.Sum(b => b.CostAmount);
            rowNumber++;
            rows.Add(new PayrollJVCustom016MainFormatMybRowDto
            {
                Id = rowNumber,
                LineNumber = rowNumber,
                CostCenter = SummaryCostCenterBucket,
                Exp = (sample.OffsetAccountInfoCode ?? string.Empty).PadLeft(6, '0'),
                Date = endDate,
                DocDate = endDate,
                Suffix = suffixSlplacReference,
                SlPlAccount = suffixSlplacReference,
                Reference = reference,
                Narrative = "SAL " + (sample.OffsetAccountInfoName ?? string.Empty),
                Debit = amount < 0 ? -1 * amount : 0,
                Credit = amount > 0 ? amount : 0
            });
        }

        // Legacy lines 1730-1773: Debit-side - Indemnity Pay Adjust.
        var indemnityPayAdjust = buckets.Where(b => IndemnityPayAdjustWageTypeCodes.Contains(b.Sample.WageTypeCode)).ToList();
        if (indemnityPayAdjust.Count > 0)
        {
            var sample = indemnityPayAdjust[0].Sample;
            var amount = indemnityPayAdjust.Sum(b => b.CostAmount);
            AddSummaryRow(
                amount < 0
                    ? (sample.AccountInfoCode ?? string.Empty).PadLeft(6, '0')
                    : (sample.OffsetAccountInfoCode ?? string.Empty).PadLeft(6, '0'),
                amount,
                "SAL " + fixedName("15"));
        }

        // Legacy lines 1814-1848: Credit-side - Indemnity Provision (sign NOT pre-negated).
        var indemnityProvision = buckets.Where(b => IndemnityProvisionWageTypeCodes.Contains(b.Sample.WageTypeCode)).ToList();
        if (indemnityProvision.Count > 0)
        {
            var sample = indemnityProvision[0].Sample;
            var amount = indemnityProvision.Sum(b => b.CostAmount);
            rowNumber++;
            rows.Add(new PayrollJVCustom016MainFormatMybRowDto
            {
                Id = rowNumber,
                LineNumber = rowNumber,
                CostCenter = SummaryCostCenterBucket,
                Exp = (sample.OffsetAccountInfoCode ?? string.Empty).PadLeft(6, '0'),
                Date = endDate,
                DocDate = endDate,
                Suffix = suffixSlplacReference,
                SlPlAccount = suffixSlplacReference,
                Reference = reference,
                Narrative = "SAL " + (sample.OffsetAccountInfoName ?? string.Empty),
                Debit = amount < 0 ? -1 * amount : 0,
                Credit = amount > 0 ? amount : 0
            });
        }

        // Legacy lines 1850-1887: Debit-side - Indemnity Pay.
        var indemnityPay = buckets.Where(b => IndemnityPayWageTypeCodes.Contains(b.Sample.WageTypeCode)).ToList();
        if (indemnityPay.Count > 0)
        {
            var sample = indemnityPay[0].Sample;
            AddSummaryRow(
                (sample.OffsetAccountInfoCode ?? string.Empty).PadLeft(6, '0'),
                indemnityPay.Sum(b => b.CostAmount),
                "SAL " + fixedName("1010"));
        }

        // Legacy lines 1889-1928: Credit-side - Net Pay (sign NOT pre-negated).
        var netPaySample = buckets.FirstOrDefault(b => b.Sample.WageTypeCode == "9000");
        if (netPaySample != null)
        {
            var netPay = buckets
                .Where(b => b.Sample.WageTypeCode != "9000"
                            && (b.Sample.BillingType == BillingType.Earning || b.Sample.BillingType == BillingType.Deduction))
                .Sum(b => b.Sample.BillingType == BillingType.Earning ? b.CostAmount : -1 * b.CostAmount);

            rowNumber++;
            rows.Add(new PayrollJVCustom016MainFormatMybRowDto
            {
                Id = rowNumber,
                LineNumber = rowNumber,
                CostCenter = SummaryCostCenterBucket,
                Exp = (netPaySample.Sample.OffsetAccountInfoCode ?? string.Empty).PadLeft(6, '0'),
                Date = endDate,
                DocDate = endDate,
                Suffix = suffixSlplacReference,
                SlPlAccount = suffixSlplacReference,
                Reference = reference,
                Narrative = "SAL " + (netPaySample.Sample.AccountInfoName ?? string.Empty),
                Debit = netPay < 0 ? -1 * netPay : 0,
                Credit = netPay > 0 ? netPay : 0
            });
        }

        // Legacy lines 1959-2016 (IsDetail == false): grouped CostCenter-wise Debit, real cost center.
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
            rows.Add(new PayrollJVCustom016MainFormatMybRowDto
            {
                Id = rowNumber,
                LineNumber = rowNumber,
                CostCenter = (group.Sample.CostCenterCode ?? string.Empty).PadLeft(4, '0'),
                Exp = (group.Sample.AccountInfoCode ?? string.Empty).PadLeft(6, '0'),
                Date = endDate,
                DocDate = endDate,
                Suffix = suffixSlplacReference,
                SlPlAccount = suffixSlplacReference,
                Reference = reference,
                Narrative = "SAL " + (group.Sample.AccountInfoName ?? string.Empty),
                Debit = group.CostAmount > 0 ? group.CostAmount : 0,
                Credit = group.CostAmount < 0 ? -1 * group.CostAmount : 0
            });
        }

        return rows;
    }

    /// <summary>Legacy lines 1660, 1735, 1855: narratives use a FIXED wage-type code's name -- see
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
        var parts = new List<string> { "Payroll Area : " + MybPayrollAreaNameFragment };

        if (input.PayrollRunType.HasValue)
        {
            parts.Add("Run Type : " + input.PayrollRunType.Value);
        }

        parts.Add("Month : " + (input.Month ?? Today.Month));
        parts.Add("Year : " + (input.Year ?? Today.Year));
        parts.Add("Type : Main Format-New");

        if (!string.IsNullOrWhiteSpace(input.EmployeeCode))
        {
            parts.Add("Employee Code : " + input.EmployeeCode);
        }

        return string.Join(" | ", parts);
    }

    private static readonly List<GridReportColumn<PayrollJVCustom016MainFormatMybRowDto>> ExportColumns = new()
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
