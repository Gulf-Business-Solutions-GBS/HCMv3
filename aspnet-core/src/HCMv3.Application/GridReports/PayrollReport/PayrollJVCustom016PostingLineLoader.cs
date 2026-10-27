using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.OrganizationManagement;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// SHARED CORE of the legacy <c>uscRptPayrollJV_Custom016</c> screen's common preprocessing pipeline
/// (<c>Table1()</c>, legacy lines 573-1168) -- the block every LIVE <c>cmbType</c> format branch except
/// "NetPay Difference" (needs an extra embedded SQL comparison, not ported) and "Indemnity Pay Adjust"
/// (its own bespoke query, not ported) reads from before applying its own final grouping/DataTable shape.
///
/// This wave only PORTS the "Loan Format" branch
/// (<see cref="PayrollJVCustom016LoanFormatBmcReportAppService"/> /
/// <see cref="PayrollJVCustom016LoanFormatMybReportAppService"/>), so only the preprocessing steps that
/// can affect a Loan-Format row are exercised by tests -- but the pipeline itself is built faithfully to
/// legacy end-to-end (not scoped down to Loan-only), so a future wave porting the remaining ~9 format
/// variants ("Main Format", "Main Format-New", "Detail", "Leave Provision", "Indemnity Provision",
/// "Prov Difference-ALL/-Active/-Inactive") can reuse it directly.
///
/// STEPS (legacy line ranges):
/// <list type="number">
/// <item><description>Load <c>SalaryRegisterDetail</c> for the month, Run Type, Payroll Area, Employee
/// Code (573-633).</description></item>
/// <item><description>LEFT JOIN to <c>EmployeeWageAccount</c> by (Employee, WageType); CostCenter comes
/// from the employee's <c>EmployeeOrgAssignment</c> at month end, overridden by the wage account's
/// CostElement1 code when present (674-708).</description></item>
/// <item><description>"If AdvancePay Then Remove Arrears": Regular-run arrear/overtime lines of an
/// employee whose register also has an Advance-Payment deduction line are re-pointed at wage type 1000
/// (Basic Pay) and its wage account; a synthetic "AnnualLeavePayArrear" duplicate is unioned in for wage
/// type 7003 under the same condition (710-859).</description></item>
/// <item><description>"Filter OffCycle Employees in Regular Registers": non-Provision lines of a Regular
/// register are zeroed for employees who ALSO have a non-"End Of Service" OffCycle register that period
/// (861-902).</description></item>
/// <item><description>CostCenter resolution + CostAmount = Round(CostPercentage * Amount / 100, 3)
/// (905-948).</description></item>
/// <item><description>FullMonthLeave / LeaveEnd: Regular-run Earning/Deduction lines of an employee on a
/// full-month advance-paid leave (or with an OffCycle register that period) are zeroed; wage types 1003 /
/// 2005 / 8005 are additionally zeroed for an employee whose leave ended in the period, and the
/// "AnnualLeavePayArrear" synthetic line is negated instead of zeroed (950-1136).</description></item>
/// <item><description>Wage type 8005 (Advance Payment Ded Arrear) is dropped outright; then any line with
/// no resolved CostCenter or AccountInfo is dropped (1141-1168).</description></item>
/// </list>
///
/// DIVERGENCE (agreed for this wave, matching the rest of the JV family): RLS IS applied here even though
/// legacy never applied it to any JV variant -- see <c>PayrollJVPostingLineLoader</c> remarks for the
/// reasoning; a journal voucher aggregates salary.
///
/// SIMPLIFICATION: legacy's <c>dsSalaryRegister2.Union(dsAnnualLeavePayArrear)</c> is LINQ's structural-
/// equality set union over anonymous types. This port uses a plain concatenation instead: the unioned
/// rows carry a distinct <c>Comment</c> tag ("AnnualLeavePayArrear") that no other row carries, so an
/// accidental structural collision that <c>Union</c> would have deduplicated is not realistically
/// reachable, and Concat is observably identical for every case exercised by tests.
/// </summary>
public class PayrollJVCustom016PostingLineLoader : ITransientDependency
{
    /// <summary>Legacy line 729: the wage type arrear/overtime lines are re-pointed at.</summary>
    public const string BasicPayWageTypeCode = "1000";

