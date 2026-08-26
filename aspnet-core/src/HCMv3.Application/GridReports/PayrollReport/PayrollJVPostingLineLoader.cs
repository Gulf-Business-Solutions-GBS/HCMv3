using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// SHARED CORE of the "wage-account driven" Journal Voucher variants
/// (legacy <c>uscRptPayrollJV_Custom019</c> and <c>uscRptPayrollJV_Custom023</c>; the same block
/// reappears with three extra cost dimensions in <c>uscRptPayrollJV_Custom017</c>).
///
/// Those legacy controls open with an identical ~120-line block: load the month's
/// <c>SalaryRegisterDetail</c> rows, apply Run Type / Payroll Area / Employee Code, then LEFT JOIN each
/// line to the employee's <c>EmployeeWageAccount</c> row for that wage type to pick up the GL account,
/// the offset account and cost elements 1-4. Everything after that block differs per customer, so this
/// class is that block -- once -- rather than a base class: the per-customer services differ in their
/// row shapes and column sets, and composition keeps each service's constructor honest about what it
/// queries.
///
/// LEGACY DEFECT CARRIED THROUGH -- <see cref="PayrollJVPostingLine.CostPercentage"/> IS NEVER APPLIED
/// by the 019/023 callers. Legacy's join (019 lines 730-760, 023 lines 652-682) selects the raw
/// <c>sr.Amount</c>, not <c>sr.Amount * CostPercentage / 100</c> -- while the sibling Custom 017 report
/// DOES apply it (017 line 795). Two consequences, both reproduced and pinned by tests:
/// (a) an employee/wage type with several cost-split rows makes the join emit the salary line once per
///     split and the FULL amount is counted each time, inflating the voucher; and
/// (b) the cost percentage itself is ignored, so a 60/40 split posts 100/100.
/// This is an accounting defect, not a porting choice. Flagged for a customer decision.
/// </summary>
/// <summary>
/// Which wage types a JV variant admits. Custom 019/023 take Earning/Deduction plus an explicit list of
/// provision codes; Custom 017 takes EVERY billing type except Net Pay (legacy 017 line 645).
/// </summary>
public enum PayrollJVWageTypeScope
{
    EarningDeductionAndExtraCodes = 0,

    AllExceptNetPay = 1,

    /// <summary>
    /// Custom 042 (legacy <c>uscRptPayrollJV_Custom042.ascx.cs</c> lines 599-602): Earning, Deduction AND
    /// every Provision line, unconditionally -- unlike <see cref="EarningDeductionAndExtraCodes"/>, which
    /// only admits an explicit allow-list of provision wage-type CODES.
    /// </summary>
    EarningDeductionAndAllProvision = 2
}

public class PayrollJVPostingLineLoader : ITransientDependency
{
    /// <summary>The Net Pay wage type, singled out by every JV variant.</summary>
    public const string NetPayWageTypeCode = "9000";

    private readonly IRepository<SalaryRegisterDetail> _salaryRegisterDetailRepository;
    private readonly IRepository<EmployeeWageAccount> _employeeWageAccountRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;

    public PayrollJVPostingLineLoader(
        IRepository<SalaryRegisterDetail> salaryRegisterDetailRepository,
        IRepository<EmployeeWageAccount> employeeWageAccountRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository)
    {
        _salaryRegisterDetailRepository = salaryRegisterDetailRepository;
        _employeeWageAccountRepository = employeeWageAccountRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
    }

