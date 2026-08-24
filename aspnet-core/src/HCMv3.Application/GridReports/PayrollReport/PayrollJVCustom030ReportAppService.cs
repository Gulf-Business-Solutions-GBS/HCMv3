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
/// Payroll JV (Custom 030) analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPayrollJV_Custom030.ascx(.cs)</c>,
/// method <c>Table1()</c> (lines 558-1815).
///
/// FIVE SHEETS BEHIND ONE SCREEN (<c>cmbType</c>): Salary JV, Missing Accounts, EmployeeWise BreakDown,
/// Indemnity JV and Annual Leave JV. The first three share the preamble (lines 563-762); the last two
/// re-query from scratch and share nothing but the period.
///
/// A TWO-COLUMN PIVOT, NOT A LINE-PER-ACCOUNT VOUCHER. This customer books everything to two cost
/// centres identified by <c>CostElement1.Code</c>: "1" is Ops and "2" is Admin (lines 810-816). Every
/// amount is pro-rated by <c>EmployeeWageAccount.CostPercentage</c>. "Lowwo" (legacy's misspelling of
/// Lowvo) is the Ops+Admin total.
///
/// ENGLISH NAMES ONLY, DELIBERATELY: legacy reads <c>NameEn</c> throughout with no
/// <c>CurrentAspect == "ar-KW"</c> switch, so <see cref="GridReportAppServiceBase.UseArabicNames"/> is not
/// consulted -- these are finance master-data keys, not UI labels.
///
/// === FLAGGED ACCOUNTING DEFECTS ===
///
/// 1. REPRODUCED -- THE NET PAY ROW DROPS EVERY EMPLOYEE WHOSE NET IS NEGATIVE (line 986:
///    <c>.Where(r => r.Net >= 0)</c>). An employee who over-recovered in the month simply does not
///    contribute, so the Net Pay row does not equal the earnings less the deductions above it.
///
/// 2. REPRODUCED -- THE NET PAY ROW ALSO DROPS THE "Lowvo" BUCKET. Employees whose lines touch BOTH cost
///    centres (or neither) are bucketed "Lowvo" (lines 966-969), and only the "Ops" and "Admin" buckets are
///    summed into the row (lines 991-992). That money is computed and then discarded.
///
/// 3. REPRODUCED -- EMPLOYEEWISE BREAKDOWN ZEROES NEGATIVE AMOUNTS (line 1136:
///    <c>CostAmount >= 0 ? CostAmount : 0</c>) while still emitting the row.
///
/// 4. REPRODUCED -- INDEMNITY JV AND ANNUAL LEAVE JV IGNORE THE WHOLE FILTER PANEL. Both re-query
///    <c>SalaryRegisterDetail</c> with only a period and a billing-type predicate (lines 1157-1163,
///    1431-1438), so Payroll Area, Run Type and Employee Code have no effect on them at all.
///
/// 5. REPRODUCED -- THE ANNUAL LEAVE FORFEIT CALCULATION IS DEAD CODE. Lines 1622-1667 read
///    <c>EmployeeAbsQuotaForfeit</c>, derive a per-day rate (basic pay / 26) and accumulate
///    <c>totalOpsForfeit</c>/<c>totalAdminForfeit</c> -- which are then never read by any row. Forfeited
///    leave therefore never posts. Because the totals are provably unused, this port does not query
///    <c>EmployeeAbsQuotaForfeit</c> at all: the OUTPUT is identical and the dead query is not worth its
///    cost. Pinned by a test asserting the Annual Leave sheet is unaffected by forfeit records.
///
/// 6. REPRODUCED (cosmetic) -- on the two balance sheets legacy never increments its row counter, so every
///    "Oid" cell reads 0 (lines 1345-1408). The port writes the true emit order instead, because that
///    column is the paging key here.
///
/// === DIVERGENCES THAT CANNOT BE AVOIDED ===
///
/// * HARD-CODED LEGACY DATABASE ID NOT PORTED. Lines 675, 1231 and 1552 clamp negative amounts to zero for
///   <c>WageTypeOid == 65</c> -- a raw primary key of the client-030 production database. HCMv3 assigns its
///   own WageType ids, so reproducing the number would clamp an arbitrary, unrelated wage type. The clamp
///   is therefore NOT applied; it is the same class of problem as the Oid switch in
///   <c>PayrollSheetCustom023ReportAppService</c>, but unlike that one there is no caption or code to
///   recover the intended wage type from. FLAGGED FOR THE CUSTOMER: if wage type Oid 65 is identified, the
///   clamp must be reinstated keyed on its CODE.
/// * ROW LEVEL SECURITY IS APPLIED, including on the two balance sheets. Legacy declares
///   <c>ApplyRowLevelSecurity</c> and never calls it -- an agreed wave-wide divergence.
/// * WAGE TYPE 2011 MISSING NO LONGER CRASHES. The Last Month Recovery block dereferences
///   <c>wtLastMonthRecoveryDed.Oid</c> (line 717) on a <c>FirstOrDefault()</c>; the port skips the block
///   when the wage type does not exist rather than throwing <c>NullReferenceException</c>.
///
/// NOT PORTED -- DevExpress/WebForms plumbing with no data meaning: the drill-down session stack, the
/// ColumnChooser JSON persistence, the callback-panel state machine, the PDF export button, and the fully
/// commented-out <c>Table1_Old()</c> (lines 1823-3222).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_PayrollJVCustom030)]
public class PayrollJVCustom030ReportAppService : GridReportAppServiceBase, IPayrollJVCustom030ReportAppService
{
    private const string NetPayWageTypeCode = "9000";
    private const string LastMonthRecoveryWageTypeCode = "2011";
    private const string OpsDepartmentCode = "1";
    private const string AdminDepartmentCode = "2";
    private const string DrCrMarker = "+Dr/+Cr";