    private static readonly string[] AdvancePayWageTypeCodes = { "2005", "8005" };

    private static readonly string[] SuppressIfAdvancePayWageTypeCodes =
        { "7007", "7008", "7009", "7042", "7052", "7003", "8005" };

    private static readonly string[] SuppressIfLeaveEndWageTypeCodes = { "1003", "2005", "8005" };

    private const string AnnualLeavePayArrearWageTypeCode = "7003";

    private const string AdvancePayDedArrearWageTypeCode = "8005";

    private const string EndOfServiceReasonNameEn = "End Of Service";

    private readonly IRepository<SalaryRegisterDetail> _salaryRegisterDetailRepository;
    private readonly IRepository<SalaryRegister> _salaryRegisterRepository;
    private readonly IRepository<EmployeeWageAccount> _employeeWageAccountRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly IRepository<CostCenter> _costCenterRepository;
    private readonly IRepository<EmployeeAbsence> _employeeAbsenceRepository;
    private readonly IRepository<WageType> _wageTypeRepository;

    public PayrollJVCustom016PostingLineLoader(
        IRepository<SalaryRegisterDetail> salaryRegisterDetailRepository,
        IRepository<SalaryRegister> salaryRegisterRepository,
        IRepository<EmployeeWageAccount> employeeWageAccountRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        IRepository<CostCenter> costCenterRepository,
        IRepository<EmployeeAbsence> employeeAbsenceRepository,
        IRepository<WageType> wageTypeRepository)
    {
        _salaryRegisterDetailRepository = salaryRegisterDetailRepository;
        _salaryRegisterRepository = salaryRegisterRepository;
        _employeeWageAccountRepository = employeeWageAccountRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _costCenterRepository = costCenterRepository;
        _employeeAbsenceRepository = employeeAbsenceRepository;
        _wageTypeRepository = wageTypeRepository;
    }