    /// <summary>
    /// The month's salary-register lines, filtered and left-joined to their wage accounts.
    /// </summary>
    /// <param name="endDate">Month end; legacy matched <c>ForPeriod == EndDate</c> exactly.</param>
    /// <param name="extraProvisionWageTypeCodes">
    /// Wage-type codes admitted IN ADDITION to Earning/Deduction billing types. Custom 019 passes
    /// {3001, 3000}; Custom 023 passes none.
    /// </param>
    /// <param name="payrollRunType">Legacy <c>cmbRunType</c>; null means every run type.</param>
    /// <param name="payrollAreaIds">
    /// Payroll areas the employee's org assignment must point at as of <paramref name="endDate"/>.
    /// Empty means "do not filter". Custom 019 resolves a 0/1 toggle into this list; 023 resolves one id.
    /// </param>
    /// <param name="employeeCodes">Legacy comma-split <c>txtEmpCode</c>; empty means "do not filter".</param>
    /// <param name="rlsEmployeeIds">Row-level-security allow-list; empty means "no restriction".</param>
    public async Task<List<PayrollJVPostingLine>> LoadAsync(
        DateTime endDate,
        IReadOnlyCollection<string> extraProvisionWageTypeCodes,
        PayrollRunType? payrollRunType,
        IReadOnlyCollection<int> payrollAreaIds,
        IReadOnlyCollection<string> employeeCodes,
        IReadOnlyCollection<int> rlsEmployeeIds,
        PayrollJVWageTypeScope wageTypeScope = PayrollJVWageTypeScope.EarningDeductionAndExtraCodes)
    {
        var details = await LoadSalaryLinesAsync(
            endDate, extraProvisionWageTypeCodes, payrollRunType, payrollAreaIds, employeeCodes, rlsEmployeeIds,
            wageTypeScope);

        var lookup = await LoadWageAccountLookupAsync(endDate, employeeCodes);

        return Join(details, lookup);
    }

    /// <summary>
    /// The filtered salary-register lines WITHOUT the wage-account join. Custom 019 needs these on their
    /// own so it can spot the registers whose Net Pay came out as zero (legacy 019 lines 646-655).
    /// </summary>
    public async Task<List<PayrollJVSalaryLine>> LoadSalaryLinesAsync(
        DateTime endDate,
        IReadOnlyCollection<string> extraProvisionWageTypeCodes,
        PayrollRunType? payrollRunType,
        IReadOnlyCollection<int> payrollAreaIds,
        IReadOnlyCollection<string> employeeCodes,
        IReadOnlyCollection<int> rlsEmployeeIds,
        PayrollJVWageTypeScope wageTypeScope = PayrollJVWageTypeScope.EarningDeductionAndExtraCodes)
    {
        var provisionCodes = (extraProvisionWageTypeCodes ?? Array.Empty<string>()).ToArray();
        var areaIds = (payrollAreaIds ?? Array.Empty<int>()).ToArray();
        var codes = (employeeCodes ?? Array.Empty<string>()).ToArray();
        var rlsIds = (rlsEmployeeIds ?? Array.Empty<int>()).ToArray();

        // Legacy 023 lines 562-570 / 019 lines 568-578.
        return await _salaryRegisterDetailRepository.GetAll()
            .Where(d => d.SalaryRegister.ForPeriod == endDate
                        && d.SalaryRegister.ForPeriod == d.SalaryRegister.InPeriod
                        && d.WageType.IncludeInFIPosting
                        && (wageTypeScope == PayrollJVWageTypeScope.AllExceptNetPay
                            ? d.WageType.Code != NetPayWageTypeCode
                            : wageTypeScope == PayrollJVWageTypeScope.EarningDeductionAndAllProvision
                            ? d.WageType.BillingType == BillingType.Earning
                              || d.WageType.BillingType == BillingType.Deduction
                              || d.WageType.BillingType == BillingType.Provision
                            : d.WageType.BillingType == BillingType.Earning
                              || d.WageType.BillingType == BillingType.Deduction
                              || provisionCodes.Contains(d.WageType.Code)))
            // Legacy 023 lines 576-583 / 019 lines 581-588.
            .WhereIf(payrollRunType.HasValue,
                d => d.SalaryRegister.PayrollRunType == payrollRunType.Value)
            // Legacy 023 lines 585-596 / 019 lines 590-614.
            .WhereIf(areaIds.Length > 0, d => _employeeOrgAssignmentRepository.GetAll().Any(
                o => o.EmployeeId == d.SalaryRegister.EmployeeId
                     && o.StartDate <= endDate
                     && o.EndDate >= endDate
                     && o.PayrollAreaId != null
                     && areaIds.Contains(o.PayrollAreaId.Value)))
            // Legacy 023 lines 598-604 / 019 lines 616-622.
            .WhereIf(codes.Length > 0, d => codes.Contains(d.SalaryRegister.Employee.Code))
            // DIVERGENCE (agreed for this wave): legacy never applied RLS to any JV variant. A journal
            // voucher aggregates salary, so it is applied here. Empty list still means "no restriction".
            .WhereIf(rlsIds.Length > 0, d => rlsIds.Contains(d.SalaryRegister.EmployeeId))
            .Select(d => new PayrollJVSalaryLine
            {
                SalaryRegisterDetailId = d.Id,
                SalaryRegisterId = d.SalaryRegisterId,
                ForPeriod = d.SalaryRegister.ForPeriod,
                EmployeeId = d.SalaryRegister.EmployeeId,
                EmployeeCode = d.SalaryRegister.Employee.Code,
                EmployeeName = d.SalaryRegister.Employee.FullNameEn,
                WageTypeId = d.WageTypeId,
                WageTypeCode = d.WageType.Code,
                WageTypeName = d.WageType.NameEn,
                BillingType = d.WageType.BillingType,
                DontImpactNetPay = d.WageType.DontImpactNetPay,
                PayrollOrder = d.WageType.PayrollOrder,
                Amount = d.Amount
            })
            .ToListAsync();
    }

