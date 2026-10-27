using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
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
/// Payroll JV (Custom 045) analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPayrollJV_Custom045.ascx(.cs)</c>,
/// method <c>Table1()</c> (lines 557-1145).
///
/// THREE SHEETS BEHIND ONE SCREEN. <c>cmbType</c> selects Salary JV (30 columns), Missing Accounts (13) or
/// EmployeeWise BreakDown (17). They share only the preamble -- the filtered
/// <c>SalaryRegisterDetail</c> left-joined to <c>EmployeeWageAccount</c> (lines 567-705) -- and nothing
/// else, so the type is modelled as a filter with a server-served column list rather than as three
/// reports.
///
/// LITERAL COLUMN CAPTIONS: the Salary JV headers are ERP import tokens
/// ("lines.debit.value.amount", "lines.subsidiary#type"). They go through
/// <c>GridReportColumn.FromLiteralHeader</c>, never through localization, which would render them as
/// [lines.debit.value.amount] and break the import.
///
/// ENGLISH NAMES ONLY, DELIBERATELY: legacy reads <c>FullNameEn</c>/<c>NameEn</c> throughout with no
/// <c>CurrentAspect == "ar-KW"</c> switch, so <see cref="GridReportAppServiceBase.UseArabicNames"/> is not
/// consulted -- the values are finance master-data keys, not UI labels.
///
/// === FLAGGED ACCOUNTING DEFECTS, REPRODUCED (each pinned by a test named to say so) ===
///
/// 1. CREDIT LINES POST THEIR AMOUNT TO THE DEBIT COLUMN (lines 947-948). The Credit Entry loop writes
///    <c>lines.debit.value.amount = CostAmount</c> and <c>lines.credit.value.amount = 0</c> -- identical to
///    the Debit Entry loop (lines 875-876). Only the LOCAL amount pair is swapped (lines 951-952). Any
///    importer that reads the value pair rather than the local pair therefore books every deduction and
///    every net-pay line as a debit, and the voucher cannot balance.
///
/// 2. THE NARRATION LOSES ITS PERIOD ON EVERY MAPPED LINE (lines 885, 957):
///    <c>(rec.AccountInfo != null) ? rec.AccountInfo.NameEn : "" + " " + voucherDate</c>. C# binds
///    <c>+</c> tighter than <c>?:</c>, so the month is appended only in the NULL branch: a line WITH an
///    account gets a bare account name and a line WITHOUT one gets " Jun 2024".
///
/// 3. EMPLOYEEWISE BREAKDOWN SILENTLY ZEROES NEGATIVE AMOUNTS (lines 1130-1131):
///    <c>CostAmount >= 0 ? CostAmount : 0</c> for both Fc amount and Net amount, while the row itself is
///    still emitted and still carries a Dr/Cr marker. A negative net becomes a zero-value line rather than
///    a credit.
///
/// 4. MISSING ACCOUNTS CANNOT SHOW COST CENTRES. The sheet declares Division/Department/Analysis (lines
///    1002-1004) and fills them from CostElement1..3 (lines 1030-1032) -- but the rows it iterates are
///    exactly those where the left join produced NO <c>EmployeeWageAccount</c>, so those three columns and
///    CostPercentage are always blank/0 by construction.
///
/// 5. THE "sector"/"branch"/"department" DIMENSION CODES CARRY NAMES, NOT CODES (lines 887-892): both the
///    code column and the @name column are filled from <c>CostElement.NameEn</c>. The literal "PUBLIC"
///    is used when the cost element is missing, so an unmapped dimension is indistinguishable from a real
///    one called PUBLIC.
///
/// DELIBERATE DIVERGENCE: ROW LEVEL SECURITY IS APPLIED. Legacy declares <c>ApplyRowLevelSecurity</c>
/// (lines 1332-1342) and never calls it. Applied here (empty list == no restriction), an agreed wave-wide
/// divergence -- JV sheets carry salary data.
///
/// NOT PORTED -- DevExpress/WebForms plumbing with no data meaning: the drill-down session stack, the
/// ColumnChooser JSON persistence, the callback-panel state machine, the PDF export button, and the dead
/// <c>"Advance Salary JV"</c> run-type override at lines 585-586 (no such <c>cmbType</c> branch exists in
/// this control).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_PayrollJVCustom045)]
public class PayrollJVCustom045ReportAppService : GridReportAppServiceBase, IPayrollJVCustom045ReportAppService
{
    private const string NetPayWageTypeCode = "9000";
    private const string CurrencyShort = "KWD";
    private const string CurrencyFull = "Kuwaiti Dinar";
    private const string PublicDimension = "PUBLIC";
    private const string EmployeeEntity = "Employee";
    private const string Ref1TypeDocCategory = "DocCategory";