    /// <summary>
    /// The whole legacy <c>Table1()</c> common-preprocessing pipeline, up to and including "Filter Blank
    /// CostCenter, AccountInfo" (legacy line 1168). <paramref name="payrollAreaId"/> is a SINGLE payroll
    /// area id (legacy's <c>cmbPayrollArea</c> is a full picker, not a toggle) -- callers that split BMC
    /// and MYB into separate reports resolve the id by literal Name before calling in.
    /// </summary>
    public async Task<List<PayrollJVCustom016Line>> LoadAsync(
        DateTime endDate,
        int payrollAreaId,
        PayrollRunType? payrollRunType,
        IReadOnlyCollection<string> employeeCodes,
        IReadOnlyCollection<int> rlsEmployeeIds)
    {
        var codes = (employeeCodes ?? Array.Empty<string>()).ToArray();
        var rlsIds = (rlsEmployeeIds ?? Array.Empty<int>()).ToArray();

        // Legacy lines 598-633.
        var salaryLines = await _salaryRegisterDetailRepository.GetAll()
            .Where(d => d.SalaryRegister.ForPeriod == endDate
                        && d.SalaryRegister.ForPeriod == d.SalaryRegister.InPeriod
                        && d.WageType.IncludeInFIPosting)
            .WhereIf(codes.Length > 0, d => codes.Contains(d.SalaryRegister.Employee.Code))
            .Where(d => _employeeOrgAssignmentRepository.GetAll().Any(
                o => o.EmployeeId == d.SalaryRegister.EmployeeId
                     && o.StartDate <= endDate
                     && o.EndDate >= endDate
                     && o.PayrollAreaId == payrollAreaId))
            .WhereIf(payrollRunType.HasValue, d => d.SalaryRegister.PayrollRunType == payrollRunType.Value)
            // DIVERGENCE (agreed for this wave): see class remarks -- RLS applied although legacy never did.
            .WhereIf(rlsIds.Length > 0, d => rlsIds.Contains(d.SalaryRegister.EmployeeId))
            .Select(d => new RawLine
            {
                SalaryRegisterId = d.SalaryRegisterId,
                EmployeeId = d.SalaryRegister.EmployeeId,
                EmployeeCode = d.SalaryRegister.Employee.Code,
                EmployeeName = d.SalaryRegister.Employee.FullNameEn,
                EmployeeReferenceCode = d.SalaryRegister.Employee.ReferenceCode,
                PayrollRunType = d.SalaryRegister.PayrollRunType,
                PayrollReasonTypeNameEn = d.SalaryRegister.PayrollReasonType == null
                    ? null
                    : d.SalaryRegister.PayrollReasonType.NameEn,
                WageTypeId = d.WageTypeId,
                WageTypeCode = d.WageType.Code,
                WageTypeName = d.WageType.NameEn,
                BillingType = d.WageType.BillingType,
                ArrearSourceWageTypeName = d.WageType.ArrearSourceWageType == null
                    ? null
                    : d.WageType.ArrearSourceWageType.NameEn,
                Amount = d.Amount
            })
            .ToListAsync();

        if (salaryLines.Count == 0)
        {
            return new List<PayrollJVCustom016Line>();
        }

        // Legacy line 677: EmployeeWageAccount current as of month end, NOT narrowed by employee code.
        var wageAccounts = await _employeeWageAccountRepository.GetAll()
            .Where(a => a.StartDate <= endDate && a.EndDate >= endDate)
            .Select(a => new
            {
                a.EmployeeId,
                a.WageTypeId,
                a.AccountInfoId,
                AccountInfoCode = a.AccountInfo.Code,
                AccountInfoName = a.AccountInfo.NameEn,
                a.OffsetAccountInfoId,
                OffsetAccountInfoCode = a.OffsetAccountInfo == null ? null : a.OffsetAccountInfo.Code,
                OffsetAccountInfoName = a.OffsetAccountInfo == null ? null : a.OffsetAccountInfo.NameEn,
                CostElement1Code = a.CostElement1 == null ? null : a.CostElement1.Code,
                a.CostPercentage
            })
            .ToListAsync();
        var wageAccountLookup = wageAccounts
            .GroupBy(a => (a.EmployeeId, a.WageTypeId))
            .ToDictionary(g => g.Key, g => g.First());

        // Legacy line 696-702: the employee's OrgAssignment CostCenter code as of month end.
        var employeeIds = salaryLines.Select(l => l.EmployeeId).Distinct().ToList();
        var employeeCostCenterCodes = await _employeeOrgAssignmentRepository.GetAll()
            .Where(o => employeeIds.Contains(o.EmployeeId) && o.StartDate <= endDate && o.EndDate >= endDate)
            .Select(o => new { o.EmployeeId, Code = o.CostCenter == null ? null : o.CostCenter.Code })
            .ToListAsync();
        var employeeCostCenterCodeLookup = employeeCostCenterCodes
            .GroupBy(o => o.EmployeeId)
            .ToDictionary(g => g.Key, g => g.First().Code);

        // Legacy lines 729/793-798: the Basic Pay wage type and its own wage-account rows, keyed by
        // (Employee, CostPercentage) so the arrear substitution can find the matching split.
        var basicPayWageType = await _wageTypeRepository.GetAll()
            .Where(w => w.Code == BasicPayWageTypeCode)
            .Select(w => new { w.Id, w.Code, w.NameEn, w.BillingType, ArrearSourceWageTypeName = (string)null })
            .FirstOrDefaultAsync();
        var basicPayWageAccountLookup = basicPayWageType == null
            ? new Dictionary<(int, double), (int? Id, string Code, string Name)>()
            : wageAccounts
                .Where(a => a.WageTypeId == basicPayWageType.Id)
                .GroupBy(a => (a.EmployeeId, a.CostPercentage))
                .ToDictionary(
                    g => g.Key,
                    g => (Id: (int?)g.First().AccountInfoId, Code: g.First().AccountInfoCode, Name: g.First().AccountInfoName));

        // Legacy line 763: registers that contain at least one Advance-Payment deduction line -- read
        // across ALL details of the register, not just the ones matched by the outer filter.
        var registerIds = salaryLines.Select(l => l.SalaryRegisterId).Distinct().ToList();
        var registersWithAdvancePay = (await _salaryRegisterDetailRepository.GetAll()
                .Where(d => registerIds.Contains(d.SalaryRegisterId)
                            && AdvancePayWageTypeCodes.Contains(d.WageType.Code))
                .Select(d => d.SalaryRegisterId)
                .ToListAsync())
            .ToHashSet();

        // Legacy lines 861-868: employees with an OffCycle, non-"End Of Service" register that period --
        // an INDEPENDENT query, not scoped by the outer filters.
        var offCycleEmployeeCodes = (await _salaryRegisterRepository.GetAll()
                .Where(r => r.ForPeriod == endDate
                            && r.ForPeriod == r.InPeriod
                            && r.PayrollRunType == PayrollRunType.OffCycle
                            && r.PayrollReasonType != null
                            && r.PayrollReasonType.NameEn != EndOfServiceReasonNameEn)
                .Select(r => r.Employee.Code)
                .Distinct()
                .ToListAsync())
            .ToHashSet();

        // ---- Legacy lines 674-708: LEFT JOIN + CostCenter resolution. ----
        var joined = salaryLines.Select(l =>
        {
            wageAccountLookup.TryGetValue((l.EmployeeId, l.WageTypeId), out var account);
            employeeCostCenterCodeLookup.TryGetValue(l.EmployeeId, out var orgCostCenterCode);
            var costElementCode = account?.CostElement1Code;
            var costCenterCode = string.IsNullOrEmpty(costElementCode) ? orgCostCenterCode : costElementCode;

            return new WorkingLine
            {
                Raw = l,
                WageTypeId = l.WageTypeId,
                WageTypeCode = l.WageTypeCode,
                WageTypeName = l.WageTypeName,
                BillingType = l.BillingType,
                ArrearSourceWageTypeName = l.ArrearSourceWageTypeName,
                AccountInfoId = account?.AccountInfoId,
                AccountInfoCode = account?.AccountInfoCode,
                AccountInfoName = account?.AccountInfoName,
                OffsetAccountInfoId = account?.OffsetAccountInfoId,
                OffsetAccountInfoCode = account?.OffsetAccountInfoCode,
                OffsetAccountInfoName = account?.OffsetAccountInfoName,
                CostCenterCode = costCenterCode,
                CostPercentage = account?.CostPercentage ?? 0,
                Amount = l.Amount,
                Comment = string.Empty
            };
        }).ToList();

        // ---- Legacy lines 731-859: "If AdvancePay Then Remove Arrears". ----
        bool IsRegularOrOffCycleEndOfService(RawLine l) =>
            l.PayrollRunType == PayrollRunType.Regular
            || (l.PayrollRunType == PayrollRunType.OffCycle
                && l.PayrollReasonTypeNameEn == EndOfServiceReasonNameEn);

        bool HasAdvancePayLine(WorkingLine l) => registersWithAdvancePay.Contains(l.Raw.SalaryRegisterId);

        var annualLeavePayArrear = joined
            .Where(l => IsRegularOrOffCycleEndOfService(l.Raw)
                        && l.WageTypeCode == AnnualLeavePayArrearWageTypeCode
                        && HasAdvancePayLine(l))
            .Select(l => Clone(l, comment: "AnnualLeavePayArrear"))
            .ToList();
        var employeesWithAnnualLeavePayArrearAdded = annualLeavePayArrear
            .Select(l => l.Raw.EmployeeCode)
            .ToHashSet();

        foreach (var l in joined)
        {
            var isAdvancePayCase = IsRegularOrOffCycleEndOfService(l.Raw)
                                    && SuppressIfAdvancePayWageTypeCodes.Contains(l.WageTypeCode)
                                    && HasAdvancePayLine(l);
            if (!isAdvancePayCase)
            {
                continue;
            }

            if (basicPayWageType != null)
            {
                if (basicPayWageAccountLookup.TryGetValue((l.Raw.EmployeeId, l.CostPercentage), out var basicAccount))
                {
                    l.AccountInfoId = basicAccount.Id;
                    l.AccountInfoCode = basicAccount.Code;
                    l.AccountInfoName = basicAccount.Name;
                }

                if (l.BillingType == BillingType.Deduction)
                {
                    l.Amount = -1 * l.Amount;
                }

                l.WageTypeId = basicPayWageType.Id;
                l.WageTypeCode = basicPayWageType.Code;
                l.WageTypeName = basicPayWageType.NameEn;
                l.BillingType = basicPayWageType.BillingType;
                l.ArrearSourceWageTypeName = null;
            }
        }

        joined.AddRange(annualLeavePayArrear);

        // ---- Legacy lines 861-902: "Filter OffCycle Employees in Regular Registers". ----
        if (offCycleEmployeeCodes.Count > 0)
        {
            foreach (var l in joined)
            {
                if (l.Raw.PayrollRunType == PayrollRunType.Regular
                    && offCycleEmployeeCodes.Contains(l.Raw.EmployeeCode)
                    && l.BillingType != BillingType.Provision)
                {
                    l.Amount = 0;
                }
            }
        }

        // ---- Legacy lines 917-948: CostCenter entity resolution + CostAmount. ----
        var costCenterCodesNeeded = joined.Select(l => l.CostCenterCode).Where(c => !string.IsNullOrEmpty(c))
            .Distinct().ToList();
        var costCenters = costCenterCodesNeeded.Count == 0
            ? new List<CostCenter>()
            : await _costCenterRepository.GetAll().Where(c => costCenterCodesNeeded.Contains(c.Code)).ToListAsync();
        var costCenterByCode = costCenters.GroupBy(c => c.Code).ToDictionary(g => g.Key, g => g.First());

        foreach (var l in joined)
        {
            costCenterByCode.TryGetValue(l.CostCenterCode ?? string.Empty, out var cc);
            l.ResolvedCostCenter = cc;
            l.CostAmount = Math.Round(l.CostPercentage * l.Amount / 100, 3);
        }

        // ---- Legacy lines 950-1136: FullMonthLeave / LeaveEnd. ----
        var regularEmployeeIds = joined
            .Where(l => l.Raw.PayrollRunType == PayrollRunType.Regular)
            .Select(l => l.Raw.EmployeeId)
            .Distinct()
            .ToList();

        var (fullMonthLeaveEmployeeCodes, leaveEndEmployeeCodes) = regularEmployeeIds.Count == 0
            ? (new HashSet<string>(), new HashSet<string>())
            : await ResolveLeaveFlagsAsync(regularEmployeeIds, endDate);

        var employeeCodeById = joined.Select(l => new { l.Raw.EmployeeId, l.Raw.EmployeeCode })
            .GroupBy(x => x.EmployeeId).ToDictionary(g => g.Key, g => g.First().EmployeeCode);

        foreach (var l in joined)
        {
            var isRegularOrOffCycleEos = IsRegularOrOffCycleEndOfService(l.Raw);
            var empCode = l.Raw.EmployeeCode;

            // Legacy lines 1069-1089 -- LeaveEnd suppression of 1003/2005/8005.
            if (isRegularOrOffCycleEos && leaveEndEmployeeCodes.Contains(empCode)
                                       && SuppressIfLeaveEndWageTypeCodes.Contains(l.WageTypeCode))
            {
                l.CostAmount = 0;
            }

            // Legacy lines 1092-1111 -- negate the synthetic AnnualLeavePayArrear line on LeaveEnd, else
            // zero it.
            if (isRegularOrOffCycleEos
                && employeesWithAnnualLeavePayArrearAdded.Contains(empCode)
                && l.Comment == "AnnualLeavePayArrear")
            {
                l.CostAmount = leaveEndEmployeeCodes.Contains(empCode) && l.WageTypeCode == AnnualLeavePayArrearWageTypeCode
                    ? -1 * l.CostAmount
                    : 0;
            }

            // Legacy lines 1114-1136 -- FullMonthLeave zeroing of Earning/Deduction lines.
            if (isRegularOrOffCycleEos
                && (fullMonthLeaveEmployeeCodes.Contains(empCode) || offCycleEmployeeCodes.Contains(empCode))
                && (l.BillingType == BillingType.Earning || l.BillingType == BillingType.Deduction))
            {
                l.CostAmount = 0;
            }
        }

        // ---- Legacy lines 1141-1168: exclude 8005, then drop blank CostCenter/AccountInfo. ----
        var result = joined
            .Where(l => l.WageTypeCode != AdvancePayDedArrearWageTypeCode)
            .Where(l => l.ResolvedCostCenter != null && l.AccountInfoId != null)
            .Select(l => new PayrollJVCustom016Line
            {
                EmployeeId = l.Raw.EmployeeId,
                EmployeeCode = l.Raw.EmployeeCode,
                EmployeeName = l.Raw.EmployeeName,
                EmployeeReferenceCode = l.Raw.EmployeeReferenceCode,
                WageTypeId = l.WageTypeId,
                WageTypeCode = l.WageTypeCode,
                WageTypeName = l.WageTypeName,
                BillingType = l.BillingType,
                ArrearSourceWageTypeName = l.ArrearSourceWageTypeName,
                CostCenterId = l.ResolvedCostCenter.Id,
                CostCenterCode = l.ResolvedCostCenter.Code,
                AccountInfoId = l.AccountInfoId.Value,
                AccountInfoCode = l.AccountInfoCode,
                AccountInfoName = l.AccountInfoName,
                OffsetAccountInfoId = l.OffsetAccountInfoId,
                OffsetAccountInfoCode = l.OffsetAccountInfoCode,
                OffsetAccountInfoName = l.OffsetAccountInfoName,
                CostAmount = l.CostAmount
            })
            .ToList();

        return result;
    }