    /// <summary>
    /// Legacy 023 lines 630-650 / 019 lines 712-728: the wage accounts current as of the month end,
    /// narrowed by the SAME employee-code box as the salary register.
    /// </summary>
    public async Task<PayrollJVWageAccountLookup> LoadWageAccountLookupAsync(
        DateTime endDate,
        IReadOnlyCollection<string> employeeCodes)
    {
        var codes = (employeeCodes ?? Array.Empty<string>()).ToArray();

        var accounts = await _employeeWageAccountRepository.GetAll()
            .Where(a => a.StartDate <= endDate && a.EndDate >= endDate)
            .WhereIf(codes.Length > 0, a => codes.Contains(a.Employee.Code))
            .Select(a => new PayrollJVWageAccountInfo
            {
                EmployeeId = a.EmployeeId,
                WageTypeId = a.WageTypeId,
                AccountInfoId = a.AccountInfoId,
                AccountInfoCode = a.AccountInfo.Code,
                AccountInfoName = a.AccountInfo.NameEn,
                OffsetAccountInfoId = a.OffsetAccountInfoId,
                OffsetAccountInfoCode = a.OffsetAccountInfo == null ? null : a.OffsetAccountInfo.Code,
                OffsetAccountInfoName = a.OffsetAccountInfo == null ? null : a.OffsetAccountInfo.NameEn,
                CostElement1Id = a.CostElement1Id,
                CostElement1Code = a.CostElement1 == null ? null : a.CostElement1.Code,
                CostElement2Id = a.CostElement2Id,
                CostElement2Code = a.CostElement2 == null ? null : a.CostElement2.Code,
                CostElement3Id = a.CostElement3Id,
                CostElement3Code = a.CostElement3 == null ? null : a.CostElement3.Code,
                CostElement4Id = a.CostElement4Id,
                CostElement4Code = a.CostElement4 == null ? null : a.CostElement4.Code,
                CostElement5Id = a.CostElement5Id,
                CostElement5Code = a.CostElement5 == null ? null : a.CostElement5.Code,
                CostElement6Id = a.CostElement6Id,
                CostElement6Code = a.CostElement6 == null ? null : a.CostElement6.Code,
                CostElement7Id = a.CostElement7Id,
                CostElement7Code = a.CostElement7 == null ? null : a.CostElement7.Code,
                AccountTypeName = a.AccountInfo.AccountType == null ? null : a.AccountInfo.AccountType.NameEn,
                OffsetAccountTypeName = a.OffsetAccountInfo == null || a.OffsetAccountInfo.AccountType == null
                    ? null
                    : a.OffsetAccountInfo.AccountType.NameEn,
                MergeOffsetAccount = a.MergeOffsetAccount,
                CostPercentage = a.CostPercentage
            })
            .ToListAsync();

        return new PayrollJVWageAccountLookup(accounts);
    }