    // --- Salary JV captions (legacy lines 771-800) ---
    private const string SjDetailLines = ":-detail:lines";
    private const string SjAccount = "lines.account";
    private const string SjAccountName = "lines.account@name";
    private const string SjSubsidiaryType = "lines.subsidiary#type";
    private const string SjSubsidiaryCode = "lines.subsidiary#code";
    private const string SjSubsidiaryName = "lines.subsidiary@name";
    private const string SjDebitValueAmount = "lines.debit.value.amount";
    private const string SjCreditValueAmount = "lines.credit.value.amount";
    private const string SjDebitValueCurrency = "lines.debit.value.currency";
    private const string SjDebitValueCurrencyName = "lines.debit.value.currency@name";
    private const string SjDebitRate = "lines.debit.rate";
    private const string SjDebitLocalAmount = "lines.debit.localAmount";
    private const string SjCreditLocalAmount = "lines.credit.localAmount";
    private const string SjNarration = "lines.narration";
    private const string SjRef1Type = "lines.ref1#type";
    private const string SjRef1Code = "lines.ref1#code";
    private const string SjRef1Name = "lines.ref1@name";
    private const string SjSector = "lines.sector";
    private const string SjSectorName = "lines.sector@name";
    private const string SjBranch = "lines.branch";
    private const string SjBranchName = "lines.branch@name";
    private const string SjDepartment = "lines.department";
    private const string SjDepartmentName = "lines.department@name";
    private const string SjAnalysisSet = "lines.analysisSet";
    private const string SjAnalysisSetName = "lines.analysisSet@name";
    private const string SjEntityDimensionType = "lines.entityDimension#type";
    private const string SjEntityDimensionCode = "lines.entityDimension#code";
    private const string SjEntityDimensionName = "lines.entityDimension@name";
    private const string SjAttachment = "lines.attachment";

    // --- Missing Accounts captions (legacy lines 976-1008) ---
    private const string MaEmpCode = "EmpCode";
    private const string MaForPeriod = "ForPeriod";
    private const string MaWageType = "WageType";
    private const string MaAccountInfoCode = "AccountInfo Code";
    private const string MaAccountInfoName = "AccountInfo Name";
    private const string MaOffsetAccountInfoCode = "OffsetAccountInfo Code";
    private const string MaOffsetAccountInfoName = "OffsetAccountInfo Name";
    private const string MaDivision = "Division";
    private const string MaDepartment = "Department";
    private const string MaAnalysis = "Analysis";
    private const string MaCostPercentage = "CostPercentage";
    private const string MaAmount = "Amount";

    // --- EmployeeWise BreakDown captions (legacy lines 1050-1086). "Anl2" is legacy's own typo for the
    // variable named Anly2 and is kept verbatim: the customer reconciles against the legacy header. ---
    private const string BdEmployeeCode = "EmployeeCode";
    private const string BdMain = "Main";
    private const string BdMainDescription = "Main descripton";
    private const string BdSub = "Sub";
    private const string BdSubDescription = "SubDescription";
    private const string BdTdDesc = "TD_DESC";
    private const string BdDiv = "Div";
    private const string BdDept = "Dept";
    private const string BdAnly1 = "Anly1";
    private const string BdAnly2 = "Anl2";
    private const string BdAct1 = "Act1";
    private const string BdAct2 = "Act2";
    private const string BdCurrency = "Currency";
    private const string BdFcAmount = "Fc amount";
    private const string BdNetAmount = "Net amount";
    private const string BdDrCr = "Dr/Cr";

    private static readonly (string Name, bool IsNumeric)[] SalaryJvColumns =
    {
        (SjDetailLines, false),
        (SjAccount, false),
        (SjAccountName, false),
        (SjSubsidiaryType, false),
        (SjSubsidiaryCode, false),
        (SjSubsidiaryName, false),
        (SjDebitValueAmount, true),
        (SjCreditValueAmount, true),
        (SjDebitValueCurrency, false),
        (SjDebitValueCurrencyName, false),
        (SjDebitRate, false),
        (SjDebitLocalAmount, true),
        (SjCreditLocalAmount, true),
        (SjNarration, false),
        (SjRef1Type, false),
        (SjRef1Code, false),
        (SjRef1Name, false),
        (SjSector, false),
        (SjSectorName, false),
        (SjBranch, false),
        (SjBranchName, false),
        (SjDepartment, false),
        (SjDepartmentName, false),
        (SjAnalysisSet, false),
        (SjAnalysisSetName, false),
        (SjEntityDimensionType, false),
        (SjEntityDimensionCode, false),
        (SjEntityDimensionName, false),
        (SjAttachment, false)
    };