    /// <summary>
    /// Legacy lines 956-1018: for each Regular-run employee, find their latest Pay-In-Advance leave
    /// starting before the month, then flag "full month leave" (leave spans the whole month) and,
    /// separately (using only PAID leave), "leave end" (leave ends within the month).
    /// </summary>
    private async Task<(HashSet<string> fullMonthLeave, HashSet<string> leaveEnd)> ResolveLeaveFlagsAsync(
        List<int> employeeIds, DateTime endDate)
    {
        var payStartDate = new DateTime(endDate.Year, endDate.Month, 1);
        var payEndDate = new DateTime(endDate.Year, endDate.Month, DateTime.DaysInMonth(endDate.Year, endDate.Month));

        var advanceLeaves = await _employeeAbsenceRepository.GetAll()
            .Where(a => employeeIds.Contains(a.EmployeeId)
                        && a.LeavePaymentType == LeavePaymentType.PayInAdvance
                        && a.PaymentDate < payStartDate)
            .Select(a => new
            {
                a.EmployeeId,
                EmployeeCode = a.Employee.Code,
                a.PaymentDate,
                a.StartDate,
                a.EndDate,
                IsPaid = a.AbsenceType.IsPaid
            })
            .ToListAsync();

        var fullMonthLeave = new HashSet<string>();
        foreach (var group in advanceLeaves.GroupBy(a => a.EmployeeId))
        {
            var maxPaymentDate = group.Max(a => a.PaymentDate);
            var span = group.Where(a => a.PaymentDate == maxPaymentDate)
                .Aggregate((min: DateTime.MaxValue, max: DateTime.MinValue),
                    (acc, a) => (acc.min < a.StartDate ? acc.min : a.StartDate, acc.max > a.EndDate ? acc.max : a.EndDate));

            if ((span.min <= payStartDate || maxPaymentDate <= payStartDate) && span.max >= payEndDate)
            {
                fullMonthLeave.Add(group.First().EmployeeCode);
            }
        }

        var leaveEnd = new HashSet<string>();
        foreach (var group in advanceLeaves.Where(a => a.IsPaid).GroupBy(a => a.EmployeeId))
        {
            var maxPaymentDate = group.Max(a => a.PaymentDate);
            var span = group.Where(a => a.PaymentDate == maxPaymentDate)
                .Aggregate((min: DateTime.MaxValue, max: DateTime.MinValue),
                    (acc, a) => (acc.min < a.StartDate ? acc.min : a.StartDate, acc.max > a.EndDate ? acc.max : a.EndDate));

            if (span.max >= payStartDate && span.max <= payEndDate)
            {
                leaveEnd.Add(group.First().EmployeeCode);
            }
        }

        return (fullMonthLeave, leaveEnd);
    }

