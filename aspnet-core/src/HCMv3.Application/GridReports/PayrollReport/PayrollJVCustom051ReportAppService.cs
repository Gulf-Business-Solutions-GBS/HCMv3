using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
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
/// Payroll JV (Custom 051) analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPayrollJV_Custom051.ascx(.cs)</c>,
/// method <c>Table1()</c> (lines 508-702). The smallest member of the ten-strong
/// <c>uscRptPayrollJV_Custom0NN</c> family: one journal-voucher line per (employee, wage type) with a
/// non-zero amount, laid out in a flat ERP import sheet of 28 columns.
///
/// LITERAL COLUMN CAPTIONS: every header is an ERP import token ("Amount|Debit", "#headerconnector",
/// "Subsidiary: Entity Type"), matched by exact text by the finance system that consumes the file. They
/// are exported via <c>GridReportColumn.FromLiteralHeader</c> -- NOT as localization keys, which would
/// render as [Amount|Debit] and break the import.
///
/// ENGLISH NAMES ONLY, DELIBERATELY: legacy reads <c>Employee.FullNameEn</c> and
/// <c>AccountInfo.NameEn</c> unconditionally (lines 596-598, 610) with no <c>CurrentAspect == "ar-KW"</c>
/// switch, unlike the rest of the Analysis family. <see cref="GridReportAppServiceBase.UseArabicNames"/>
/// is therefore deliberately not consulted here: these values are finance master-data keys, not UI labels.
///
/// === FLAGGED ACCOUNTING DEFECTS, REPRODUCED (each pinned by a test named to say so) ===
///
/// 1. THE PAYROLL AREA FILTER DOES NOTHING (lines 578-584 vs 590-593). <c>Table1()</c> builds a local
///    <c>query</c>, narrows it by the selected payroll area -- and then never uses it: the rows actually
///    rendered come from a SECOND, UNFILTERED query over <c>SalaryRegisterDetail</c> at line 590. Every
///    payroll area's employees therefore land in a voucher the user believes is scoped to one.
///
/// 2. THE COST-CENTRE COLUMNS ARE NEVER POPULATED. Lines 543-550 declare Unit / Unit: Name / Department /
///    Department: Name / Division / Division: Name / Project: Entity Type / Project: Value / Project: Name;
///    the fill loop (lines 605-676) assigns none of them. Every line posts with a blank cost centre even
///    though the employee's <c>EmployeeWageAccount</c> carries CostElement1..7.
///
/// 3. THE VOUCHER NEED NOT BALANCE. Lines are emitted one per wage type with no balancing entry and no
///    check, and a missing <c>EmployeeWageAccount</c> silently yields an EMPTY Account code (line 635)
///    rather than an error -- so an unpostable voucher looks exactly like a good one.
///
/// DELIBERATE DIVERGENCES FROM LEGACY:
/// * ROW LEVEL SECURITY IS APPLIED. Legacy declares <c>ApplyRowLevelSecurity</c> (lines 855-865) and never
///   calls it, so any user who could open the screen saw every employee's salary. This port applies it
///   (an empty list means "no restriction", the legacy convention) -- an agreed wave-wide divergence, and
///   JV sheets carry salary data.
/// * DUPLICATE WAGE-ACCOUNT KEYS DO NOT CRASH. Legacy's ToDictionary keyed by
///   "{EmployeeOid}_{WageTypeCode}" (lines 585-598) throws <c>ArgumentException</c> the moment an employee
///   has two effective-dated <c>EmployeeWageAccount</c> rows for one wage type -- which that entity is
///   explicitly designed to have. Here the newest-starting mapping wins (ties broken by Id).
///
/// NOT PORTED -- DevExpress/WebForms plumbing with no data meaning: the drill-down session stack (dead:
/// <c>GetTable()</c> declares only case 1), the ColumnChooser JSON persistence, the callback-panel state
/// machine and the PDF export button.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_PayrollJVCustom051)]
public class PayrollJVCustom051ReportAppService : GridReportAppServiceBase, IPayrollJVCustom051ReportAppService
{
    private const string NetPayWageTypeCode = "9000";
    private const string CurrencyCode = "KWD";
    private const string CurrencyName = "Kuwaiti Dinar";

