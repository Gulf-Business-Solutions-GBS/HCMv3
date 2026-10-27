using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.Dto;
using HCMv3.GridReports.Exporting;
using HCMv3.GridReports.PayrollReport.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// Payroll JV (Custom 017) analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPayrollJV_Custom017.ascx(.cs)</c>,
/// method <c>Table1()</c> (lines 558-1011).
///
/// SHAPE: this variant does not summarise anything. It emits a MICROSOFT DYNAMICS AX JOURNAL IMPORT
/// FILE -- one row per salary-register line (plus an offset row per provision, plus one closing Net Pay
/// row), with AX's own 27 column names and AX's own date format. That is why the row DTO keeps the AX
/// field names verbatim.
///
/// The 27 columns come straight from legacy lines 594-620. Cost elements map onto AX dimensions:
/// CostElement1 -> Company accounts, 2 -> LedgerDimension.Location, 3 -> LedgerDimension.Brand,
/// 4 -> LedgerDimension.Department, 5 -> LedgerDimension.Employees, 6 -> DefaultDimension.Employees,
/// 7 -> DefaultDimension.Location (legacy lines 799/812-817).
///
/// DIFFERENCES FROM ITS SIBLINGS 019/023 worth calling out:
/// <list type="bullet">
/// <item><description>The wage-type filter is EVERY billing type except Net Pay (legacy line 645), not
/// Earning/Deduction -- see <see cref="PayrollJVWageTypeScope.AllExceptNetPay"/>.</description></item>
/// <item><description>It DOES apply the cost percentage (legacy line 795:
/// <c>Amount = CostPercentage * sr.Amount / 100</c>). 019 and 023 do not, which is what identifies
/// their behaviour as a defect rather than a design choice.</description></item>
/// <item><description>It hard-requires an <c>AXBatch</c> row and throws when there is none (legacy
/// lines 624-626).</description></item>
/// </list>
///
/// DIVERGENCE (agreed for this wave): RLS is applied -- see <see cref="PayrollJVPostingLineLoader"/>.
///
/// ACCOUNTING DEFECT FLAGGED, NOT FIXED -- SILENT LINE LOSS. Because the amount is
/// <c>CostPercentage * Amount / 100</c> and the join is a LEFT join, a salary line whose employee/wage
/// type has NO <c>EmployeeWageAccount</c> row (or whose mapping has <c>CostPercentage = 0</c>) comes out
/// as zero and is then dropped entirely by the <c>Debit != 0 || Credit != 0</c> guard (legacy line 842).
/// Unmapped pay therefore VANISHES from the journal instead of surfacing as an exception, and the file
/// still looks complete. Reproduced verbatim and pinned by
/// <c>Should_Reproduce_The_Legacy_Defect_Where_Unmapped_Wage_Types_Vanish_From_The_Journal</c>. This
/// needs a customer decision -- the sibling 019/023 reports at least have a "Missing Accounts" format
/// that lists them, and Custom 017's own Type combo is inert (see below).
///
/// LEGACY DEFECT REPRODUCED: the Type combo is INERT. <c>BindType()</c> (line 241) offers four formats
/// but the live <c>Table1()</c> never reads <c>cmbType</c>; only the execution-log parameter string
/// (line 547) and the export FILE NAME (line 1605) use it. Kept verbatim and pinned by
/// <c>Should_Ignore_The_Format_Combo_Because_Legacy_Table1_Never_Reads_It</c>.
///
/// NOT PORTED -- DevExpress/WebForms plumbing with no data meaning: the drill-down/drill-up session
/// stack, the ColumnChooser JSON persistence (lines 1621-1661), the callback-panel state machine
/// (lines 1459-1582), the PDF export button (line 1587) and the dead <c>Table1_Old()</c> (lines
/// 1012-1458).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_PayrollJVCustom017)]
public class PayrollJVCustom017ReportAppService
    : GridReportAppServiceBase, IPayrollJVCustom017ReportAppService
{
    /// <summary>
    /// Legacy lines 634-638: Annual Leave Provision Increment Pay and Indemnity Provision Increment
    /// Pay. These are reported with the literal BillingType "Provision" and get an offset row.
    /// </summary>
    private static readonly string[] ProvisionIncrementWageTypeCodes = { "1066", "1067" };

    /// <summary>Legacy line 53: every amount is rounded to three decimals.</summary>
    private const int DecimalPlaces = 3;

    /// <summary>Legacy line 54: AX expects the posting date as "MM/dd/yyyy" TEXT.</summary>
    private const string AxDateFormat = "MM/dd/yyyy";

    /// <summary>
    /// Legacy lines 855-859: for a Customer-type account AX wants the employee dimension in
    /// DefaultDimension and the ledger dimensions blanked.
    /// </summary>
    private const string CustomerAccountTypeName = "Customer";

    private readonly PayrollJVPostingLineLoader _postingLineLoader;
    private readonly IRepository<AXBatch> _axBatchRepository;
    private readonly IRepository<PayrollArea> _payrollAreaRepository;
    private readonly IRepository<EmployeeBasicPayDetail> _basicPayDetailRepository;

    public PayrollJVCustom017ReportAppService(
        PayrollJVPostingLineLoader postingLineLoader,
        IRepository<AXBatch> axBatchRepository,
        IRepository<PayrollArea> payrollAreaRepository,
        IRepository<EmployeeBasicPayDetail> basicPayDetailRepository)
    {
        _postingLineLoader = postingLineLoader;
        _axBatchRepository = axBatchRepository;
        _payrollAreaRepository = payrollAreaRepository;
        _basicPayDetailRepository = basicPayDetailRepository;
    }

    protected override string LegacyScreenName => "uscRptPayrollJV_Custom017";

    protected override string ScreenCaption => "Payroll JV (Custom 017)";

    [HttpPost]
    public async Task<PayrollJVCustom017ReportOutput> GetPayrollJVCustom017Report(
        GetPayrollJVCustom017ReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);

        var paged = rows.AsQueryable()
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToList();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildJvParameterString(input), rows.Count);

        return new PayrollJVCustom017ReportOutput
        {
            TotalCount = rows.Count,
            Items = paged,
            Columns = GetColumnDtos()
        };
    }

    [HttpPost]
    public async Task<FileDto> GetPayrollJVCustom017ReportToExcel(GetPayrollJVCustom017ReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the whole filtered data set.
        var ordered = rows.AsQueryable().OrderBy(input.Sorting).ToList();

        return await ExportToFileAsync(
            "PayrollJVCustom017.xlsx",
            ordered,
            GetExportColumns(),
            BuildJvParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND row construction, shared by the paged read and the
    /// export. Ports legacy <c>Table1()</c> (lines 558-1011).
    /// </summary>
    private async Task<List<PayrollJVCustom017ReportRowDto>> BuildRowsAsync(
        GetPayrollJVCustom017ReportInput input)
    {
        var endDate = ResolveEndDate(input);
        var employeeCodes = input.ResolveEmployeeCodes();

        // Legacy lines 624-626: no AXBatch row is a hard error, not an empty report.
        var axBatch = await _axBatchRepository.GetAll()
            .Select(b => new { b.JournalBatchNumber })
            .FirstOrDefaultAsync();

        if (axBatch == null)
        {
            throw new UserFriendlyException(L("AXBatchHasNoRecord"));
        }

        var lines = await _postingLineLoader.LoadAsync(
            endDate,
            // Not used by this scope, but the signature requires it.
            Array.Empty<string>(),
            input.PayrollRunType,
            input.PayrollAreaId.HasValue ? new[] { input.PayrollAreaId.Value } : Array.Empty<int>(),
            employeeCodes,
            await GetRlsEmployeeIdsAsync(),
            PayrollJVWageTypeScope.AllExceptNetPay);

        // Legacy lines 729-745: the currency of ANY basic-pay detail of the employee current at the
        // month end. Legacy re-evaluated this per row against a lazily-enumerated query; a dictionary
        // built once is the same answer.
        var currencyByEmployeeId = await BuildCurrencyLookupAsync(endDate);

        var axLines = lines
            .Select(l => new AxLine
            {
                EmployeeId = l.EmployeeId,
                WageTypeId = l.WageTypeId,
                WageTypeCode = l.WageTypeCode,
                CompanyAccounts = l.CostElement1Code,
                Currency = currencyByEmployeeId.TryGetValue(l.EmployeeId, out var currency) ? currency : null,
                JournalBatchNumber = axBatch.JournalBatchNumber,
                LineNumberDate = endDate.ToString(AxDateFormat, CultureInfo.InvariantCulture),
                LineNumberAccountType = l.AccountTypeName,
                LedgerDimensionMainAccount = l.AccountInfoCode,
                LedgerDimensionMainAccountName = l.AccountInfoName,
                DefaultDimensionLocation = l.CostElement7Code,
                DefaultDimensionEmployees = l.CostElement6Code,
                LedgerDimensionLocation = l.CostElement2Code,
                LedgerDimensionBrand = l.CostElement3Code,
                LedgerDimensionDepartment = l.CostElement4Code,
                LedgerDimensionEmployees = l.CostElement5Code,
                // Legacy line 818-819. Note Provision counts as a DEBIT alongside Earning.
                Debit = Math.Round(
                    l.BillingType == BillingType.Earning || l.BillingType == BillingType.Provision
                        ? ApplyCostPercentage(l)
                        : 0,
                    DecimalPlaces),
                Credit = Math.Round(
                    l.BillingType == BillingType.Deduction ? ApplyCostPercentage(l) : 0,
                    DecimalPlaces),
                Description = l.WageTypeName,
                OffsetAccountType = l.OffsetAccountTypeName ?? string.Empty,
                OffsetLedgerDimensionMainAccount = l.OffsetAccountInfoCode ?? string.Empty,
                RawBillingType = l.BillingType,
                BillingTypeText = ProvisionIncrementWageTypeCodes.Contains(l.WageTypeCode)
                    ? BillingType.Provision.ToString()
                    : l.BillingType.ToString(),
                MergeOffsetAccount = l.MergeOffsetAccount
            })
            .ToList();

        return BuildAxRows(axLines, endDate, axBatch.JournalBatchNumber,
            await LoadPayrollAreaClosingInfoAsync(input.PayrollAreaId));
    }

    /// <summary>
    /// Legacy line 795: <c>Amount = (CostPercentage * sr.Amount) / 100</c>. Unlike its 019/023 siblings
    /// this variant DOES honour the split percentage -- and, as a side effect, silently zeroes any line
    /// with no wage-account mapping (see the class remarks).
    /// </summary>
    private static double ApplyCostPercentage(PayrollJVPostingLine line)
    {
        return line.CostPercentage * line.Amount / 100;
    }

    /// <summary>Legacy fill loop, lines 840-1000.</summary>
    private static List<PayrollJVCustom017ReportRowDto> BuildAxRows(
        List<AxLine> axLines,
        DateTime endDate,
        string journalBatchNumber,
        PayrollAreaClosingInfo closingInfo)
    {
        var rows = new List<PayrollJVCustom017ReportRowDto>();
        var count = 1;

        foreach (var line in axLines)
        {
            // Legacy line 842: zero-value lines never reach the file.
            if (line.Debit == 0 && line.Credit == 0)
            {
                continue;
            }

            var isCustomerAccount = line.LineNumberAccountType == CustomerAccountTypeName;

            // Legacy lines 844-877 -- the main-account row.
            rows.Add(new PayrollJVCustom017ReportRowDto
            {
                Id = rows.Count + 1,
                LineNumber = rows.Count + 1,
                Valid = string.Empty,
                CompanyAccounts = line.CompanyAccounts,
                JournalBatchNumber = line.JournalBatchNumber,
                RecId = count,
                AxLineNumber = count,
                LineNumberDate = line.LineNumberDate,
                LineNumberAccountType = line.LineNumberAccountType,
                LedgerDimensionMainAccount = line.LedgerDimensionMainAccount,
                LedgerDimensionMainAccountName = line.LedgerDimensionMainAccountName,
                DefaultDimensionLocation = line.DefaultDimensionLocation,
                // Legacy lines 855-859: a Customer account moves the employee dimension across and
                // blanks the ledger dimensions.
                DefaultDimensionEmployees = isCustomerAccount
                    ? line.LedgerDimensionEmployees
                    : line.DefaultDimensionEmployees,
                LedgerDimensionLocation = isCustomerAccount ? string.Empty : line.LedgerDimensionLocation,
                LedgerDimensionBrand = line.LedgerDimensionBrand,
                LedgerDimensionDepartment = isCustomerAccount ? string.Empty : line.LedgerDimensionDepartment,
                LedgerDimensionEmployees = isCustomerAccount ? string.Empty : line.LedgerDimensionEmployees,
                Currency = line.Currency,
                ExchangeRate = string.Empty,
                Debit = line.Debit,
                Credit = line.Credit,
                Description = line.Description,
                Voucher = string.Empty,
                // Legacy never writes the three Offset columns on a main-account row: the offset account
                // gets its own row below instead. Left blank on purpose.
                OffsetAccountType = string.Empty,
                OffsetLedgerDimensionMainAccount = string.Empty,
                OffsetCompanyAccounts = string.Empty,
                BillingType = line.BillingTypeText,
                IsMerged = false
            });

            count++;

            // Legacy lines 880-970 -- the offset row, provisions only.
            var isProvision = ProvisionIncrementWageTypeCodes.Contains(line.WageTypeCode)
                              || line.RawBillingType == BillingType.Provision;

            if (!isProvision || line.OffsetLedgerDimensionMainAccount == string.Empty)
            {
                continue;
            }

            // Legacy lines 884-886: is there ANY line of the same employee/wage type flagged
            // MergeOffsetAccount? If so, all such offsets collapse onto one row.
            var mergeLine = axLines.FirstOrDefault(
                l => l.EmployeeId == line.EmployeeId
                     && l.WageTypeId == line.WageTypeId
                     && l.MergeOffsetAccount);

            if (mergeLine == null)
            {
                // Legacy lines 890-919 -- a plain offset row with Debit and Credit swapped.
                rows.Add(new PayrollJVCustom017ReportRowDto
                {
                    Id = rows.Count + 1,
                    LineNumber = rows.Count + 1,
                    Valid = string.Empty,
                    CompanyAccounts = line.CompanyAccounts,
                    JournalBatchNumber = line.JournalBatchNumber,
                    RecId = count,
                    AxLineNumber = count,
                    LineNumberDate = line.LineNumberDate,
                    LineNumberAccountType = line.OffsetAccountType,
                    LedgerDimensionMainAccount = line.OffsetLedgerDimensionMainAccount,
                    // Legacy does NOT copy MainAccountName onto the offset row (no line 899 equivalent).
                    LedgerDimensionMainAccountName = null,
                    DefaultDimensionLocation = line.DefaultDimensionLocation,
                    DefaultDimensionEmployees = line.DefaultDimensionEmployees,
                    LedgerDimensionLocation = line.LedgerDimensionLocation,
                    LedgerDimensionBrand = line.LedgerDimensionBrand,
                    LedgerDimensionDepartment = line.LedgerDimensionDepartment,
                    LedgerDimensionEmployees = line.LedgerDimensionEmployees,
                    Currency = line.Currency,
                    ExchangeRate = string.Empty,
                    Debit = line.Credit,
                    Credit = line.Debit,
                    Description = line.Description,
                    Voucher = string.Empty,
                    BillingType = line.BillingTypeText,
                    IsMerged = false
                });

                count++;
                continue;
            }

            // Legacy lines 922-969 -- merge onto the existing merged row for this
            // (LedgerDimension.Employees, offset account) pair, or start one.
            var mergedRow = rows.FirstOrDefault(
                r => r.IsMerged
                     && r.LedgerDimensionEmployees == mergeLine.LedgerDimensionEmployees
                     && r.LedgerDimensionMainAccount == mergeLine.OffsetLedgerDimensionMainAccount);

            if (mergedRow == null)
            {
                mergedRow = new PayrollJVCustom017ReportRowDto
                {
                    Id = rows.Count + 1,
                    LineNumber = rows.Count + 1,
                    RecId = count,
                    AxLineNumber = count
                };

                rows.Add(mergedRow);
                count++;
            }

            mergedRow.Valid = string.Empty;
            mergedRow.CompanyAccounts = mergeLine.CompanyAccounts;
            mergedRow.JournalBatchNumber = mergeLine.JournalBatchNumber;
            mergedRow.LineNumberDate = mergeLine.LineNumberDate;
            mergedRow.LineNumberAccountType = mergeLine.OffsetAccountType;
            mergedRow.LedgerDimensionMainAccount = mergeLine.OffsetLedgerDimensionMainAccount;
            mergedRow.DefaultDimensionLocation = mergeLine.DefaultDimensionLocation;
            mergedRow.DefaultDimensionEmployees = mergeLine.DefaultDimensionEmployees;
            mergedRow.LedgerDimensionLocation = mergeLine.LedgerDimensionLocation;
            mergedRow.LedgerDimensionBrand = mergeLine.LedgerDimensionBrand;
            mergedRow.LedgerDimensionDepartment = mergeLine.LedgerDimensionDepartment;
            mergedRow.LedgerDimensionEmployees = mergeLine.LedgerDimensionEmployees;
            // Legacy line 958 takes the currency from the CURRENT line, not the merge line.
            mergedRow.Currency = line.Currency;
            mergedRow.ExchangeRate = string.Empty;
            mergedRow.Debit += line.Credit;
            mergedRow.Credit += line.Debit;
            mergedRow.Description = mergeLine.Description;
            mergedRow.Voucher = string.Empty;
            mergedRow.BillingType = mergeLine.BillingTypeText;
            mergedRow.IsMerged = true;
        }

        // Legacy lines 973-1000 -- the closing Net Pay credit against the payroll area's FI bank.
        if (rows.Count == 0)
        {
            return rows;
        }

        var totalNetAmount = axLines
            .Where(l => !ProvisionIncrementWageTypeCodes.Contains(l.WageTypeCode)
                        && (l.RawBillingType == BillingType.Earning
                            || l.RawBillingType == BillingType.Deduction))
            .Sum(l => l.Debit - l.Credit);

        if (totalNetAmount == 0)
        {
            return rows;
        }

        rows.Add(new PayrollJVCustom017ReportRowDto
        {
            Id = rows.Count + 1,
            LineNumber = rows.Count + 1,
            Valid = string.Empty,
            // Null when no payroll area was picked -- legacy behaved identically.
            CompanyAccounts = closingInfo?.FiCompanyCode,
            JournalBatchNumber = journalBatchNumber,
            RecId = count,
            AxLineNumber = count,
            LineNumberDate = endDate.ToString(AxDateFormat, CultureInfo.InvariantCulture),
            LineNumberAccountType = closingInfo?.FiBankAccountTypeName,
            LedgerDimensionMainAccount = closingInfo?.FiBankCode,
            Currency = closingInfo?.CurrencyShortForm ?? string.Empty,
            ExchangeRate = string.Empty,
            Debit = 0,
            Credit = Math.Round(totalNetAmount, DecimalPlaces),
            Description = "Net Pay",
            Voucher = string.Empty,
            OffsetAccountType = string.Empty,
            OffsetLedgerDimensionMainAccount = string.Empty,
            OffsetCompanyAccounts = string.Empty,
            BillingType = BillingType.Earning.ToString(),
            IsMerged = false
        });

        return rows;
    }

    /// <summary>Legacy lines 729-745: the currency of any basic-pay detail current at the month end.</summary>
    private async Task<Dictionary<int, string>> BuildCurrencyLookupAsync(DateTime endDate)
    {
        var details = await _basicPayDetailRepository.GetAll()
            .Where(d => d.EmployeeBasicPay.StartDate <= endDate && d.EmployeeBasicPay.EndDate >= endDate)
            .Select(d => new
            {
                d.EmployeeBasicPay.EmployeeId,
                CurrencyShortForm = d.Currency == null ? null : d.Currency.CurrencyShortFormEn
            })
            .ToListAsync();

        return details
            .GroupBy(d => d.EmployeeId)
            .ToDictionary(g => g.Key, g => g.Select(d => d.CurrencyShortForm).FirstOrDefault());
    }

    /// <summary>Legacy lines 985-991: the closing Net Pay line's company, bank and currency.</summary>
    private async Task<PayrollAreaClosingInfo> LoadPayrollAreaClosingInfoAsync(int? payrollAreaId)
    {
        if (!payrollAreaId.HasValue)
        {
            return null;
        }

        return await _payrollAreaRepository.GetAll()
            .Where(a => a.Id == payrollAreaId.Value)
            .Select(a => new PayrollAreaClosingInfo
            {
                FiCompanyCode = a.FICompany == null ? null : a.FICompany.Code,
                FiBankCode = a.FIBank == null ? null : a.FIBank.Code,
                FiBankAccountTypeName = a.FIBank == null || a.FIBank.AccountType == null
                    ? null
                    : a.FIBank.AccountType.NameEn,
                CurrencyShortForm = a.Currency == null ? null : a.Currency.CurrencyShortFormEn
            })
            .FirstOrDefaultAsync();
    }

    /// <summary>Legacy <c>GetTable()</c>: blank month/year mean "now"; an invalid pair fell back to 1900.</summary>
    private DateTime ResolveEndDate(GetPayrollJVCustom017ReportInput input)
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

    /// <summary>Legacy <c>GetParameterStr()</c> (lines 530-552).</summary>
    private string BuildJvParameterString(GetPayrollJVCustom017ReportInput input)
    {
        var parts = new List<string>();

        if (input.PayrollAreaId.HasValue)
        {
            parts.Add("Payroll Area : " + input.PayrollAreaId.Value);
        }

        if (input.PayrollRunType.HasValue)
        {
            parts.Add("Run Type : " + input.PayrollRunType.Value);
        }

        parts.Add("Month : " + (input.Month ?? Today.Month));
        parts.Add("Year : " + (input.Year ?? Today.Year));
        // Logged even though it changes nothing -- see the class remarks.
        parts.Add("Type : " + input.Format);

        if (!string.IsNullOrWhiteSpace(input.EmployeeCode))
        {
            parts.Add("Employee Code : " + input.EmployeeCode);
        }

        return string.Join(" | ", parts);
    }

    /// <summary>
    /// The AX column list (legacy lines 594-620) in legacy order. The legacy "Oid" column is omitted:
    /// legacy declared it but never filled it, so it was a permanently blank column.
    /// </summary>
    private static List<PayrollJVReportColumnDto> GetColumnDtos()
    {
        return new List<PayrollJVReportColumnDto>
        {
            new("valid", "Valid"),
            new("companyAccounts", "CompanyAccounts"),
            new("journalBatchNumber", "JournalBatchNumber"),
            new("recId", "RecId"),
            new("axLineNumber", "LineNumber"),
            new("lineNumberDate", "LineNumberDate"),
            new("lineNumberAccountType", "LineNumberAccountType"),
            new("ledgerDimensionMainAccount", "LedgerDimensionMainAccount"),
            new("ledgerDimensionMainAccountName", "LedgerDimensionMainAccountName"),
            new("defaultDimensionLocation", "DefaultDimensionLocation"),
            new("defaultDimensionEmployees", "DefaultDimensionEmployees"),
            new("ledgerDimensionLocation", "LedgerDimensionLocation"),
            new("ledgerDimensionBrand", "LedgerDimensionBrand"),
            new("ledgerDimensionDepartment", "LedgerDimensionDepartment"),
            new("ledgerDimensionEmployees", "LedgerDimensionEmployees"),
            new("currency", "Currency"),
            new("exchangeRate", "ExchangeRate"),
            new("debit", "Debit"),
            new("credit", "Credit"),
            new("description", "Description"),
            new("voucher", "Voucher"),
            new("offsetAccountType", "OffsetAccountType"),
            new("offsetLedgerDimensionMainAccount", "OffsetLedgerDimensionMainAccount"),
            new("offsetCompanyAccounts", "OffsetCompanyAccounts"),
            new("billingType", "BillingType"),
            new("isMerged", "IsMerged")
        };
    }

    private static IReadOnlyList<GridReportColumn<PayrollJVCustom017ReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<PayrollJVCustom017ReportRowDto>>
        {
            new("Valid", r => r.Valid),
            new("CompanyAccounts", r => r.CompanyAccounts),
            new("JournalBatchNumber", r => r.JournalBatchNumber),
            new("RecId", r => r.RecId),
            new("LineNumber", r => r.AxLineNumber),
            new("LineNumberDate", r => r.LineNumberDate),
            new("LineNumberAccountType", r => r.LineNumberAccountType),
            new("LedgerDimensionMainAccount", r => r.LedgerDimensionMainAccount),
            new("LedgerDimensionMainAccountName", r => r.LedgerDimensionMainAccountName),
            new("DefaultDimensionLocation", r => r.DefaultDimensionLocation),
            new("DefaultDimensionEmployees", r => r.DefaultDimensionEmployees),
            new("LedgerDimensionLocation", r => r.LedgerDimensionLocation),
            new("LedgerDimensionBrand", r => r.LedgerDimensionBrand),
            new("LedgerDimensionDepartment", r => r.LedgerDimensionDepartment),
            new("LedgerDimensionEmployees", r => r.LedgerDimensionEmployees),
            new("Currency", r => r.Currency),
            new("ExchangeRate", r => r.ExchangeRate),
            new("Debit", r => r.Debit),
            new("Credit", r => r.Credit),
            new("Description", r => r.Description),
            new("Voucher", r => r.Voucher),
            new("OffsetAccountType", r => r.OffsetAccountType),
            new("OffsetLedgerDimensionMainAccount", r => r.OffsetLedgerDimensionMainAccount),
            new("OffsetCompanyAccounts", r => r.OffsetCompanyAccounts),
            new("BillingType", r => r.BillingType),
            new("IsMerged", r => r.IsMerged)
        };
    }

    /// <summary>Legacy's <c>newds</c> projection (lines 797-825), flattened.</summary>
    private sealed class AxLine
    {
        public int EmployeeId { get; set; }

        public int WageTypeId { get; set; }

        public string WageTypeCode { get; set; }

        public string CompanyAccounts { get; set; }

        public string Currency { get; set; }

        public string JournalBatchNumber { get; set; }

        public string LineNumberDate { get; set; }

        public string LineNumberAccountType { get; set; }

        public string LedgerDimensionMainAccount { get; set; }

        public string LedgerDimensionMainAccountName { get; set; }

        public string DefaultDimensionLocation { get; set; }

        public string DefaultDimensionEmployees { get; set; }

        public string LedgerDimensionLocation { get; set; }

        public string LedgerDimensionBrand { get; set; }

        public string LedgerDimensionDepartment { get; set; }

        public string LedgerDimensionEmployees { get; set; }

        public double Debit { get; set; }

        public double Credit { get; set; }

        public string Description { get; set; }

        public string OffsetAccountType { get; set; }

        public string OffsetLedgerDimensionMainAccount { get; set; }

        public BillingType RawBillingType { get; set; }

        public string BillingTypeText { get; set; }

        public bool MergeOffsetAccount { get; set; }
    }

    private sealed class PayrollAreaClosingInfo
    {
        public string FiCompanyCode { get; set; }

        public string FiBankCode { get; set; }

        public string FiBankAccountTypeName { get; set; }

        public string CurrencyShortForm { get; set; }
    }
}