    private static WorkingLine Clone(WorkingLine source, string comment)
    {
        return new WorkingLine
        {
            Raw = source.Raw,
            WageTypeId = source.WageTypeId,
            WageTypeCode = source.WageTypeCode,
            WageTypeName = source.WageTypeName,
            BillingType = source.BillingType,
            ArrearSourceWageTypeName = source.ArrearSourceWageTypeName,
            AccountInfoId = source.AccountInfoId,
            AccountInfoCode = source.AccountInfoCode,
            AccountInfoName = source.AccountInfoName,
            OffsetAccountInfoId = source.OffsetAccountInfoId,
            OffsetAccountInfoCode = source.OffsetAccountInfoCode,
            OffsetAccountInfoName = source.OffsetAccountInfoName,
            CostCenterCode = source.CostCenterCode,
            CostPercentage = source.CostPercentage,
            Amount = source.Amount,
            Comment = comment
        };
    }

    private class RawLine
    {
        public int SalaryRegisterId { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }
        public string EmployeeReferenceCode { get; set; }
        public PayrollRunType PayrollRunType { get; set; }
        public string PayrollReasonTypeNameEn { get; set; }
        public int WageTypeId { get; set; }
        public string WageTypeCode { get; set; }
        public string WageTypeName { get; set; }
        public BillingType BillingType { get; set; }
        public string ArrearSourceWageTypeName { get; set; }
        public double Amount { get; set; }
    }