    /// <summary>
    /// Legacy "individual" wage types (lines 790-799): these are pivoted per Department/Account rather than
    /// rolled up, because the customer wants each recovery line visible on its own row.
    /// </summary>
    private static readonly string[] IndividualWageTypeCodes =
    {
        "2005", "8005", "2001", "8001", "2009", "8009", "2011", "8011"
    };

    // --- Salary JV captions (legacy lines 766-780) ---
    private const string SjDrCr = "DrCr";
    private const string SjWageType = "WageType";
    private const string SjOps = "Ops";
    private const string SjAdmin = "Admin";
    private const string SjLowwo = "Lowwo";
    private const string SjOpsAccountDescription = "Ops Account Description";
    private const string SjAdminAccountDescription = "Admin Account Description";

    // --- Missing Accounts captions (legacy lines 1015-1039) ---
    private const string MaEmpCode = "EmpCode";
    private const string MaForPeriod = "ForPeriod";
    private const string MaWageType = "WageType";
    private const string MaAccountInfoCode = "AccountInfo Code";
    private const string MaAccountInfoName = "AccountInfo Name";
    private const string MaDepartment = "Department";
    private const string MaCompany = "Company";
    private const string MaCostPercentage = "CostPercentage";
    private const string MaAmount = "Amount";

    // --- EmployeeWise BreakDown captions (legacy lines 1082-1103) ---
    private const string BdEmployeeCode = "EmployeeCode";
    private const string BdAccountInfoCode = "AccountInfo Code";
    private const string BdAccountInfoName = "AccountInfo Name";
    private const string BdDepartment = "Department";
    private const string BdCompany = "Company";
    private const string BdAmount = "Amount";

    // --- Balance-sheet captions, shared by Indemnity JV and Annual Leave JV (lines 1147-1157) ---
    private const string BalDescription = "Description";

    private static readonly (string Name, bool IsNumeric)[] SalaryJvColumns =
    {
        (SjDrCr, false),
        (SjWageType, false),
        (SjOps, true),
        (SjAdmin, true),
        (SjLowwo, true),
        (SjOpsAccountDescription, false),
        (SjAdminAccountDescription, false)
    };

    private static readonly (string Name, bool IsNumeric)[] MissingAccountsColumns =
    {
        (MaEmpCode, false),
        (MaForPeriod, false),
        (MaWageType, false),
        (MaAccountInfoCode, false),
        (MaAccountInfoName, false),
        (MaDepartment, false),
        (MaCompany, false),
        (MaCostPercentage, true),
        (MaAmount, true)
    };

    private static readonly (string Name, bool IsNumeric)[] BreakDownColumns =
    {
        (BdEmployeeCode, false),
        (BdAccountInfoCode, false),
        (BdAccountInfoName, false),
        (BdDepartment, false),
        (BdCompany, false),
        (BdAmount, true)
    };

    private static readonly (string Name, bool IsNumeric)[] BalanceColumns =
    {
        (BalDescription, false),
        (SjOps, true),
        (SjAdmin, true),
        (SjLowwo, true)
    };

    private readonly IRepository<SalaryRegisterDetail> _salaryRegisterDetailRepository;
    private readonly IRepository<EmployeeWageAccount> _employeeWageAccountRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly IRepository<WageType> _wageTypeRepository;

    public PayrollJVCustom030ReportAppService(
        IRepository<SalaryRegisterDetail> salaryRegisterDetailRepository,
        IRepository<EmployeeWageAccount> employeeWageAccountRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        IRepository<WageType> wageTypeRepository)
    {
        _salaryRegisterDetailRepository = salaryRegisterDetailRepository;
        _employeeWageAccountRepository = employeeWageAccountRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _wageTypeRepository = wageTypeRepository;
    }

    protected override string LegacyScreenName => "uscRptPayrollJV_Custom030";

    protected override string ScreenCaption => "Payroll JV";

    [HttpPost]
    public async Task<PayrollJVCustom030ReportOutput> GetPayrollJVCustom030Report(
        GetPayrollJVCustom030ReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var totalCount = query.Count();

        var rows = query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToList();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildJvParameterString(input), totalCount);