    private static readonly (string Name, bool IsNumeric)[] MissingAccountsColumns =
    {
        (MaEmpCode, false),
        (MaForPeriod, false),
        (MaWageType, false),
        (MaAccountInfoCode, false),
        (MaAccountInfoName, false),
        (MaOffsetAccountInfoCode, false),
        (MaOffsetAccountInfoName, false),
        (MaDivision, false),
        (MaDepartment, false),
        (MaAnalysis, false),
        (MaCostPercentage, true),
        (MaAmount, true)
    };

    private static readonly (string Name, bool IsNumeric)[] BreakDownColumns =
    {
        (BdEmployeeCode, false),
        (BdMain, false),
        (BdMainDescription, false),
        (BdSub, false),
        (BdSubDescription, false),
        (BdTdDesc, false),
        (BdDiv, false),
        (BdDept, false),
        (BdAnly1, false),
        (BdAnly2, false),
        (BdAct1, false),
        (BdAct2, false),
        (BdCurrency, false),
        (BdFcAmount, true),
        (BdNetAmount, true),
        (BdDrCr, false)
    };

    private readonly IRepository<SalaryRegisterDetail> _salaryRegisterDetailRepository;
    private readonly IRepository<EmployeeWageAccount> _employeeWageAccountRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;

    public PayrollJVCustom045ReportAppService(
        IRepository<SalaryRegisterDetail> salaryRegisterDetailRepository,
        IRepository<EmployeeWageAccount> employeeWageAccountRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository)
    {
        _salaryRegisterDetailRepository = salaryRegisterDetailRepository;
        _employeeWageAccountRepository = employeeWageAccountRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
    }

    protected override string LegacyScreenName => "uscRptPayrollJV_Custom045";

    protected override string ScreenCaption => "Payroll JV";

    [HttpPost]
    public async Task<PayrollJVCustom045ReportOutput> GetPayrollJVCustom045Report(
        GetPayrollJVCustom045ReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var totalCount = query.Count();

        var rows = query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToList();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildJvParameterString(input), totalCount);