    private class WorkingLine
    {
        public RawLine Raw { get; set; }
        public int WageTypeId { get; set; }
        public string WageTypeCode { get; set; }
        public string WageTypeName { get; set; }
        public BillingType BillingType { get; set; }
        public string ArrearSourceWageTypeName { get; set; }
        public int? AccountInfoId { get; set; }
        public string AccountInfoCode { get; set; }
        public string AccountInfoName { get; set; }
        public int? OffsetAccountInfoId { get; set; }
        public string OffsetAccountInfoCode { get; set; }
        public string OffsetAccountInfoName { get; set; }
        public string CostCenterCode { get; set; }
        public double CostPercentage { get; set; }
        public double Amount { get; set; }
        public double CostAmount { get; set; }
        public string Comment { get; set; }
        public CostCenter ResolvedCostCenter { get; set; }
    }
}

/// <summary>One fully-processed Custom016 posting line, ready for a format branch to group/shape.</summary>
public class PayrollJVCustom016Line
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; }
    public string EmployeeName { get; set; }
    public string EmployeeReferenceCode { get; set; }
    public int WageTypeId { get; set; }
    public string WageTypeCode { get; set; }
    public string WageTypeName { get; set; }
    public BillingType BillingType { get; set; }

    /// <summary>Legacy <c>narativeWT</c>: the arrear source wage type's name when present, else null.</summary>
    public string ArrearSourceWageTypeName { get; set; }

    public int CostCenterId { get; set; }
    public string CostCenterCode { get; set; }
    public int AccountInfoId { get; set; }
    public string AccountInfoCode { get; set; }
    public string AccountInfoName { get; set; }
    public int? OffsetAccountInfoId { get; set; }
    public string OffsetAccountInfoCode { get; set; }
    public string OffsetAccountInfoName { get; set; }
    public double CostAmount { get; set; }

    /// <summary>Legacy <c>narativeWT.NameEn</c> -- the arrear source name when set, else the wage type's own.</summary>
    public string NarrativeWageTypeName => ArrearSourceWageTypeName ?? WageTypeName;
}