        return new PayrollJVCustom030ReportOutput
        {
            TotalCount = totalCount,
            Items = rows,
            Columns = GetColumnDefinitions(input.ReportType)
                .Select(c => new PayrollJVCustom030ReportColumnDto { Name = c.Name, IsNumeric = c.IsNumeric })
                .ToList()
        };
    }

    [HttpPost]
    public async Task<FileDto> GetPayrollJVCustom030ReportToExcel(GetPayrollJVCustom030ReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the entire filtered data set,
        // and a partial journal voucher is worse than no journal voucher.
        var rows = query
            .OrderBy(input.Sorting)
            .ToList();

        return await ExportToFileAsync(
            "PayrollJVCustom030.xlsx",
            rows,
            GetExportColumns(input.ReportType),
            BuildJvParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export --
    /// this is what guarantees the export honours exactly the same filters as the grid.
    /// </summary>
    private async Task<IQueryable<PayrollJVCustom030ReportRowDto>> BuildQueryAsync(
        GetPayrollJVCustom030ReportInput input)
    {
        var periodEnd = ResolvePeriodEnd(input);

        List<PayrollJVCustom030ReportRowDto> rows;

        switch (input.ReportType)
        {
            case PayrollJVCustom030ReportType.IndemnityJV:
                rows = await BuildIndemnityRowsAsync(periodEnd);
                break;

            case PayrollJVCustom030ReportType.AnnualLeaveJV:
                rows = await BuildAnnualLeaveRowsAsync(periodEnd);
                break;

            default:
                var lines = await LoadPreambleLinesAsync(input, periodEnd);

                rows = input.ReportType switch
                {
                    PayrollJVCustom030ReportType.MissingAccounts => BuildMissingAccountsRows(lines),
                    PayrollJVCustom030ReportType.EmployeeWiseBreakDown => BuildBreakDownRows(lines),
                    _ => BuildSalaryJvRows(lines)
                };
                break;
        }

        return rows.AsQueryable();
    }

    /// <summary>
    /// The preamble the first three sheets share (legacy lines 563-762): the filtered
    /// <c>SalaryRegisterDetail</c> set left-joined to the effective-dated <c>EmployeeWageAccount</c>
    /// mapping, then augmented with the synthetic "Last Month Recovery Ded" lines, then stripped of net pay.
    /// </summary>
    private async Task<List<JoinedLine>> LoadPreambleLinesAsync(
        GetPayrollJVCustom030ReportInput input,
        DateTime periodEnd)
    {
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var payrollAreaId = input.PayrollAreaId;
        var runType = input.PayrollRunType;

        var registerQuery = _salaryRegisterDetailRepository.GetAll()
            // Legacy lines 564-571. Provision is explicitly commented out of the billing-type set.
            .Where(d => d.SalaryRegister.ForPeriod == periodEnd
                        && d.SalaryRegister.ForPeriod == d.SalaryRegister.InPeriod
                        && d.WageType.IncludeInFIPosting
                        && (d.WageType.BillingType == BillingType.Earning
                            || d.WageType.BillingType == BillingType.Deduction))
            // Legacy lines 574-582.
            .WhereIf(runType.HasValue, d => d.SalaryRegister.PayrollRunType == runType.Value)
            // Legacy lines 584-594.
            .WhereIf(payrollAreaId.HasValue, d => _orgAssignmentRepository.GetAll()
                .Any(o => o.EmployeeId == d.SalaryRegister.EmployeeId
                          && o.StartDate <= periodEnd
                          && o.EndDate >= periodEnd
                          && o.PayrollAreaId == payrollAreaId.Value))
            // Legacy lines 596-602.
            .WhereIf(employeeCodes.Count > 0, d => employeeCodes.Contains(d.SalaryRegister.Employee.Code))
            // DELIBERATE DIVERGENCE: legacy never called its own ApplyRowLevelSecurity.
            .WhereIf(rlsEmployeeIds.Count > 0, d => rlsEmployeeIds.Contains(d.SalaryRegister.EmployeeId));

        // Legacy lines 605-624.
        var registerLines = await registerQuery
            .Select(d => new RegisterLine
            {
                SalaryRegisterId = d.SalaryRegisterId,
                ForPeriod = d.SalaryRegister.ForPeriod,
                EmployeeId = d.SalaryRegister.EmployeeId,
                EmployeeCode = d.SalaryRegister.Employee.Code,
                WageTypeId = d.WageTypeId,
                WageTypeCode = d.WageType.Code,
                WageTypeName = d.WageType.NameEn,
                BillingType = d.WageType.BillingType,
                PayrollOrder = d.WageType.PayrollOrder,
                Amount = d.Amount
            })
            .ToListAsync();

        var mappingLookup = await LoadMappingLookupAsync(periodEnd, employeeCodes);

        // NOTE: legacy line 675 clamps negative amounts for WageTypeOid == 65 here. Not portable -- see the
        // class remarks.
        var joined = JoinWithMappings(registerLines, mappingLookup);

        joined.AddRange(await BuildLastMonthRecoveryLinesAsync(
            periodEnd,
            registerLines,
            mappingLookup,
            rlsEmployeeIds));

        // Legacy lines 760-762: net pay is removed from the working set entirely; the Net Pay row is
        // recomputed from earnings less deductions instead.
        return joined.Where(l => l.WageTypeCode != NetPayWageTypeCode).ToList();
    }

    /// <summary>
    /// Legacy "Last Month Recovery Ded (NetPay = 0)" (lines 682-758): for every payslip whose net pay came
    /// out at exactly zero, the month's earnings less deductions are re-posted as a single synthetic
    /// wage-type-2011 line dated to the END OF THE FOLLOWING MONTH.
    ///
    /// NOTE the widened scope legacy used here: this second query is NOT narrowed by payroll area, employee
    /// code, run type or <c>IncludeInFIPosting</c> -- only by the payslips already found. Reproduced.
    /// </summary>
    private async Task<List<JoinedLine>> BuildLastMonthRecoveryLinesAsync(
        DateTime periodEnd,
        List<RegisterLine> registerLines,
        ILookup<(int EmployeeId, int WageTypeId), WageAccountRef> mappingLookup,
        List<int> rlsEmployeeIds)
    {
        // Legacy lines 686-688.
        var nextMonthStart = periodEnd.AddDays(1);
        var nextMonthEnd = new DateTime(
            nextMonthStart.Year,
            nextMonthStart.Month,
            DateTime.DaysInMonth(nextMonthStart.Year, nextMonthStart.Month));

        // Legacy line 690. DIVERGENCE: legacy dereferenced this FirstOrDefault() unguarded.
        var recoveryWageType = await _wageTypeRepository.GetAll()
            .Where(w => w.Code == LastMonthRecoveryWageTypeCode)
            .Select(w => new { w.Id, w.Code, w.NameEn, w.BillingType, w.PayrollOrder })
            .FirstOrDefaultAsync();

        if (recoveryWageType == null)
        {
            return new List<JoinedLine>();
        }

        // Legacy lines 691-692.
        var zeroNetRegisterIds = registerLines
            .Where(l => l.WageTypeCode == NetPayWageTypeCode && l.Amount == 0)
            .Select(l => l.SalaryRegisterId)
            .Distinct()
            .ToList();

        if (zeroNetRegisterIds.Count == 0)
        {
            return new List<JoinedLine>();
        }

        // Legacy lines 694-708: deductions are negated so the sum is a net movement.
        var recoveryAmounts = await _salaryRegisterDetailRepository.GetAll()
            .Where(d => zeroNetRegisterIds.Contains(d.SalaryRegisterId)
                        && !d.WageType.DontImpactNetPay
                        && (d.WageType.BillingType == BillingType.Earning
                            || d.WageType.BillingType == BillingType.Deduction))
            .WhereIf(rlsEmployeeIds.Count > 0, d => rlsEmployeeIds.Contains(d.SalaryRegister.EmployeeId))
            .Select(d => new
            {
                d.SalaryRegister.EmployeeId,
                EmployeeCode = d.SalaryRegister.Employee.Code,
                Amount = d.WageType.BillingType == BillingType.Deduction ? -d.Amount : d.Amount
            })
            .ToListAsync();

        // Legacy lines 710-724.
        var grouped = recoveryAmounts
            .GroupBy(a => new { a.EmployeeId, a.EmployeeCode })
            .Select(g => new RegisterLine
            {
                SalaryRegisterId = 0,
                ForPeriod = nextMonthEnd,
                EmployeeId = g.Key.EmployeeId,
                EmployeeCode = g.Key.EmployeeCode,
                WageTypeId = recoveryWageType.Id,
                WageTypeCode = recoveryWageType.Code,
                WageTypeName = recoveryWageType.NameEn,
                BillingType = recoveryWageType.BillingType,
                PayrollOrder = recoveryWageType.PayrollOrder,
                Amount = g.Sum(a => a.Amount)
            })
            .ToList();

        // Legacy lines 726-755: joined to the SAME wage-account mapping as the main set.
        return JoinWithMappings(grouped, mappingLookup);
    }

    /// <summary>Legacy lines 627-650 / 1189-1206: the mapping current as of the period end.</summary>
    private async Task<ILookup<(int EmployeeId, int WageTypeId), WageAccountRef>> LoadMappingLookupAsync(
        DateTime asOf,
        List<string> employeeCodes)
    {
        var mappings = await _employeeWageAccountRepository.GetAll()
            .Where(w => w.StartDate <= asOf && w.EndDate >= asOf)
            .WhereIf(employeeCodes.Count > 0, w => employeeCodes.Contains(w.Employee.Code))
            .Select(w => new WageAccountRef
            {
                EmployeeId = w.EmployeeId,
                WageTypeId = w.WageTypeId,
                AccountCode = w.AccountInfo.Code,
                AccountName = w.AccountInfo.NameEn,
                // Legacy slot names on this control (lines 645-646): CostElement1 is Department (the
                // Ops/Admin pivot key) and CostElement2 is Company.
                DepartmentCode = w.CostElement1 == null ? null : w.CostElement1.Code,
                DepartmentName = w.CostElement1 == null ? null : w.CostElement1.NameEn,
                CompanyName = w.CostElement2 == null ? null : w.CostElement2.NameEn,
                CostPercentage = w.CostPercentage
            })
            .ToListAsync();

        return mappings.ToLookup(m => (m.EmployeeId, m.WageTypeId));
    }

    /// <summary>Legacy's LEFT join idiom (lines 652-676): a line with no mapping survives with nulls.</summary>
    private static List<JoinedLine> JoinWithMappings(
        IEnumerable<RegisterLine> registerLines,
        ILookup<(int EmployeeId, int WageTypeId), WageAccountRef> mappingLookup)
    {
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
                    WageTypeId = line.WageTypeId,
                    WageTypeCode = line.WageTypeCode,
                    WageTypeName = line.WageTypeName,
                    BillingType = line.BillingType,
                    PayrollOrder = line.PayrollOrder,
                    Amount = line.Amount,
                    Mapping = mapping
                });
            }
        }

        return joined;
    }

    /// <summary>Legacy "Salary JV" branch (lines 763-1010).</summary>
    private static List<PayrollJVCustom030ReportRowDto> BuildSalaryJvRows(List<JoinedLine> lines)
    {
        // Legacy lines 801-826: the rolled-up pivot for ordinary wage types.
        var rolledUp = lines
            .Where(l => !IndividualWageTypeCodes.Contains(l.WageTypeCode))
            .GroupBy(l => new
            {
                l.WageTypeId,
                l.WageTypeName,
                l.BillingType,
                l.PayrollOrder,
                CompanyName = l.Mapping?.CompanyName,
                l.ForPeriod
            })
            .Select(g => new PivotRow
            {
                WageTypeName = g.Key.WageTypeName,
                BillingType = g.Key.BillingType,
                PayrollOrder = g.Key.PayrollOrder,
                Ops = SumForDepartment(g, OpsDepartmentCode),
                Admin = SumForDepartment(g, AdminDepartmentCode),
                Lowvo = g.Where(l => l.Mapping != null
                                     && (l.Mapping.DepartmentCode == OpsDepartmentCode
                                         || l.Mapping.DepartmentCode == AdminDepartmentCode))
                    .Sum(Allocated),
                OpsAccountName = g.Where(l => l.Mapping != null && l.Mapping.DepartmentCode == OpsDepartmentCode)
                    .Select(l => l.Mapping.AccountName)
                    .FirstOrDefault(),
                AdminAccountName = g
                    .Where(l => l.Mapping != null && l.Mapping.DepartmentCode == AdminDepartmentCode)
                    .Select(l => l.Mapping.AccountName)
                    .FirstOrDefault()
            })
            .Where(r => r.Lowvo != 0)
            .OrderBy(r => r.PayrollOrder)
            .ToList();

        // Legacy lines 828-866: the "individual" wage types keep one row per Department/Account, so Lowvo is
        // the group's own total rather than the Ops+Admin subtotal.
        var individual = lines
            .Where(l => IndividualWageTypeCodes.Contains(l.WageTypeCode))
            .GroupBy(l => new
            {
                l.WageTypeId,
                l.WageTypeName,
                l.BillingType,
                l.PayrollOrder,
                CompanyName = l.Mapping?.CompanyName,
                DepartmentCode = l.Mapping?.DepartmentCode,
                AccountName = l.Mapping?.AccountName,
                l.ForPeriod
            })
            .Select(g => new PivotRow
            {
                WageTypeName = g.Key.WageTypeName,
                BillingType = g.Key.BillingType,
                PayrollOrder = g.Key.PayrollOrder,
                Ops = g.Key.DepartmentCode == OpsDepartmentCode ? g.Sum(Allocated) : 0,
                Admin = g.Key.DepartmentCode == AdminDepartmentCode ? g.Sum(Allocated) : 0,
                Lowvo = g.Sum(Allocated),
                OpsAccountName = g.Key.DepartmentCode == OpsDepartmentCode ? g.Key.AccountName : null,
                AdminAccountName = g.Key.DepartmentCode == AdminDepartmentCode ? g.Key.AccountName : null
            })
            .Where(r => r.Lowvo != 0)
            .OrderBy(r => r.PayrollOrder)
            .ToList();

        // Legacy line 868: Union, which also de-duplicates identical rows.
        var pivot = rolledUp.Concat(individual).ToList();

        var rows = new List<PayrollJVCustom030ReportRowDto>();

        // Legacy lines 936-953.
        foreach (var record in pivot)
        {
            var row = NewRow(rows.Count + 1, SalaryJvColumns);

            row.Values[SjDrCr] = DrCrMarker;
            row.Values[SjWageType] = record.WageTypeName;
            row.Amounts[SjOps] = record.Ops;
            row.Amounts[SjAdmin] = record.Admin;
            row.Amounts[SjLowwo] = record.Lowvo;
            row.Values[SjOpsAccountDescription] = record.OpsAccountName ?? string.Empty;
            row.Values[SjAdminAccountDescription] = record.AdminAccountName ?? string.Empty;

            rows.Add(row);
        }

        // Legacy lines 955-1008: only when the pivot produced something.
        if (rows.Count > 0)
        {
            rows.Add(BuildNetPayRow(rows.Count + 1, lines));
        }

        return rows;
    }

    /// <summary>
    /// Legacy "NetPay Entry" (lines 955-1008), reproducing defects 1 and 2 of the class remarks: the
    /// per-employee net is bucketed Ops / Admin / Lowvo, negative nets are dropped, and only the first two
    /// buckets reach the row.
    /// </summary>
    private static PayrollJVCustom030ReportRowDto BuildNetPayRow(int serialNumber, List<JoinedLine> lines)
    {
        // Legacy lines 870-934: the same two pivots again, this time keyed per employee.
        var rolledUpEmp = lines
            .Where(l => !IndividualWageTypeCodes.Contains(l.WageTypeCode))
            .GroupBy(l => new { l.EmployeeId, l.EmployeeCode, l.WageTypeId, l.BillingType, l.ForPeriod })
            .Select(g => new EmployeePivotRow
            {
                EmployeeId = g.Key.EmployeeId,
                EmployeeCode = g.Key.EmployeeCode,
                BillingType = g.Key.BillingType,
                Ops = SumForDepartment(g, OpsDepartmentCode),
                Admin = SumForDepartment(g, AdminDepartmentCode),
                Lowvo = g.Where(l => l.Mapping != null
                                     && (l.Mapping.DepartmentCode == OpsDepartmentCode
                                         || l.Mapping.DepartmentCode == AdminDepartmentCode))
                    .Sum(Allocated)
            })
            .Where(r => r.Lowvo != 0)
            .ToList();

        var individualEmp = lines
            .Where(l => IndividualWageTypeCodes.Contains(l.WageTypeCode))
            .GroupBy(l => new
            {
                l.EmployeeId,
                l.EmployeeCode,
                l.WageTypeId,
                l.BillingType,
                DepartmentCode = l.Mapping?.DepartmentCode,
                l.ForPeriod
            })
            .Select(g => new EmployeePivotRow
            {
                EmployeeId = g.Key.EmployeeId,
                EmployeeCode = g.Key.EmployeeCode,
                BillingType = g.Key.BillingType,
                Ops = g.Key.DepartmentCode == OpsDepartmentCode ? g.Sum(Allocated) : 0,
                Admin = g.Key.DepartmentCode == AdminDepartmentCode ? g.Sum(Allocated) : 0,
                Lowvo = g.Sum(Allocated)
            })
            .Where(r => r.Lowvo != 0)
            .ToList();

        var allRows = rolledUpEmp.Concat(individualEmp);

        // Legacy lines 961-987.
        var employeeDepartmentSummary = allRows
            .GroupBy(r => new
            {
                r.EmployeeId,
                r.EmployeeCode,
                DepartmentType = r.Ops != 0 && r.Admin == 0
                    ? "Ops"
                    : r.Admin != 0 && r.Ops == 0
                        ? "Admin"
                        : "Lowvo"
            })
            .Select(g => new
            {
                g.Key.DepartmentType,
                Net = g.Where(r => r.BillingType == BillingType.Earning).Sum(r => (decimal)r.Lowvo)
                      - g.Where(r => r.BillingType == BillingType.Deduction).Sum(r => (decimal)r.Lowvo)
            })
            // LEGACY DEFECT 1 REPRODUCED (line 986).
            .Where(r => r.Net >= 0)
            .ToList();

        // LEGACY DEFECT 2 REPRODUCED (lines 991-992): the "Lowvo" bucket is never added in.
        var netOps = employeeDepartmentSummary.Where(r => r.DepartmentType == "Ops").Sum(r => r.Net);
        var netAdmin = employeeDepartmentSummary.Where(r => r.DepartmentType == "Admin").Sum(r => r.Net);

        var row = NewRow(serialNumber, SalaryJvColumns);

        row.Values[SjDrCr] = DrCrMarker;
        row.Values[SjWageType] = "Net Pay";
        row.Amounts[SjOps] = (double)netOps;
        row.Amounts[SjAdmin] = (double)netAdmin;
        row.Amounts[SjLowwo] = (double)(netOps + netAdmin);
        row.Values[SjOpsAccountDescription] = string.Empty;
        row.Values[SjAdminAccountDescription] = string.Empty;

        return row;
    }

    /// <summary>Legacy "Missing Accounts" branch (lines 1013-1073).</summary>
    private static List<PayrollJVCustom030ReportRowDto> BuildMissingAccountsRows(List<JoinedLine> lines)
    {
        var rows = new List<PayrollJVCustom030ReportRowDto>();

        foreach (var line in lines.Where(l => l.Mapping == null))
        {
            var row = NewRow(rows.Count + 1, MissingAccountsColumns);

            row.Values[MaEmpCode] = line.EmployeeCode;
            // Legacy typed this column DateTime; carried as an invariant ISO date here.
            row.Values[MaForPeriod] = line.ForPeriod.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            row.Values[MaWageType] = line.WageTypeName;

            row.Values[MaAccountInfoCode] = string.Empty;
            row.Values[MaAccountInfoName] = string.Empty;
            // Legacy lines 1062-1063 print the cost element's NAME here, not its code -- unlike the sibling
            // controls, which print the code on this same sheet.
            row.Values[MaDepartment] = string.Empty;
            row.Values[MaCompany] = string.Empty;

            row.Amounts[MaCostPercentage] = 0;
            row.Amounts[MaAmount] = line.Amount;

            rows.Add(row);
        }

        return rows;
    }

    /// <summary>Legacy "EmployeeWise BreakDown" branch (lines 1075-1141).</summary>
    private static List<PayrollJVCustom030ReportRowDto> BuildBreakDownRows(List<JoinedLine> lines)
    {
        // Legacy lines 1105-1126: EVERY billing type (the earning/non-9000 filter is commented out at line
        // 1106), grouped per employee, wage type, account and both cost elements.
        var groups = lines
            .GroupBy(l => new
            {
                l.EmployeeId,
                l.EmployeeCode,
                l.WageTypeId,
                AccountCode = l.Mapping?.AccountCode,
                AccountName = l.Mapping?.AccountName,
                DepartmentName = l.Mapping?.DepartmentName,
                CompanyName = l.Mapping?.CompanyName
            })
            .Select(g => new { g.Key, CostAmount = g.Sum(l => l.Amount) })
            .Where(g => g.CostAmount != 0)
            .ToList();

        var rows = new List<PayrollJVCustom030ReportRowDto>();

        foreach (var group in groups)
        {
            var row = NewRow(rows.Count + 1, BreakDownColumns);

            row.Values[BdEmployeeCode] = group.Key.EmployeeCode;
            row.Values[BdAccountInfoCode] = group.Key.AccountCode ?? string.Empty;
            row.Values[BdAccountInfoName] = group.Key.AccountName ?? string.Empty;
            row.Values[BdDepartment] = group.Key.DepartmentName ?? string.Empty;
            row.Values[BdCompany] = group.Key.CompanyName ?? string.Empty;

            // LEGACY DEFECT 3 REPRODUCED (line 1136).
            row.Amounts[BdAmount] = group.CostAmount >= 0 ? group.CostAmount : 0;

            rows.Add(row);
        }

        return rows;
    }

    /// <summary>
    /// Legacy "Indemnity JV" branch (lines 1143-1408): a four-line movement built from provision wage types
    /// 3000 (this month's charge) and 3003 (the closing balance), compared against last month's 3003.
    /// Ignores the whole filter panel -- see defect 4.
    /// </summary>
    private async Task<List<PayrollJVCustom030ReportRowDto>> BuildIndemnityRowsAsync(DateTime periodEnd)
    {
        var previousPeriodEnd = new DateTime(periodEnd.Year, periodEnd.Month, 1).AddDays(-1);

        var current = await LoadProvisionLinesAsync(periodEnd, regularRunOnly: false);
        var previous = await LoadProvisionLinesAsync(previousPeriodEnd, regularRunOnly: false);

        // Legacy lines 1320-1329.
        var currentCharge = current.Where(l => l.WageTypeCode == "3000").ToList();
        var currentClosing = current.Where(l => l.WageTypeCode == "3003").ToList();
        var previousClosing = previous.Where(l => l.WageTypeCode == "3003").ToList();

        var opsCurrent = SumAllocated(currentCharge, OpsDepartmentCode);
        var adminCurrent = SumAllocated(currentCharge, AdminDepartmentCode);

        // Legacy lines 1333-1343 guard with `if (wtPrev != null)`, which is ALWAYS true (it is a LINQ
        // sequence, never null), so the else branch is unreachable. Reproduced by taking the true branch
        // unconditionally.
        var opsPrevious = SumAllocated(previousClosing, OpsDepartmentCode);
        var adminPrevious = SumAllocated(previousClosing, AdminDepartmentCode);

        var opsClosing = SumAllocated(currentClosing, OpsDepartmentCode);
        var adminClosing = SumAllocated(currentClosing, AdminDepartmentCode);

        // Legacy lines 1349-1350.
        var opsDifference = opsPrevious + opsCurrent - opsClosing;
        var adminDifference = adminPrevious + adminCurrent - adminClosing;

        var rows = new List<PayrollJVCustom030ReportRowDto>();

        rows.Add(BuildBalanceRow(rows.Count + 1, "Previous Opening Balance", opsPrevious, adminPrevious));
        rows.Add(BuildBalanceRow(rows.Count + 1, "Current Month Balance", opsCurrent, adminCurrent));
        rows.Add(BuildBalanceRow(rows.Count + 1, "Closing Balance", opsClosing, adminClosing));
        rows.Add(BuildBalanceRow(rows.Count + 1, "Difference", opsDifference, adminDifference));

        return rows;
    }

    /// <summary>
    /// Legacy "Annual Leave JV" branch (lines 1410-1812): the same movement plus a "Less (QB Payroll
    /// Sheet)" line for leave actually paid (wage type 1003), built from provisions 3001 (charge) and 3004
    /// (closing). Ignores the whole filter panel -- see defect 4 -- and its forfeit calculation is dead code
    /// -- see defect 5.
    /// </summary>
    private async Task<List<PayrollJVCustom030ReportRowDto>> BuildAnnualLeaveRowsAsync(DateTime periodEnd)
    {
        var previousPeriodEnd = new DateTime(periodEnd.Year, periodEnd.Month, 1).AddDays(-1);

        // Legacy lines 1431-1438: the current set additionally requires a REGULAR run.
        var current = await LoadProvisionLinesAsync(periodEnd, regularRunOnly: true);
        var previous = await LoadProvisionLinesAsync(previousPeriodEnd, regularRunOnly: false);
        var paid = await LoadPaidLeaveLinesAsync(periodEnd);

        var currentCharge = current.Where(l => l.WageTypeCode == "3001").ToList();
        var currentClosing = current.Where(l => l.WageTypeCode == "3004").ToList();
        var previousClosing = previous.Where(l => l.WageTypeCode == "3004").ToList();
        var paidLeave = paid.Where(l => l.WageTypeCode == "1003").ToList();

        var opsCurrent = SumAllocated(currentCharge, OpsDepartmentCode);
        var adminCurrent = SumAllocated(currentCharge, AdminDepartmentCode);

        // Same always-true `wtPrev != null` guard as the Indemnity branch (lines 1712-1722).
        var opsPrevious = SumAllocated(previousClosing, OpsDepartmentCode);
        var adminPrevious = SumAllocated(previousClosing, AdminDepartmentCode);

        var opsPaid = SumAllocated(paidLeave, OpsDepartmentCode);
        var adminPaid = SumAllocated(paidLeave, AdminDepartmentCode);

        var opsClosing = SumAllocated(currentClosing, OpsDepartmentCode);
        var adminClosing = SumAllocated(currentClosing, AdminDepartmentCode);

        // Legacy lines 1732-1733.
        var opsDifference = opsPrevious + opsCurrent - opsClosing - opsPaid;
        var adminDifference = adminPrevious + adminCurrent - adminClosing - adminPaid;

        var rows = new List<PayrollJVCustom030ReportRowDto>();

        rows.Add(BuildBalanceRow(rows.Count + 1, "Previous Opening Balance", opsPrevious, adminPrevious));
        rows.Add(BuildBalanceRow(rows.Count + 1, "Current Month Balance", opsCurrent, adminCurrent));
        rows.Add(BuildBalanceRow(rows.Count + 1, "Less (QB Payroll Sheet)", opsPaid, adminPaid));
        rows.Add(BuildBalanceRow(rows.Count + 1, "Closing Balance", opsClosing, adminClosing));
        rows.Add(BuildBalanceRow(rows.Count + 1, "Difference", opsDifference, adminDifference));

        return rows;
    }

    /// <summary>
    /// Legacy lines 1157-1233 / 1431-1502: provision lines for one period, joined to that period's wage
    /// accounts. No payroll-area, employee or run-type filter -- reproduced.
    /// </summary>
    private async Task<List<JoinedLine>> LoadProvisionLinesAsync(DateTime periodEnd, bool regularRunOnly)
    {
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();

        var registerLines = await _salaryRegisterDetailRepository.GetAll()
            .Where(d => d.SalaryRegister.ForPeriod == periodEnd
                        && d.SalaryRegister.ForPeriod == d.SalaryRegister.InPeriod
                        && d.WageType.BillingType == BillingType.Provision)
            .WhereIf(regularRunOnly, d => d.SalaryRegister.PayrollRunType == PayrollRunType.Regular)
            // DELIBERATE DIVERGENCE: legacy applied no row level security here either.
            .WhereIf(rlsEmployeeIds.Count > 0, d => rlsEmployeeIds.Contains(d.SalaryRegister.EmployeeId))
            .Select(d => new RegisterLine
            {
                ForPeriod = d.SalaryRegister.ForPeriod,
                EmployeeId = d.SalaryRegister.EmployeeId,
                EmployeeCode = d.SalaryRegister.Employee.Code,
                WageTypeId = d.WageTypeId,
                WageTypeCode = d.WageType.Code,
                WageTypeName = d.WageType.NameEn,
                BillingType = d.WageType.BillingType,
                PayrollOrder = d.WageType.PayrollOrder,
                Amount = d.Amount
            })
            .ToListAsync();

        var mappingLookup = await LoadMappingLookupAsync(periodEnd, new List<string>());

        return JoinWithMappings(registerLines, mappingLookup);
    }

    /// <summary>
    /// Legacy lines 1583-1610 / 1670-1697: the Annual Leave sheet's "paid" side comes from EARNING wage
    /// types of a REGULAR run in the period, joined to the same wage accounts.
    /// </summary>
    private async Task<List<JoinedLine>> LoadPaidLeaveLinesAsync(DateTime periodEnd)
    {
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();

        var registerLines = await _salaryRegisterDetailRepository.GetAll()
            .Where(d => d.SalaryRegister.ForPeriod == periodEnd
                        && d.SalaryRegister.ForPeriod == d.SalaryRegister.InPeriod
                        && d.WageType.BillingType == BillingType.Earning
                        && d.SalaryRegister.PayrollRunType == PayrollRunType.Regular)
            .WhereIf(rlsEmployeeIds.Count > 0, d => rlsEmployeeIds.Contains(d.SalaryRegister.EmployeeId))
            .Select(d => new RegisterLine
            {
                ForPeriod = d.SalaryRegister.ForPeriod,
                EmployeeId = d.SalaryRegister.EmployeeId,
                EmployeeCode = d.SalaryRegister.Employee.Code,
                WageTypeId = d.WageTypeId,
                WageTypeCode = d.WageType.Code,
                WageTypeName = d.WageType.NameEn,
                BillingType = d.WageType.BillingType,
                PayrollOrder = d.WageType.PayrollOrder,
                Amount = d.Amount
            })
            .ToListAsync();

        var mappingLookup = await LoadMappingLookupAsync(periodEnd, new List<string>());

        return JoinWithMappings(registerLines, mappingLookup);
    }

    private static PayrollJVCustom030ReportRowDto BuildBalanceRow(
        int serialNumber,
        string description,
        double ops,
        double admin)
    {
        var row = NewRow(serialNumber, BalanceColumns);

        row.Values[BalDescription] = description;
        row.Amounts[SjOps] = ops;
        row.Amounts[SjAdmin] = admin;
        row.Amounts[SjLowwo] = ops + admin;

        return row;
    }

    /// <summary>Legacy's per-line allocation: <c>(Amount * CostPercentage) / 100</c>.</summary>
    private static double Allocated(JoinedLine line)
    {
        return line.Mapping == null ? 0 : line.Amount * line.Mapping.CostPercentage / 100;
    }

    private static double SumForDepartment(IEnumerable<JoinedLine> lines, string departmentCode)
    {
        return lines
            .Where(l => l.Mapping != null && l.Mapping.DepartmentCode == departmentCode)
            .Sum(Allocated);
    }

    private static double SumAllocated(IEnumerable<JoinedLine> lines, string departmentCode)
    {
        return SumForDepartment(lines, departmentCode);
    }

    private static PayrollJVCustom030ReportRowDto NewRow(
        int serialNumber,
        (string Name, bool IsNumeric)[] columns)
    {
        var row = new PayrollJVCustom030ReportRowDto
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

    private static (string Name, bool IsNumeric)[] GetColumnDefinitions(PayrollJVCustom030ReportType type)
    {
        return type switch
        {
            PayrollJVCustom030ReportType.MissingAccounts => MissingAccountsColumns,
            PayrollJVCustom030ReportType.EmployeeWiseBreakDown => BreakDownColumns,
            PayrollJVCustom030ReportType.IndemnityJV => BalanceColumns,
            PayrollJVCustom030ReportType.AnnualLeaveJV => BalanceColumns,
            _ => SalaryJvColumns
        };
    }

    /// <summary>
    /// Legacy <c>GetTable()</c>: Month/Year default to today's and resolve to the LAST day of that month;
    /// an unparseable pair fell back to 1900-01-31.
    /// </summary>
    private DateTime ResolvePeriodEnd(GetPayrollJVCustom030ReportInput input)
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
    /// Legacy <c>GetParameterStr</c>: Payroll Area, Run Type, Month, Year, Type, Employee Code.
    /// </summary>
    private string BuildJvParameterString(GetPayrollJVCustom030ReportInput input)
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
    /// <c>FromLiteralHeader</c> -- these captions are reconciled against the legacy file, not localization
    /// keys, so none of them can ever render as an unresolved [Key].
    /// </summary>
    private static IReadOnlyList<GridReportColumn<PayrollJVCustom030ReportRowDto>> GetExportColumns(
        PayrollJVCustom030ReportType type)
    {
        var columns = new List<GridReportColumn<PayrollJVCustom030ReportRowDto>>
        {
            GridReportColumn<PayrollJVCustom030ReportRowDto>.FromLiteralHeader("Oid", r => r.SerialNumber)
        };

        foreach (var (name, isNumeric) in GetColumnDefinitions(type))
        {
            var columnName = name;

            columns.Add(isNumeric
                ? GridReportColumn<PayrollJVCustom030ReportRowDto>.FromLiteralHeader(
                    columnName,
                    r => r.Amounts.TryGetValue(columnName, out var amount) ? amount : 0)
                : GridReportColumn<PayrollJVCustom030ReportRowDto>.FromLiteralHeader(
                    columnName,
                    r => r.Values.TryGetValue(columnName, out var value) ? value : string.Empty));
        }

        return columns;
    }

    private class RegisterLine
    {
        public int SalaryRegisterId { get; set; }

        public DateTime ForPeriod { get; set; }

        public int EmployeeId { get; set; }

        public string EmployeeCode { get; set; }

        public int WageTypeId { get; set; }

        public string WageTypeCode { get; set; }

        public string WageTypeName { get; set; }

        public BillingType BillingType { get; set; }

        public int PayrollOrder { get; set; }

        public double Amount { get; set; }
    }

    private class JoinedLine
    {
        public DateTime ForPeriod { get; set; }

        public int EmployeeId { get; set; }

        public string EmployeeCode { get; set; }

        public int WageTypeId { get; set; }

        public string WageTypeCode { get; set; }

        public string WageTypeName { get; set; }

        public BillingType BillingType { get; set; }

        public int PayrollOrder { get; set; }

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

        /// <summary>CostElement1: the Ops ("1") / Admin ("2") pivot key.</summary>
        public string DepartmentCode { get; set; }

        public string DepartmentName { get; set; }

        /// <summary>CostElement2.</summary>
        public string CompanyName { get; set; }

        public double CostPercentage { get; set; }
    }

    private class PivotRow
    {
        public string WageTypeName { get; set; }

        public BillingType BillingType { get; set; }

        public int PayrollOrder { get; set; }

        public double Ops { get; set; }

        public double Admin { get; set; }

        public double Lowvo { get; set; }

        public string OpsAccountName { get; set; }

        public string AdminAccountName { get; set; }
    }

    private class EmployeePivotRow
    {
        public int EmployeeId { get; set; }

        public string EmployeeCode { get; set; }

        public BillingType BillingType { get; set; }

        public double Ops { get; set; }

        public double Admin { get; set; }

        public double Lowvo { get; set; }
    }
}