    /// <summary>
    /// Legacy's LEFT JOIN on (Employee, WageType) -- 023 lines 652-682 / 019 lines 730-760. An unmapped
    /// wage type still produces a line, with a null account: that is exactly what the "Missing Accounts"
    /// format lists. A wage type with SEVERAL cost splits produces one line per split, each carrying the
    /// FULL amount -- the defect described in the class remarks.
    /// </summary>
    public List<PayrollJVPostingLine> Join(
        IEnumerable<PayrollJVSalaryLine> salaryLines,
        PayrollJVWageAccountLookup lookup)
    {
        var lines = new List<PayrollJVPostingLine>();

        foreach (var salaryLine in salaryLines)
        {
            foreach (var account in lookup.MatchOrNull(salaryLine.EmployeeId, salaryLine.WageTypeId))
            {
                lines.Add(new PayrollJVPostingLine
                {
                    SalaryRegisterDetailId = salaryLine.SalaryRegisterDetailId,
                    SalaryRegisterId = salaryLine.SalaryRegisterId,
                    ForPeriod = salaryLine.ForPeriod,
                    EmployeeId = salaryLine.EmployeeId,
                    EmployeeCode = salaryLine.EmployeeCode,
                    EmployeeName = salaryLine.EmployeeName,
                    WageTypeId = salaryLine.WageTypeId,
                    WageTypeCode = salaryLine.WageTypeCode,
                    WageTypeName = salaryLine.WageTypeName,
                    BillingType = salaryLine.BillingType,
                    DontImpactNetPay = salaryLine.DontImpactNetPay,
                    PayrollOrder = salaryLine.PayrollOrder,
                    AccountInfoId = account?.AccountInfoId,
                    AccountInfoCode = account?.AccountInfoCode,
                    AccountInfoName = account?.AccountInfoName,
                    OffsetAccountInfoId = account?.OffsetAccountInfoId,
                    OffsetAccountInfoCode = account?.OffsetAccountInfoCode,
                    OffsetAccountInfoName = account?.OffsetAccountInfoName,
                    CostElement1Id = account?.CostElement1Id,
                    CostElement1Code = account?.CostElement1Code,
                    CostElement2Id = account?.CostElement2Id,
                    CostElement2Code = account?.CostElement2Code,
                    CostElement3Id = account?.CostElement3Id,
                    CostElement3Code = account?.CostElement3Code,
                    CostElement4Id = account?.CostElement4Id,
                    CostElement4Code = account?.CostElement4Code,
                    CostElement5Id = account?.CostElement5Id,
                    CostElement5Code = account?.CostElement5Code,
                    CostElement6Id = account?.CostElement6Id,
                    CostElement6Code = account?.CostElement6Code,
                    CostElement7Id = account?.CostElement7Id,
                    CostElement7Code = account?.CostElement7Code,
                    AccountTypeName = account?.AccountTypeName,
                    OffsetAccountTypeName = account?.OffsetAccountTypeName,
                    MergeOffsetAccount = account?.MergeOffsetAccount ?? false,
                    CostPercentage = account?.CostPercentage ?? 0,
                    // NOT multiplied by CostPercentage -- see the class remarks (legacy defect).
                    Amount = salaryLine.Amount
                });
            }
        }

        return lines;
    }
}

/// <summary>In-memory (Employee, WageType) -> wage-account index backing the legacy LEFT JOIN.</summary>
public class PayrollJVWageAccountLookup
{
    private static readonly PayrollJVWageAccountInfo[] NoMatch = { null };

    private readonly Dictionary<(int EmployeeId, int WageTypeId), List<PayrollJVWageAccountInfo>> _byKey;

    public PayrollJVWageAccountLookup(IEnumerable<PayrollJVWageAccountInfo> accounts)
    {
        _byKey = accounts
            .GroupBy(a => (a.EmployeeId, a.WageTypeId))
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    /// <summary>
    /// The wage accounts for the pair, or a single <c>null</c> element -- LINQ's
    /// <c>DefaultIfEmpty()</c>, which is what makes legacy's join a LEFT join.
    /// </summary>
    public IEnumerable<PayrollJVWageAccountInfo> MatchOrNull(int employeeId, int wageTypeId)
    {
        return _byKey.TryGetValue((employeeId, wageTypeId), out var matches) && matches.Count > 0
            ? matches
            : NoMatch;
    }
}

/// <summary>One <c>EmployeeWageAccount</c> row, flattened to the fields the JV reports read.</summary>
public class PayrollJVWageAccountInfo
{
    public int EmployeeId { get; set; }

    public int WageTypeId { get; set; }

    public int? AccountInfoId { get; set; }

    public string AccountInfoCode { get; set; }

    public string AccountInfoName { get; set; }

    public int? OffsetAccountInfoId { get; set; }

    public string OffsetAccountInfoCode { get; set; }

    public string OffsetAccountInfoName { get; set; }

    public int? CostElement1Id { get; set; }

    public string CostElement1Code { get; set; }

    public int? CostElement2Id { get; set; }

    public string CostElement2Code { get; set; }

    public int? CostElement3Id { get; set; }

    public string CostElement3Code { get; set; }