    // Legacy column captions, in declaration order (lines 526-553). "OID" and "SortOrder" are technical
    // keys carried as row properties rather than in the caption dictionaries.
    private const string ColDetailLines = "//";
    private const string ColHeaderConnector = "#headerconnector";
    private const string ColAccount = "Account";
    private const string ColAccountName = "Account Name";
    private const string ColSubsidiaryEntityType = "Subsidiary: Entity Type";
    private const string ColSubsidiaryValue = "Subsidiary: Value";
    private const string ColSubsidiaryName = "Subsidiary: Name";
    private const string ColDebit = "Amount|Debit";
    private const string ColCredit = "Amount|Credit";
    private const string ColCurrency = "Currency";
    private const string ColCurrencyName = "Currency: Name";
    private const string ColRate = "Rate";
    private const string ColLocalDebit = "Local|Debit";
    private const string ColLocalCredit = "Local|Credit";
    private const string ColNarration = "Narration";
    private const string ColUnit = "Unit";
    private const string ColUnitName = "Unit: Name";
    private const string ColDepartment = "Department";
    private const string ColDepartmentName = "Department: Name";
    private const string ColDivision = "Division";
    private const string ColDivisionName = "Division: Name";
    private const string ColProjectEntityType = "Project: Entity Type";
    private const string ColProjectValue = "Project: Value";
    private const string ColProjectName = "Project: Name";
    private const string ColId = "Id";
    private const string ColErrors = "Errors";

    /// <summary>Legacy declaration order, lines 526-553.</summary>
    private static readonly (string Name, bool IsNumeric)[] ColumnDefinitions =
    {
        (ColDetailLines, false),
        (ColHeaderConnector, false),
        (ColAccount, false),
        (ColAccountName, false),
        (ColSubsidiaryEntityType, false),
        (ColSubsidiaryValue, false),
        (ColSubsidiaryName, false),
        (ColDebit, true),
        (ColCredit, true),
        (ColCurrency, false),
        (ColCurrencyName, false),
        (ColRate, true),
        (ColLocalDebit, true),
        (ColLocalCredit, true),
        (ColNarration, false),
        (ColUnit, false),
        (ColUnitName, false),
        (ColDepartment, false),
        (ColDepartmentName, false),
        (ColDivision, false),
        (ColDivisionName, false),
        (ColProjectEntityType, false),
        (ColProjectValue, false),
        (ColProjectName, false),
        (ColId, false),
        (ColErrors, false)
    };

    private readonly IRepository<SalaryRegisterDetail> _salaryRegisterDetailRepository;
    private readonly IRepository<EmployeeWageAccount> _employeeWageAccountRepository;

    public PayrollJVCustom051ReportAppService(
        IRepository<SalaryRegisterDetail> salaryRegisterDetailRepository,
        IRepository<EmployeeWageAccount> employeeWageAccountRepository)
    {
        _salaryRegisterDetailRepository = salaryRegisterDetailRepository;
        _employeeWageAccountRepository = employeeWageAccountRepository;
    }

    protected override string LegacyScreenName => "uscRptPayrollJV_Custom051";

    protected override string ScreenCaption => "Payroll JV";

    [HttpPost]
    public async Task<PayrollJVCustom051ReportOutput> GetPayrollJVCustom051Report(
        GetPayrollJVCustom051ReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var totalCount = query.Count();

        var rows = query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToList();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildJvParameterString(input), totalCount);