        return new PayrollJVCustom045ReportOutput
        {
            TotalCount = totalCount,
            Items = rows,
            Columns = GetColumnDefinitions(input.ReportType)
                .Select(c => new PayrollJVCustom045ReportColumnDto { Name = c.Name, IsNumeric = c.IsNumeric })
                .ToList()
        };
    }

    [HttpPost]
    public async Task<FileDto> GetPayrollJVCustom045ReportToExcel(GetPayrollJVCustom045ReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the entire filtered data set,
        // and a partial journal voucher is worse than no journal voucher.
        var rows = query
            .OrderBy(input.Sorting)
            .ToList();

        return await ExportToFileAsync(
            "PayrollJVCustom045.xlsx",
            rows,
            GetExportColumns(input.ReportType),
            BuildJvParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export --
    /// this is what guarantees the export honours exactly the same filters as the grid. Ports legacy
    /// <c>Table1()</c> in full: the shared preamble (lines 567-705) then the selected type's fill loop.
    ///
    /// Returns an IQueryable over an in-memory list because the legacy shape is an aggregation with
    /// running serial numbers, not a straight projection -- legacy likewise materialised at line 640.
    /// </summary>
    private async Task<IQueryable<PayrollJVCustom045ReportRowDto>> BuildQueryAsync(
        GetPayrollJVCustom045ReportInput input)
    {
        var lines = await LoadJoinedLinesAsync(input);
        var periodEnd = ResolvePeriodEnd(input);

        // Legacy line 634: "MMM yyyy", rendered invariantly so the voucher text does not change with the
        // caller's culture.
        var voucherDate = periodEnd.ToString("MMM yyyy", CultureInfo.InvariantCulture);

        var rows = input.ReportType switch
        {
            PayrollJVCustom045ReportType.MissingAccounts => BuildMissingAccountsRows(lines),
            PayrollJVCustom045ReportType.EmployeeWiseBreakDown => BuildBreakDownRows(lines, voucherDate),
            _ => BuildSalaryJvRows(lines, voucherDate)
        };

        return rows.AsQueryable();
    }

    /// <summary>
    /// The preamble every type shares: the filtered <c>SalaryRegisterDetail</c> set (legacy lines 567-612)
    /// LEFT JOINED to the effective-dated <c>EmployeeWageAccount</c> mapping (lines 655-705).
    /// </summary>
    private async Task<List<JoinedLine>> LoadJoinedLinesAsync(GetPayrollJVCustom045ReportInput input)
    {
        var periodEnd = ResolvePeriodEnd(input);
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var payrollAreaId = input.PayrollAreaId;
        var runType = input.PayrollRunType;

        var registerQuery = _salaryRegisterDetailRepository.GetAll()
            // Legacy lines 568-576: the period's own regular result, FI-posting wage types only, and only
            // the three billing types that carry money.
            .Where(d => d.SalaryRegister.ForPeriod == periodEnd
                        && d.SalaryRegister.ForPeriod == d.SalaryRegister.InPeriod
                        && d.WageType.IncludeInFIPosting
                        && (d.WageType.BillingType == BillingType.Earning
                            || d.WageType.BillingType == BillingType.Deduction
                            || d.WageType.BillingType == BillingType.Provision))
            // Legacy lines 580-589.
            .WhereIf(runType.HasValue, d => d.SalaryRegister.PayrollRunType == runType.Value)
            // Legacy lines 594-600: an org assignment CURRENT AS OF the period end in that payroll area.
            .WhereIf(payrollAreaId.HasValue, d => _orgAssignmentRepository.GetAll()
                .Any(o => o.EmployeeId == d.SalaryRegister.EmployeeId
                          && o.StartDate <= periodEnd
                          && o.EndDate >= periodEnd
                          && o.PayrollAreaId == payrollAreaId.Value))
            // Legacy lines 605-611.
            .WhereIf(employeeCodes.Count > 0, d => employeeCodes.Contains(d.SalaryRegister.Employee.Code))
            // DELIBERATE DIVERGENCE: legacy never called its own ApplyRowLevelSecurity.
            .WhereIf(rlsEmployeeIds.Count > 0, d => rlsEmployeeIds.Contains(d.SalaryRegister.EmployeeId));

        // Legacy lines 615-635.
        var registerLines = await registerQuery
            .Select(d => new
            {
                d.SalaryRegister.ForPeriod,
                d.SalaryRegister.EmployeeId,
                EmployeeCode = d.SalaryRegister.Employee.Code,
                EmployeeName = d.SalaryRegister.Employee.FullNameEn,
                d.WageTypeId,
                WageTypeCode = d.WageType.Code,
                WageTypeName = d.WageType.NameEn,
                d.WageType.BillingType,
                d.Amount
            })
            .ToListAsync();

        // Legacy lines 655-671: the mapping current as of the period end, narrowed by the same employee
        // codes.
        var mappings = await _employeeWageAccountRepository.GetAll()
            .Where(w => w.StartDate <= periodEnd && w.EndDate >= periodEnd)
            .WhereIf(employeeCodes.Count > 0, w => employeeCodes.Contains(w.Employee.Code))
            .Select(w => new WageAccountRef
            {
                EmployeeId = w.EmployeeId,
                WageTypeId = w.WageTypeId,
                AccountCode = w.AccountInfo.Code,
                AccountName = w.AccountInfo.NameEn,
                HasAccount = true,
                OffsetAccountCode = w.OffsetAccountInfo == null ? null : w.OffsetAccountInfo.Code,
                OffsetAccountName = w.OffsetAccountInfo == null ? null : w.OffsetAccountInfo.NameEn,
                CostElement1Id = w.CostElement1Id,
                CostElement1Code = w.CostElement1 == null ? null : w.CostElement1.Code,
                CostElement1Name = w.CostElement1 == null ? null : w.CostElement1.NameEn,
                CostElement2Id = w.CostElement2Id,
                CostElement2Code = w.CostElement2 == null ? null : w.CostElement2.Code,
                CostElement2Name = w.CostElement2 == null ? null : w.CostElement2.NameEn,
                CostElement3Id = w.CostElement3Id,
                CostElement3Code = w.CostElement3 == null ? null : w.CostElement3.Code,
                CostElement3Name = w.CostElement3 == null ? null : w.CostElement3.NameEn,
                CostPercentage = w.CostPercentage
            })
            .ToListAsync();

        var mappingLookup = mappings.ToLookup(m => (m.EmployeeId, m.WageTypeId));

        // Legacy lines 673-702: a LEFT join, so a register line with no mapping survives with nulls.
        var joined = new List<JoinedLine>();

        foreach (var line in registerLines)
        {
            var matches = mappingLookup[(line.EmployeeId, line.WageTypeId)].ToList();

            if (matches.Count == 0)
            {
                matches.Add(null);
            }

            foreach (var mapping in matches)
            {
                joined.Add(new JoinedLine
                {
                    ForPeriod = line.ForPeriod,
                    EmployeeId = line.EmployeeId,
                    EmployeeCode = line.EmployeeCode,
                    EmployeeName = line.EmployeeName,
                    WageTypeId = line.WageTypeId,
                    WageTypeCode = line.WageTypeCode,
                    WageTypeName = line.WageTypeName,
                    BillingType = line.BillingType,
                    Amount = line.Amount,
                    Mapping = mapping
                });
            }
        }

        return joined;
    }

    /// <summary>Legacy "Salary JV" branch (lines 758-987).</summary>
    private List<PayrollJVCustom045ReportRowDto> BuildSalaryJvRows(List<JoinedLine> lines, string voucherDate)
    {
        var rows = new List<PayrollJVCustom045ReportRowDto>();

        // Legacy lines 837-858: earnings other than net pay.
        var debitGroups = GroupForSalaryJv(lines.Where(l =>
            l.BillingType == BillingType.Earning && l.WageTypeCode != NetPayWageTypeCode));

        foreach (var group in debitGroups)
        {
            rows.Add(BuildSalaryJvRow(rows.Count + 1, group, voucherDate, isCreditEntry: false));
        }

        // Legacy lines 909-930: deductions plus net pay.
        var creditGroups = GroupForSalaryJv(lines.Where(l =>
            l.BillingType == BillingType.Deduction || l.WageTypeCode == NetPayWageTypeCode));

        foreach (var group in creditGroups)
        {
            rows.Add(BuildSalaryJvRow(rows.Count + 1, group, voucherDate, isCreditEntry: true));
        }

        return rows;
    }

    /// <summary>
    /// Legacy grouping key for both Salary JV loops (lines 840, 912): employee + both accounts + the three
    /// cost elements + currency, with zero-total groups dropped.
    /// </summary>
    private static List<SalaryJvGroup> GroupForSalaryJv(IEnumerable<JoinedLine> lines)
    {
        return lines
            .GroupBy(l => new
            {
                l.EmployeeId,
                l.EmployeeCode,
                l.EmployeeName,
                AccountCode = l.Mapping?.AccountCode,
                AccountName = l.Mapping?.AccountName,
                HasAccount = l.Mapping != null,
                OffsetAccountCode = l.Mapping?.OffsetAccountCode,
                // Legacy names CostElement1 "Branch", CostElement2 "Sector" and CostElement3 "Department"
                // in this control (lines 846-848) -- note this differs from its siblings.
                BranchName = l.Mapping?.CostElement1Name,
                BranchId = l.Mapping?.CostElement1Id,
                SectorName = l.Mapping?.CostElement2Name,
                SectorId = l.Mapping?.CostElement2Id,
                DepartmentName = l.Mapping?.CostElement3Name,
                DepartmentId = l.Mapping?.CostElement3Id
            })
            .Select(g => new SalaryJvGroup
            {
                EmployeeCode = g.Key.EmployeeCode,
                EmployeeName = g.Key.EmployeeName,
                AccountCode = g.Key.HasAccount ? g.Key.AccountCode : null,
                AccountName = g.Key.HasAccount ? g.Key.AccountName : null,
                BranchName = g.Key.BranchName,
                SectorName = g.Key.SectorName,
                DepartmentName = g.Key.DepartmentName,
                CostAmount = g.Sum(l => l.Amount)
            })
            .Where(g => g.CostAmount != 0)
            .ToList();
    }

    /// <summary>
    /// Legacy Debit Entry (lines 860-905) and Credit Entry (lines 932-977). The two loops are
    /// character-for-character identical except for the subsidiary block and the local-amount pair, so they
    /// share one builder here -- including legacy defects 1 and 2 (see the class remarks).
    /// </summary>
    private static PayrollJVCustom045ReportRowDto BuildSalaryJvRow(
        int serialNumber,
        SalaryJvGroup group,
        string voucherDate,
        bool isCreditEntry)
    {
        var row = NewRow(serialNumber, SalaryJvColumns);

        row.Values[SjDetailLines] = string.Empty;
        row.Values[SjAccount] = group.AccountCode ?? string.Empty;
        row.Values[SjAccountName] = group.AccountName ?? string.Empty;

        if (isCreditEntry)
        {
            // Legacy lines 941-943.
            row.Values[SjSubsidiaryType] = EmployeeEntity;
            row.Values[SjSubsidiaryCode] = group.EmployeeCode;
            row.Values[SjSubsidiaryName] = group.EmployeeName;
        }
        else
        {
            // Legacy lines 869-871.
            row.Values[SjSubsidiaryType] = string.Empty;
            row.Values[SjSubsidiaryCode] = string.Empty;
            row.Values[SjSubsidiaryName] = string.Empty;
        }

        // LEGACY DEFECT 1 REPRODUCED: the CREDIT loop also writes the amount to the DEBIT value column and
        // zero to the credit value column (lines 947-948, identical to 875-876).
        row.Amounts[SjDebitValueAmount] = group.CostAmount;
        row.Amounts[SjCreditValueAmount] = 0;

        row.Values[SjDebitValueCurrency] = CurrencyShort;
        row.Values[SjDebitValueCurrencyName] = CurrencyFull;
        // Legacy stored the rate as the STRING "1" (line 819/880), not a number.
        row.Values[SjDebitRate] = "1";

        if (isCreditEntry)
        {
            // Legacy lines 951-952.
            row.Amounts[SjDebitLocalAmount] = group.CostAmount < 0 ? -group.CostAmount : 0;
            row.Amounts[SjCreditLocalAmount] = group.CostAmount >= 0 ? group.CostAmount : 0;
        }
        else
        {
            // Legacy lines 879-880.
            row.Amounts[SjDebitLocalAmount] = group.CostAmount >= 0 ? group.CostAmount : 0;
            row.Amounts[SjCreditLocalAmount] = group.CostAmount < 0 ? -group.CostAmount : 0;
        }

        // LEGACY DEFECT 2 REPRODUCED: `(x != null) ? x.NameEn : "" + " " + voucherDate` binds as
        // `(x != null) ? x.NameEn : ("" + " " + voucherDate)`, so the period is appended ONLY when there is
        // no account (lines 885, 957).
        row.Values[SjNarration] = group.AccountName ?? (" " + voucherDate);

        row.Values[SjRef1Type] = Ref1TypeDocCategory;
        row.Values[SjRef1Code] = string.Empty;
        row.Values[SjRef1Name] = string.Empty;

        // LEGACY DEFECT 5 REPRODUCED: the dimension CODE columns are filled from NameEn, and an unmapped
        // dimension becomes the literal "PUBLIC" (lines 887-895).
        row.Values[SjSector] = group.SectorName ?? PublicDimension;
        row.Values[SjSectorName] = group.SectorName ?? PublicDimension;
        row.Values[SjBranch] = group.BranchName ?? PublicDimension;
        row.Values[SjBranchName] = group.BranchName ?? PublicDimension;
        row.Values[SjDepartment] = group.DepartmentName ?? PublicDimension;
        row.Values[SjDepartmentName] = group.DepartmentName ?? PublicDimension;

        row.Values[SjAnalysisSet] = PublicDimension;
        row.Values[SjAnalysisSetName] = PublicDimension;

        // Legacy lines 899-902: the type is blank and the CODE column carries the literal "Employee" while
        // the NAME column carries the employee code. Reproduced verbatim.
        row.Values[SjEntityDimensionType] = string.Empty;
        row.Values[SjEntityDimensionCode] = EmployeeEntity;
        row.Values[SjEntityDimensionName] = group.EmployeeCode;
        row.Values[SjAttachment] = group.EmployeeName;

        return row;
    }

    /// <summary>
    /// Legacy "Missing Accounts" branch (lines 990-1042): the joined lines whose left join found no
    /// mapping. See defect 4 in the class remarks for why the cost-centre columns are always blank.
    /// </summary>
    private static List<PayrollJVCustom045ReportRowDto> BuildMissingAccountsRows(List<JoinedLine> lines)
    {
        var rows = new List<PayrollJVCustom045ReportRowDto>();

        foreach (var line in lines.Where(l => l.Mapping == null))
        {
            var row = NewRow(rows.Count + 1, MissingAccountsColumns);

            row.Values[MaEmpCode] = line.EmployeeCode;
            // Legacy typed this column DateTime; carried as an invariant ISO date here.
            row.Values[MaForPeriod] = line.ForPeriod.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            row.Values[MaWageType] = line.WageTypeName;

            row.Values[MaAccountInfoCode] = string.Empty;
            row.Values[MaAccountInfoName] = string.Empty;
            row.Values[MaOffsetAccountInfoCode] = string.Empty;
            row.Values[MaOffsetAccountInfoName] = string.Empty;

            row.Values[MaDivision] = string.Empty;
            row.Values[MaDepartment] = string.Empty;
            row.Values[MaAnalysis] = string.Empty;

            row.Amounts[MaCostPercentage] = 0;
            row.Amounts[MaAmount] = line.Amount;

            rows.Add(row);
        }

        return rows;
    }

    /// <summary>Legacy "EmployeeWise BreakDown" branch (lines 1044-1140).</summary>
    private static List<PayrollJVCustom045ReportRowDto> BuildBreakDownRows(
        List<JoinedLine> lines,
        string voucherDate)
    {
        // Legacy lines 1088-1112: EVERY billing type, grouped per employee AND wage type. The
        // earning/non-9000 filter of the Salary JV branch is commented out at line 1089.
        var groups = lines
            .GroupBy(l => new
            {
                l.EmployeeId,
                l.EmployeeCode,
                l.WageTypeId,
                l.WageTypeName,
                l.BillingType,
                AccountCode = l.Mapping?.AccountCode,
                AccountName = l.Mapping?.AccountName,
                OffsetAccountCode = l.Mapping?.OffsetAccountCode,
                OffsetAccountName = l.Mapping?.OffsetAccountName,
                DivisionCode = l.Mapping?.CostElement1Code,
                DepartmentCode = l.Mapping?.CostElement2Code,
                AnalysisCode = l.Mapping?.CostElement3Code
            })
            .Select(g => new
            {
                g.Key,
                CostAmount = g.Sum(l => l.Amount)
            })
            .Where(g => g.CostAmount != 0)
            .ToList();

        var rows = new List<PayrollJVCustom045ReportRowDto>();

        foreach (var group in groups)
        {
            var row = NewRow(rows.Count + 1, BreakDownColumns);

            row.Values[BdEmployeeCode] = group.Key.EmployeeCode;
            row.Values[BdMain] = group.Key.AccountCode ?? string.Empty;
            row.Values[BdMainDescription] = group.Key.AccountName ?? string.Empty;
            row.Values[BdSub] = group.Key.OffsetAccountCode ?? string.Empty;
            row.Values[BdSubDescription] = group.Key.OffsetAccountName ?? string.Empty;

            // Legacy line 1119.
            row.Values[BdTdDesc] = group.Key.BillingType + "-" + group.Key.WageTypeName +
                                   "-For the month of " + voucherDate;

            // Legacy lines 1122-1127: the literal string "NULL", not an empty cell.
            row.Values[BdDiv] = group.Key.DivisionCode ?? "NULL";
            row.Values[BdDept] = group.Key.DepartmentCode ?? "NULL";
            row.Values[BdAnly1] = group.Key.AnalysisCode ?? "NULL";
            row.Values[BdAnly2] = "NULL";
            row.Values[BdAct1] = "NULL";
            row.Values[BdAct2] = "NULL";
            row.Values[BdCurrency] = CurrencyShort;

            // LEGACY DEFECT 3 REPRODUCED: a negative total is written as ZERO on both amount columns while
            // the row is still emitted (lines 1130-1131).
            row.Amounts[BdFcAmount] = group.CostAmount >= 0 ? group.CostAmount : 0;
            row.Amounts[BdNetAmount] = group.CostAmount >= 0 ? group.CostAmount : 0;

            // Legacy line 1132.
            row.Values[BdDrCr] =
                group.Key.BillingType == BillingType.Earning || group.Key.BillingType == BillingType.Provision
                    ? "D"
                    : "C";

            rows.Add(row);
        }

        return rows;
    }

    private static PayrollJVCustom045ReportRowDto NewRow(
        int serialNumber,
        (string Name, bool IsNumeric)[] columns)
    {
        var row = new PayrollJVCustom045ReportRowDto
        {
            Id = serialNumber,
            SerialNumber = serialNumber
        };

        foreach (var (name, isNumeric) in columns)
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

        return row;
    }

    private static (string Name, bool IsNumeric)[] GetColumnDefinitions(PayrollJVCustom045ReportType type)
    {
        return type switch
        {
            PayrollJVCustom045ReportType.MissingAccounts => MissingAccountsColumns,
            PayrollJVCustom045ReportType.EmployeeWiseBreakDown => BreakDownColumns,
            _ => SalaryJvColumns
        };
    }

    /// <summary>
    /// Legacy <c>GetTable()</c> lines 475-492: Month/Year default to today's and resolve to the LAST day of
    /// that month; an unparseable pair fell back to 1900-01-31.
    /// </summary>
    private DateTime ResolvePeriodEnd(GetPayrollJVCustom045ReportInput input)
    {
        var month = input.Month ?? Today.Month;
        var year = input.Year ?? Today.Year;

        if (month < 1 || month > 12 || year < 1 || year > 9999)
        {
            return new DateTime(1900, 1, 31);
        }

        return new DateTime(year, month, DateTime.DaysInMonth(year, month));
    }

    /// <summary>
    /// Legacy <c>GetParameterStr</c> (lines 525-548): Payroll Area, Run Type, Month, Year, Type,
    /// Employee Code.
    /// </summary>
    private string BuildJvParameterString(GetPayrollJVCustom045ReportInput input)
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
        parts.Add("Type : " + input.ReportType);

        if (!input.EmployeeCode.IsNullOrWhiteSpace())
        {
            parts.Add("Employee Code : " + input.EmployeeCode);
        }

        return string.Join(" | ", parts);
    }

    /// <summary>
    /// The exported column set of the SELECTED type, in legacy declaration order. Every header goes through
    /// <c>FromLiteralHeader</c> -- these are ERP import tokens reconciled against legacy output, not
    /// localization keys, so none of them can ever render as an unresolved [Key].
    /// </summary>
    private static IReadOnlyList<GridReportColumn<PayrollJVCustom045ReportRowDto>> GetExportColumns(
        PayrollJVCustom045ReportType type)
    {
        var columns = new List<GridReportColumn<PayrollJVCustom045ReportRowDto>>
        {
            GridReportColumn<PayrollJVCustom045ReportRowDto>.FromLiteralHeader("Oid", r => r.SerialNumber)
        };

        foreach (var (name, isNumeric) in GetColumnDefinitions(type))
        {
            var columnName = name;

            columns.Add(isNumeric
                ? GridReportColumn<PayrollJVCustom045ReportRowDto>.FromLiteralHeader(
                    columnName,
                    r => r.Amounts.TryGetValue(columnName, out var amount) ? amount : 0)
                : GridReportColumn<PayrollJVCustom045ReportRowDto>.FromLiteralHeader(
                    columnName,
                    r => r.Values.TryGetValue(columnName, out var value) ? value : string.Empty));
        }

        return columns;
    }

    private class JoinedLine
    {
        public DateTime ForPeriod { get; set; }

        public int EmployeeId { get; set; }

        public string EmployeeCode { get; set; }

        public string EmployeeName { get; set; }

        public int WageTypeId { get; set; }

        public string WageTypeCode { get; set; }

        public string WageTypeName { get; set; }

        public BillingType BillingType { get; set; }

        public double Amount { get; set; }

        /// <summary>Null when the left join found no <c>EmployeeWageAccount</c> for the period.</summary>
        public WageAccountRef Mapping { get; set; }
    }

    private class WageAccountRef
    {
        public int EmployeeId { get; set; }

        public int WageTypeId { get; set; }

        public string AccountCode { get; set; }

        public string AccountName { get; set; }

        public bool HasAccount { get; set; }

        public string OffsetAccountCode { get; set; }

        public string OffsetAccountName { get; set; }

        public int? CostElement1Id { get; set; }

        public string CostElement1Code { get; set; }

        public string CostElement1Name { get; set; }

        public int? CostElement2Id { get; set; }

        public string CostElement2Code { get; set; }

        public string CostElement2Name { get; set; }

        public int? CostElement3Id { get; set; }

        public string CostElement3Code { get; set; }

        public string CostElement3Name { get; set; }

        public double CostPercentage { get; set; }
    }

    private class SalaryJvGroup
    {
        public string EmployeeCode { get; set; }

        public string EmployeeName { get; set; }

        public string AccountCode { get; set; }

        public string AccountName { get; set; }

        public string BranchName { get; set; }

        public string SectorName { get; set; }

        public string DepartmentName { get; set; }

        public double CostAmount { get; set; }
    }
}