    public int? CostElement4Id { get; set; }

    public string CostElement4Code { get; set; }

    /// <summary>Legacy 017 "LedgerDimension.Employees".</summary>
    public int? CostElement5Id { get; set; }

    public string CostElement5Code { get; set; }

    /// <summary>Legacy 017 "DefaultDimension.Employees".</summary>
    public int? CostElement6Id { get; set; }

    public string CostElement6Code { get; set; }

    /// <summary>Legacy 017 "DefaultDimension.Location".</summary>
    public int? CostElement7Id { get; set; }

    public string CostElement7Code { get; set; }

    /// <summary>Legacy 017 "Line number.Account type".</summary>
    public string AccountTypeName { get; set; }

    /// <summary>Legacy 017 "Offset account type".</summary>
    public string OffsetAccountTypeName { get; set; }

    /// <summary>Legacy 017: collapses several offset postings onto one merged row.</summary>
    public bool MergeOffsetAccount { get; set; }

    public double CostPercentage { get; set; }
}

/// <summary>
/// One filtered salary-register line before the wage-account join -- legacy's first
/// <c>dsSalaryRegister</c> projection.
/// </summary>
public class PayrollJVSalaryLine
{
    public int SalaryRegisterDetailId { get; set; }

    public int SalaryRegisterId { get; set; }

    public DateTime ForPeriod { get; set; }

    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    public int WageTypeId { get; set; }

    public string WageTypeCode { get; set; }

    public string WageTypeName { get; set; }

    public BillingType BillingType { get; set; }

    public bool DontImpactNetPay { get; set; }

    public int PayrollOrder { get; set; }

    public double Amount { get; set; }
}

/// <summary>
/// One salary-register line joined to its wage-account mapping -- the flattened equivalent of legacy's
/// <c>dsSalaryRegister2</c> anonymous type.
/// </summary>
public class PayrollJVPostingLine
{
    public int SalaryRegisterDetailId { get; set; }

    public int SalaryRegisterId { get; set; }

    public DateTime ForPeriod { get; set; }

    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    public int WageTypeId { get; set; }

    public string WageTypeCode { get; set; }

    public string WageTypeName { get; set; }

    public BillingType BillingType { get; set; }

    public bool DontImpactNetPay { get; set; }

    public int PayrollOrder { get; set; }

    /// <summary>Null when the employee/wage type has no <c>EmployeeWageAccount</c> for the month.</summary>
    public int? AccountInfoId { get; set; }

    public string AccountInfoCode { get; set; }

    public string AccountInfoName { get; set; }

    public int? OffsetAccountInfoId { get; set; }

    public string OffsetAccountInfoCode { get; set; }

    public string OffsetAccountInfoName { get; set; }

    /// <summary>Legacy "Line of Business".</summary>
    public int? CostElement1Id { get; set; }

    public string CostElement1Code { get; set; }

    /// <summary>Legacy "Vehicle/Machinery".</summary>
    public int? CostElement2Id { get; set; }

    public string CostElement2Code { get; set; }

    /// <summary>Legacy "Departments".</summary>
    public int? CostElement3Id { get; set; }

    public string CostElement3Code { get; set; }

    /// <summary>Legacy "Project".</summary>
    public int? CostElement4Id { get; set; }

    public string CostElement4Code { get; set; }

    /// <summary>Legacy 017 "LedgerDimension.Employees".</summary>
    public int? CostElement5Id { get; set; }

    public string CostElement5Code { get; set; }

    /// <summary>Legacy 017 "DefaultDimension.Employees".</summary>
    public int? CostElement6Id { get; set; }

    public string CostElement6Code { get; set; }

    /// <summary>Legacy 017 "DefaultDimension.Location".</summary>
    public int? CostElement7Id { get; set; }

    public string CostElement7Code { get; set; }

    /// <summary>Legacy 017 "Line number.Account type".</summary>
    public string AccountTypeName { get; set; }

    /// <summary>Legacy 017 "Offset account type".</summary>
    public string OffsetAccountTypeName { get; set; }

    /// <summary>Legacy 017: collapses several offset postings onto one merged row.</summary>
    public bool MergeOffsetAccount { get; set; }

    /// <summary>Carried for the "Missing Accounts" listing only -- never applied to <see cref="Amount"/>.</summary>
    public double CostPercentage { get; set; }

    public double Amount { get; set; }
}
