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
using HCMv3.GridReports.PayrollReport.Dto;
using HCMv3.SystemSecurity;
using HCMv3.OrganizationManagement;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// Payroll JV (Custom 042) "Advance Salary JV" analysis grid report -- port of legacy
/// <c>uscRptPayrollJV_Custom042.ascx.cs</c>, <c>cmbType == "Advance Salary JV"</c> branch of
/// <c>Table1()</c> (lines 1353-1791). Per employee, classifies their wage-account-mapped salary lines
/// into six categories and books one line (or several month-wise lines) per category:
/// <list type="number">
/// <item><description>Advance Salary Reimbursement -- wage types with <c>IncludeInBasicPay</c> (plus
/// their arrear-source chain), against hard-coded account "516000".</description></item>
/// <item><description>Provision-Leave Salary -- Annual/Public-Holiday/Ramadan/Lieu-Day leave-pay wage
/// types, ONE ROW PER MONTH from <c>SalaryRegisterDaysDetail</c>, against the wage type's OWN mapped
/// account (hard-coded "614000" fallback narration only).</description></item>
/// <item><description>Provision-SickLeave Salary -- sick-leave-pay wage type, month-wise, against
/// "516000"/employee's own account with a FIXED Analysis1 cost element ("SP2").</description></item>
/// <item><description>Other-Leave Salary -- every other paid-leave wage type, month-wise.</description></item>
/// <item><description>Social Security Deduction -- wage type "2002", against "618001"/"61814".</description></item>
/// <item><description>Net-Leave Salary Payable -- wage type "9000", against "618001"/"61811".</description></item>
/// </list>
///
/// RUN TYPE OVERRIDE: legacy line 613-614 forces <c>PayrollRunType.OffCycle</c> for this format
/// regardless of the <c>cmbRunType</c> combo -- Advance Salary is inherently an off-cycle scenario. This
/// port always filters OffCycle for this one report; <see cref="GetPayrollJVCustom042AdvanceSalaryReportInput.PayrollRunType"/>
/// is intentionally ignored.
///
/// NOT PORTED -- the same <c>Table_GetAdvanceJV</c> off-cycle bifurcation gap documented on
/// <see cref="PayrollJVCustom042SalaryReportAppService"/> does not apply here (this format never calls
/// it); the gap that DOES apply to this format is narrower: the RunType/PayrollArea narrowing this
/// report's own SalaryRegisterDaysDetail-month-wise sums rely on is inherited from the wage-account join
/// (correct), but the "Rate" sums are not separately re-narrowed -- see
/// <see cref="LeaveProvisionQuotaCalculator"/>'s remarks for the identical, already-flagged simplification
/// pattern (not applicable here directly, referenced for consistency of documentation style).
///
/// DIVERGENCE (agreed for this wave): RLS is applied -- see <see cref="PayrollJVPostingLineLoader"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_PayrollJVCustom042AdvanceSalary)]
public class PayrollJVCustom042AdvanceSalaryReportAppService
    : PayrollJVCustom042ReportAppServiceBase, IPayrollJVCustom042AdvanceSalaryReportAppService
{
    private const string StaffAccountCode = "516000";
    private const string ProvisionLeaveSalaryAccountCode = "614000";
    private const string StaffRelatedPayableAccountCode = "618001";
    private const string SocialSecurityPayableAccountCode = "61814";
    private const string LeaveSalaryPayableAccountCode = "61811";
    private const string AnalysisCostElementCode = "SP2";

    private const string SocialSecurityDeductionWageTypeCode = "2002";
    private const string NetPayWageTypeCode = "9000";
    private const string SickLeaveAbsenceTypeCode = "3";
    private const string AnnualLeaveAbsenceTypeCode = "1";
    private const string LieuDayAbsenceTypeCode = "9";
    private const string PublicHolidayAbsenceTypeCode = "21";
    private const string RamadanTimeBackAbsenceTypeCode = "19";

    private readonly PayrollJVPostingLineLoader _postingLineLoader;
    private readonly IRepository<WageType> _wageTypeRepository;
    private readonly IRepository<AbsenceType> _absenceTypeRepository;
    private readonly IRepository<SalaryRegisterDaysDetail> _salaryRegisterDaysDetailRepository;
    private readonly IRepository<AccountInfo> _accountInfoRepository;
    private readonly IRepository<CostElement> _costElementRepository;

    public PayrollJVCustom042AdvanceSalaryReportAppService(
        PayrollJVPostingLineLoader postingLineLoader,
        IRepository<WageType> wageTypeRepository,
        IRepository<AbsenceType> absenceTypeRepository,
        IRepository<SalaryRegisterDaysDetail> salaryRegisterDaysDetailRepository,
        IRepository<AccountInfo> accountInfoRepository,
        IRepository<CostElement> costElementRepository)
    {
        _postingLineLoader = postingLineLoader;
        _wageTypeRepository = wageTypeRepository;
        _absenceTypeRepository = absenceTypeRepository;
        _salaryRegisterDaysDetailRepository = salaryRegisterDaysDetailRepository;
        _accountInfoRepository = accountInfoRepository;
        _costElementRepository = costElementRepository;
    }

    protected override string LegacyScreenName => "uscRptPayrollJV_Custom042";

    protected override string ScreenCaption => "Payroll JV (Custom 042) - Advance Salary JV";

    [HttpPost]
    public async Task<PagedResultDto<PayrollJVCustom042ReportRowDto>> GetPayrollJVCustom042AdvanceSalaryReport(
        GetPayrollJVCustom042AdvanceSalaryReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);

        var paged = rows.AsQueryable().OrderBy(input.Sorting).PageBy(input).ToList();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildJvParameterString(input), rows.Count);

        return new PagedResultDto<PayrollJVCustom042ReportRowDto>(rows.Count, paged);
    }

    [HttpPost]
    public async Task<FileDto> GetPayrollJVCustom042AdvanceSalaryReportToExcel(
        GetPayrollJVCustom042AdvanceSalaryReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);
        var ordered = rows.AsQueryable().OrderBy(input.Sorting).ToList();

        return await ExportToFileAsync(
            "PayrollJVCustom042AdvanceSalary.xlsx",
            ordered,
            GetPostingExportColumns(),
            BuildJvParameterString(input));
    }

    private async Task<List<PayrollJVCustom042ReportRowDto>> BuildRowsAsync(GetPayrollJVCustom042AdvanceSalaryReportInput input)
    {
        var endDate = ResolveEndDate(input.Month, input.Year);
        var currency = "KD";

        // Legacy lines 613-614: this format always runs OffCycle.
        var lines = await _postingLineLoader.LoadAsync(
            endDate,
            Array.Empty<string>(),
            HCMv3.PayrollManagement.PayrollRunType.OffCycle,
            input.PayrollAreaId.HasValue ? new[] { input.PayrollAreaId.Value } : Array.Empty<int>(),
            input.ResolveEmployeeCodes(),
            await GetRlsEmployeeIdsAsync(),
            PayrollJVWageTypeScope.EarningDeductionAndAllProvision);

        // Legacy line 1500: DontImpactNetPay == false.
        var advLines = lines.Where(l => !l.DontImpactNetPay).ToList();

        var wageTypes = await _wageTypeRepository.GetAll()
            .Where(w => w.BillingType == BillingType.Earning || w.BillingType == BillingType.Deduction)
            .Select(w => new WageTypeArrearInfo
            {
                Code = w.Code,
                IncludeInBasicPay = w.IncludeInBasicPay,
                IncludeInArrearPay = w.IncludeInArrearPay,
                ArrearSourceCode = w.ArrearSourceWageType.Code
            })
            .ToListAsync();

        var allWageTypeCodesById = await _wageTypeRepository.GetAll()
            .Select(w => new { w.Id, w.Code })
            .ToDictionaryAsync(w => w.Id, w => w.Code);

        var leaveTypesRaw = await _absenceTypeRepository.GetAll()
            .Where(a => a.IsPaid && a.WageTypeId != null)
            .Select(a => new { a.Code, a.QuotaType, a.WageTypeId })
            .ToListAsync();

        var leaveTypes = leaveTypesRaw
            .Where(a => allWageTypeCodesById.ContainsKey(a.WageTypeId.Value))
            .Select(a => new { a.Code, a.QuotaType, WageTypeCode = allWageTypeCodesById[a.WageTypeId.Value] })
            .ToList();

        var wtAdvBasicSalary = ExpandWithArrears(
            wageTypes.Where(w => w.IncludeInBasicPay).Select(w => w.Code).ToList(), wageTypes);

        var wtAdvSickLeavePay = ExpandWithArrears(
            leaveTypes.Where(l => l.Code == SickLeaveAbsenceTypeCode).Select(l => l.WageTypeCode).ToList(), wageTypes);

        var wtALPhRamdanLieuPay = ExpandWithArrears(
            leaveTypes.Where(l => l.Code == AnnualLeaveAbsenceTypeCode || l.QuotaType == QuotaType.AnnualLeave
                                   || l.Code == LieuDayAbsenceTypeCode || l.Code == PublicHolidayAbsenceTypeCode
                                   || l.Code == RamadanTimeBackAbsenceTypeCode)
                .Select(l => l.WageTypeCode).ToList(), wageTypes);

        var wtOtherLeavePay = ExpandWithArrears(
            leaveTypes.Where(l => !wtAdvSickLeavePay.Contains(l.WageTypeCode) && !wtALPhRamdanLieuPay.Contains(l.WageTypeCode))
                .Select(l => l.WageTypeCode).ToList(), wageTypes);

        var staffAccount = await _accountInfoRepository.FirstOrDefaultAsync(a => a.Code == StaffAccountCode);
        var provisionLeaveSalaryAccount = await _accountInfoRepository.FirstOrDefaultAsync(a => a.Code == ProvisionLeaveSalaryAccountCode);
        var staffRelatedPayableAccount = await _accountInfoRepository.FirstOrDefaultAsync(a => a.Code == StaffRelatedPayableAccountCode);
        var socialSecurityPayableAccount = await _accountInfoRepository.FirstOrDefaultAsync(a => a.Code == SocialSecurityPayableAccountCode);
        var leaveSalaryPayableAccount = await _accountInfoRepository.FirstOrDefaultAsync(a => a.Code == LeaveSalaryPayableAccountCode);
        var analysisElement = await _costElementRepository.FirstOrDefaultAsync(c => c.Code == AnalysisCostElementCode);

        var employeeCodes = advLines.Select(l => l.EmployeeCode).Distinct().ToList();
        var employeeAccounts = await _accountInfoRepository.GetAll()
            .Where(a => employeeCodes.Contains(a.Code))
            .ToDictionaryAsync(a => a.Code, a => a);

        var rows = new List<PayrollJVCustom042ReportRowDto>();

        foreach (var empGroup in advLines.GroupBy(l => new { l.EmployeeId, l.EmployeeCode }))
        {
            var empCode = empGroup.Key.EmployeeCode;
            employeeAccounts.TryGetValue(empCode, out var empAccount);
            var empFullName = empAccount != null ? empAccount.NameEn : empCode;
            var empLines = empGroup.ToList();

            // Legacy lines 1520-1547 -- Advance Salary Reimbursement.
            var advSalAmount = empLines.Where(l => wtAdvBasicSalary.Contains(l.WageTypeCode)).Sum(l => l.Amount);
            if (advSalAmount > 0)
            {
                rows.Add(NewRow(rows,
                    staffAccount?.Code, staffAccount?.NameEn, empAccount?.Code, empAccount?.NameEn,
                    "Being salary adv of " + empFullName,
                    "", "", analysisElement?.Code,
                    currency, advSalAmount, "D"));
            }

            // Legacy lines 1550-1606 -- Provision-Leave Salary (AL/PH/Ramadan/Lieu), month-wise. Main is the
            // hard-coded "614000" account; Sub is always blank (legacy lines 1586-1589); Div/Dept/Anly1 come
            // from the wage type's OWN wage-account mapping.
            await AddMonthWiseRowsAsync(rows, empLines, wtALPhRamdanLieuPay, empFullName, currency,
                (line) => (line.CostElement1Code, line.CostElement2Code, line.CostElement3Code),
                (line) => (provisionLeaveSalaryAccount?.Code, provisionLeaveSalaryAccount?.NameEn),
                subOverride: ("", ""));

            // Legacy lines 1608-1669 -- Provision-SickLeave Salary, month-wise, FIXED account/analysis.
            var sickLeaveTotal = empLines.Where(l => wtAdvSickLeavePay.Contains(l.WageTypeCode)).Sum(l => l.Amount);
            if (sickLeaveTotal > 0)
            {
                var sickLine = empLines.First(l => wtAdvSickLeavePay.Contains(l.WageTypeCode));
                var monthWise = await _salaryRegisterDaysDetailRepository.GetAll()
                    .Where(d => d.SalaryRegisterDetailId == sickLine.SalaryRegisterDetailId)
                    .OrderBy(d => d.Period)
                    .ToListAsync();

                foreach (var month in monthWise)
                {
                    var desc = "Being leave salary adv of " + empFullName + " " + month.Number + " days "
                               + sickLine.WageTypeName + " booked (" + month.Period.ToString("MMM-yyyy") + ")";

                    rows.Add(NewRow(rows,
                        staffAccount?.Code, staffAccount?.NameEn, empAccount?.Code, empAccount?.NameEn,
                        desc,
                        sickLine.CostElement1Code, sickLine.CostElement2Code, analysisElement?.Code,
                        currency, month.Amount, month.Amount > 0 ? "D" : "C"));
                }
            }

            // Legacy lines 1671-1727 -- Other-Leave Salary, month-wise, wage-account-driven cost elements.
            await AddMonthWiseRowsAsync(rows, empLines, wtOtherLeavePay, empFullName, currency,
                (line) => (line.CostElement1Code, line.CostElement2Code, line.CostElement3Code),
                (line) => (staffAccount?.Code, staffAccount?.NameEn), subOverride: (empAccount?.Code, empAccount?.NameEn));

            // Legacy lines 1729-1759 -- Social Security Deduction.
            var socSecAmount = empLines
                .Where(l => l.WageTypeCode == SocialSecurityDeductionWageTypeCode)
                .Sum(l => l.Amount);
            if (socSecAmount > 0)
            {
                var socSecLine = empLines.FirstOrDefault(l => l.WageTypeCode == SocialSecurityDeductionWageTypeCode);
                rows.Add(NewRow(rows,
                    staffRelatedPayableAccount?.Code, staffRelatedPayableAccount?.NameEn,
                    socialSecurityPayableAccount?.Code, socialSecurityPayableAccount?.NameEn,
                    "Being social security deduction adv of " + empFullName + " booked",
                    socSecLine?.CostElement1Code, socSecLine?.CostElement2Code, socSecLine?.CostElement3Code,
                    currency, socSecAmount, "C"));
            }

            // Legacy lines 1761-1789 -- Net-Leave Salary Payable.
            var netAmount = empLines.Where(l => l.WageTypeCode == NetPayWageTypeCode).Sum(l => l.Amount);
            if (netAmount > 0)
            {
                rows.Add(NewRow(rows,
                    staffRelatedPayableAccount?.Code, staffRelatedPayableAccount?.NameEn,
                    leaveSalaryPayableAccount?.Code, leaveSalaryPayableAccount?.NameEn,
                    "Being salary adv & leave salary adv of " + empFullName,
                    "", "", "",
                    currency, netAmount, "C"));
            }
        }

        return rows;
    }

    /// <summary>
    /// Legacy's identical Provision-Leave-Salary / Other-Leave-Salary loop shape (lines 1551-1605 and
    /// 1672-1726): for every wage type in <paramref name="wageTypeCodes"/> with a positive total, walk its
    /// <c>SalaryRegisterDaysDetail</c> rows and emit one line per month.
    /// </summary>
    private async Task AddMonthWiseRowsAsync(
        List<PayrollJVCustom042ReportRowDto> rows,
        List<PayrollJVPostingLine> empLines,
        IReadOnlyCollection<string> wageTypeCodes,
        string empFullName,
        string currency,
        Func<PayrollJVPostingLine, (string Div, string Dept, string Anly1)> costElementSelector,
        Func<PayrollJVPostingLine, (string Code, string Name)> mainAccountSelector,
        (string Code, string Name)? subOverride = null)
    {
        foreach (var wageTypeGroup in empLines.Where(l => wageTypeCodes.Contains(l.WageTypeCode)).GroupBy(l => l.WageTypeId))
        {
            var sample = wageTypeGroup.First();
            var total = wageTypeGroup.Sum(l => l.Amount);
            if (total <= 0)
            {
                continue;
            }

            var monthWise = await _salaryRegisterDaysDetailRepository.GetAll()
                .Where(d => d.SalaryRegisterDetailId == sample.SalaryRegisterDetailId)
                .OrderBy(d => d.Period)
                .ToListAsync();

            var (div, dept, anly1) = costElementSelector(sample);
            var (mainCode, mainName) = mainAccountSelector(sample);
            var (subCode, subName) = subOverride ?? (sample.OffsetAccountInfoCode, sample.OffsetAccountInfoName);

            foreach (var month in monthWise)
            {
                var desc = "Being leave salary adv of " + empFullName + " " + month.Number + " days "
                           + sample.WageTypeName + " booked (" + month.Period.ToString("MMM-yyyy") + ")";

                rows.Add(NewRow(rows,
                    mainCode, mainName, subCode, subName,
                    desc,
                    div, dept, anly1,
                    currency, month.Amount, month.Amount > 0 ? "D" : "C"));
            }
        }
    }

    /// <summary>Legacy's repeated "plus every arrear wage type whose source is already in the list" step.</summary>
    private static List<string> ExpandWithArrears(
        List<string> baseCodes,
        IEnumerable<WageTypeArrearInfo> wageTypes)
    {
        var codes = baseCodes.Distinct().ToList();
        var arrears = wageTypes
            .Where(w => w.IncludeInArrearPay && codes.Contains(w.ArrearSourceCode))
            .Select(w => w.Code)
            .ToList();
        codes.AddRange(arrears);
        return codes.Distinct().ToList();
    }

    private class WageTypeArrearInfo
    {
        public string Code { get; set; }

        public bool IncludeInBasicPay { get; set; }

        public bool IncludeInArrearPay { get; set; }

        public string ArrearSourceCode { get; set; }
    }
}