        return new PayrollJVCustom051ReportOutput
        {
            TotalCount = totalCount,
            Items = rows,
            Columns = ColumnDefinitions
                .Select(c => new PayrollJVCustom051ReportColumnDto { Name = c.Name, IsNumeric = c.IsNumeric })
                .ToList()
        };
    }

    [HttpPost]
    public async Task<FileDto> GetPayrollJVCustom051ReportToExcel(GetPayrollJVCustom051ReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the entire filtered data set,
        // and a partial journal voucher is worse than no journal voucher.
        var rows = query
            .OrderBy(input.Sorting)
            .ToList();

        return await ExportToFileAsync(
            "PayrollJVCustom051.xlsx",
            rows,
            GetExportColumns(),
            BuildJvParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export --
    /// this is what guarantees the export honours exactly the same filters as the grid.
    /// Ports legacy <c>Table1()</c>'s data regions (lines 576-676).
    ///
    /// Returns an IQueryable over an in-memory list because the legacy shape is not a straight projection:
    /// it groups per employee, orders wage types with net pay last and emits one row per surviving line,
    /// exactly as legacy did after its own ToList() at line 604.
    /// </summary>
    private async Task<IQueryable<PayrollJVCustom051ReportRowDto>> BuildQueryAsync(
        GetPayrollJVCustom051ReportInput input)
    {
        var periodEnd = ResolvePeriodEnd(input);
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();

        // Legacy lines 590-593. NOTE: no payroll-area predicate -- see defect 1 in the class remarks.
        var lines = await _salaryRegisterDetailRepository.GetAll()
            .Where(d => d.SalaryRegister.ForPeriod == periodEnd
                        && d.SalaryRegister.ForPeriod == d.SalaryRegister.InPeriod)
            // DELIBERATE DIVERGENCE: legacy never called its own ApplyRowLevelSecurity.
            .WhereIf(rlsEmployeeIds.Count > 0, d => rlsEmployeeIds.Contains(d.SalaryRegister.EmployeeId))
            .Select(d => new JvSourceLine
            {
                Amount = d.Amount,
                EmployeeId = d.SalaryRegister.EmployeeId,
                // Legacy line 597 uses ReferenceCode, NOT Code, as the subsidiary value.
                EmployeeCode = d.SalaryRegister.Employee.ReferenceCode,
                EmployeeName = d.SalaryRegister.Employee.FullNameEn,
                WageTypeCode = d.WageType.Code
            })
            .ToListAsync();

        var wageAccountMap = await BuildWageAccountMapAsync();

        var rows = new List<PayrollJVCustom051ReportRowDto>();
        var sortIndex = 0;

        // Legacy lines 606-609: group per employee, employees ordered by code.
        foreach (var employeeGroup in lines
                     .GroupBy(l => new { l.EmployeeId, l.EmployeeCode, l.EmployeeName })
                     .OrderBy(g => g.Key.EmployeeCode))
        {
            // Legacy lines 612-614: net pay last, then wage-type code ascending.
            var employeeLines = employeeGroup
                .OrderBy(l => l.WageTypeCode == NetPayWageTypeCode ? 1 : 0)
                .ThenBy(l => l.WageTypeCode);

            foreach (var line in employeeLines)
            {
                // Legacy line 619: zero-amount lines are dropped entirely.
                if (line.Amount == 0)
                {
                    continue;
                }

                var key = employeeGroup.Key.EmployeeId + "_" + line.WageTypeCode;
                wageAccountMap.TryGetValue(key, out var account);

                var row = new PayrollJVCustom051ReportRowDto
                {
                    // Legacy line 634 wrote the employee's Oid into the "OID" key column.
                    Id = employeeGroup.Key.EmployeeId,
                    SortOrder = sortIndex++
                };

                foreach (var (name, isNumeric) in ColumnDefinitions)
                {
                    if (isNumeric)
                    {
                        row.Amounts[name] = 0;
                    }
                    else
                    {
                        row.Values[name] = string.Empty;
                    }
                }

                // Legacy lines 635-636: blank, not null, when the (employee, wage type) has no mapping.
                row.Values[ColAccount] = account?.Code ?? string.Empty;
                row.Values[ColAccountName] = account?.NameEn ?? string.Empty;

                if (line.WageTypeCode == NetPayWageTypeCode)
                {
                    // Legacy lines 642-649: the net-pay line is always a CREDIT to the employee subsidiary.
                    row.Values[ColSubsidiaryEntityType] = "Employee";
                    row.Values[ColSubsidiaryValue] = employeeGroup.Key.EmployeeCode;
                    row.Values[ColSubsidiaryName] = employeeGroup.Key.EmployeeName;

                    var value = Math.Abs(line.Amount);
                    row.Amounts[ColCredit] = value;
                    row.Amounts[ColLocalCredit] = value;
                }
                else
                {
                    // Legacy lines 651-668: the sign decides the side; the subsidiary stays blank.
                    if (line.Amount > 0)
                    {
                        row.Amounts[ColDebit] = line.Amount;
                        row.Amounts[ColLocalDebit] = line.Amount;
                    }
                    else
                    {
                        row.Amounts[ColCredit] = Math.Abs(line.Amount);
                        row.Amounts[ColLocalCredit] = Math.Abs(line.Amount);
                    }
                }

                // Legacy lines 670-672.
                row.Values[ColCurrency] = CurrencyCode;
                row.Values[ColCurrencyName] = CurrencyName;
                row.Amounts[ColRate] = 1;

                // Legacy line 674. The month name is rendered invariantly so the voucher text does not
                // change with the caller's culture.
                row.Values[ColNarration] = employeeGroup.Key.EmployeeName + " " +
                                           periodEnd.ToString("MMMM yyyy", CultureInfo.InvariantCulture);

                rows.Add(row);
            }
        }

        return rows.AsQueryable();
    }

    /// <summary>
    /// Legacy lines 585-598. NOTE: Custom 051 -- alone in its family -- applies NO effective-date window to
    /// <c>EmployeeWageAccount</c>, so a mapping that ended years ago still supplies the account. Reproduced.
    /// See the class remarks for the duplicate-key divergence.
    /// </summary>
    private async Task<Dictionary<string, AccountRef>> BuildWageAccountMapAsync()
    {
        var mappings = await _employeeWageAccountRepository.GetAll()
            .Select(w => new
            {
                w.Id,
                w.EmployeeId,
                WageTypeCode = w.WageType.Code,
                w.StartDate,
                Code = w.AccountInfo.Code,
                NameEn = w.AccountInfo.NameEn
            })
            .ToListAsync();

        return mappings
            .GroupBy(m => m.EmployeeId + "_" + m.WageTypeCode)
            .ToDictionary(
                g => g.Key,
                g => g
                    .OrderByDescending(m => m.StartDate)
                    .ThenByDescending(m => m.Id)
                    .Select(m => new AccountRef { Code = m.Code, NameEn = m.NameEn })
                    .First());
    }

    /// <summary>
    /// Legacy <c>GetTable()</c> lines 429-448: Month/Year default to today's and resolve to the LAST day of
    /// that month; an unparseable pair fell back to 1900-01-31.
    /// </summary>
    private DateTime ResolvePeriodEnd(GetPayrollJVCustom051ReportInput input)
    {
        var month = input.Month ?? Today.Month;
        var year = input.Year ?? Today.Year;

        if (month < 1 || month > 12 || year < 1 || year > 9999)
        {
            return new DateTime(1900, 1, 31);
        }

        return new DateTime(year, month, DateTime.DaysInMonth(year, month));
    }

    /// <summary>Legacy <c>GetParameterStr</c> (lines 483-505): Payroll Area, Month, Year.</summary>
    private string BuildJvParameterString(GetPayrollJVCustom051ReportInput input)
    {
        var parts = new List<string>();

        if (input.PayrollAreaId.HasValue)
        {
            parts.Add("Payroll Area : " + input.PayrollAreaId.Value);
        }

        parts.Add("Month : " + (input.Month ?? Today.Month));
        parts.Add("Year : " + (input.Year ?? Today.Year));

        return string.Join(" | ", parts);
    }

    /// <summary>
    /// The exported column set, in legacy declaration order. Every header goes through
    /// <c>FromLiteralHeader</c> -- these are ERP import tokens, not localization keys, so none of them can
    /// ever render as an unresolved [Key].
    /// </summary>
    private static IReadOnlyList<GridReportColumn<PayrollJVCustom051ReportRowDto>> GetExportColumns()
    {
        var columns = new List<GridReportColumn<PayrollJVCustom051ReportRowDto>>
        {
            GridReportColumn<PayrollJVCustom051ReportRowDto>.FromLiteralHeader("OID", r => r.Id)
        };

        foreach (var (name, isNumeric) in ColumnDefinitions)
        {
            var columnName = name;

            columns.Add(isNumeric
                ? GridReportColumn<PayrollJVCustom051ReportRowDto>.FromLiteralHeader(
                    columnName,
                    r => r.Amounts.TryGetValue(columnName, out var amount) ? amount : 0)
                : GridReportColumn<PayrollJVCustom051ReportRowDto>.FromLiteralHeader(
                    columnName,
                    r => r.Values.TryGetValue(columnName, out var value) ? value : string.Empty));
        }

        columns.Add(GridReportColumn<PayrollJVCustom051ReportRowDto>.FromLiteralHeader(
            "SortOrder",
            r => r.SortOrder));

        return columns;
    }

    private class JvSourceLine
    {
        public double Amount { get; set; }

        public int EmployeeId { get; set; }

        public string EmployeeCode { get; set; }

        public string EmployeeName { get; set; }

        public string WageTypeCode { get; set; }
    }

    private class AccountRef
    {
        public string Code { get; set; }

        public string NameEn { get; set; }
    }
}
