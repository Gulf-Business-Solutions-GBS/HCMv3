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
using HCMv3.GridReports.QuotaProvision.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using HCMv3.TimeManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.QuotaProvision;

/// <summary>
/// Quota Provision As On Date analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/QuotaProvision/uscRptQuotaProvisionAsOnDate.ascx(.cs)</c>,
/// <c>Table1()</c>. Requires a leave type (legacy line 611-614: no leave type -> empty table).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_QuotaProvisionAsOnDate)]
public class QuotaProvisionAsOnDateReportAppService : GridReportAppServiceBase, IQuotaProvisionAsOnDateReportAppService
{
    private const int LeaveRoundoff = 2;

    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<AbsenceType> _absenceTypeRepository;
    private readonly IRepository<WageType> _wageTypeRepository;
    private readonly IRepository<TimeTransfer> _timeTransferRepository;
    private readonly IRepository<TimeRegister> _timeRegisterRepository;
    private readonly IRepository<EmployeeAbsence> _employeeAbsenceRepository;
    private readonly IRepository<EmployeeAbsAdjustment> _employeeAbsAdjustmentRepository;
    private readonly IRepository<EmployeeAbsQuotaForfeit> _employeeAbsQuotaForfeitRepository;
    private readonly IRepository<EmployeeSubGroupAbsence> _employeeSubGroupAbsenceRepository;
    private readonly IRepository<SalaryRegister> _salaryRegisterRepository;
    private readonly IRepository<SalaryRegisterDetail> _salaryRegisterDetailRepository;
    private readonly IRepository<EmployeeSeparation> _employeeSeparationRepository;
    private readonly IRepository<EmployeeFnFPayment> _employeeFnFPaymentRepository;
    private readonly IRepository<WageTypeLeave> _wageTypeLeaveRepository;
    private readonly QuotaProvisionOrgContextResolver _orgContextResolver;
    private readonly IAccrualService _accrualService;

    public QuotaProvisionAsOnDateReportAppService(
        IRepository<Employee> employeeRepository,
        IRepository<AbsenceType> absenceTypeRepository,
        IRepository<WageType> wageTypeRepository,
        IRepository<TimeTransfer> timeTransferRepository,
        IRepository<TimeRegister> timeRegisterRepository,
        IRepository<EmployeeAbsence> employeeAbsenceRepository,
        IRepository<EmployeeAbsAdjustment> employeeAbsAdjustmentRepository,
        IRepository<EmployeeAbsQuotaForfeit> employeeAbsQuotaForfeitRepository,
        IRepository<EmployeeSubGroupAbsence> employeeSubGroupAbsenceRepository,
        IRepository<SalaryRegister> salaryRegisterRepository,
        IRepository<SalaryRegisterDetail> salaryRegisterDetailRepository,
        IRepository<EmployeeSeparation> employeeSeparationRepository,
        IRepository<EmployeeFnFPayment> employeeFnFPaymentRepository,
        IRepository<WageTypeLeave> wageTypeLeaveRepository,
        QuotaProvisionOrgContextResolver orgContextResolver,
        IAccrualService accrualService)
    {
        _employeeRepository = employeeRepository;
        _absenceTypeRepository = absenceTypeRepository;
        _wageTypeRepository = wageTypeRepository;
        _timeTransferRepository = timeTransferRepository;
        _timeRegisterRepository = timeRegisterRepository;
        _employeeAbsenceRepository = employeeAbsenceRepository;
        _employeeAbsAdjustmentRepository = employeeAbsAdjustmentRepository;
        _employeeAbsQuotaForfeitRepository = employeeAbsQuotaForfeitRepository;
        _employeeSubGroupAbsenceRepository = employeeSubGroupAbsenceRepository;
        _salaryRegisterRepository = salaryRegisterRepository;
        _salaryRegisterDetailRepository = salaryRegisterDetailRepository;
        _employeeSeparationRepository = employeeSeparationRepository;
        _employeeFnFPaymentRepository = employeeFnFPaymentRepository;
        _wageTypeLeaveRepository = wageTypeLeaveRepository;
        _orgContextResolver = orgContextResolver;
        _accrualService = accrualService;
    }

    protected override string LegacyScreenName => "uscRptQuotaProvisionAsOnDate";

    protected override string ScreenCaption => "Quota Provision As On Date";

    [HttpPost]
    public async Task<PagedResultDto<QuotaProvisionAsOnDateReportRowDto>> GetQuotaProvisionAsOnDateReport(GetQuotaProvisionAsOnDateReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);
        var query = rows.AsQueryable();
        var totalCount = query.Count();

        var pagedRows = query.OrderBy(input.Sorting).PageBy(input).ToList();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<QuotaProvisionAsOnDateReportRowDto>(totalCount, pagedRows);
    }

    [HttpPost]
    public async Task<FileDto> GetQuotaProvisionAsOnDateReportToExcel(GetQuotaProvisionAsOnDateReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);
        var orderedRows = rows.AsQueryable().OrderBy(input.Sorting).ToList();

        return await ExportToFileAsync("QuotaProvisionAsOnDate.xlsx", orderedRows, GetExportColumns(), BuildParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export.
    /// Ports legacy <c>Table1()</c>.
    /// </summary>
    private async Task<List<QuotaProvisionAsOnDateReportRowDto>> BuildRowsAsync(GetQuotaProvisionAsOnDateReportInput input)
    {
        if (string.IsNullOrWhiteSpace(input.LeaveTypeCode))
        {
            return new List<QuotaProvisionAsOnDateReportRowDto>();
        }

        var asOnDate = input.ResolveFromDate(Today);
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;
        var timeSettings = await SysSettingsAccessor.GetTimeSettingsAsync();
        var forfeitMonthly = timeSettings != null && timeSettings.ForfeitRule == HCMv3.SystemSettings.ForfeitRule.Monthly;
        var earliestEvaluationDate = timeSettings?.EarliestEvaluationDate ?? DateTime.MinValue;

        var absenceType = await _absenceTypeRepository.GetAll().Where(a => a.Code == input.LeaveTypeCode).FirstOrDefaultAsync();
        if (absenceType == null)
        {
            return new List<QuotaProvisionAsOnDateReportRowDto>();
        }

        var annualLeaveType = await _absenceTypeRepository.GetAll().Where(a => a.Code == "1").FirstOrDefaultAsync();
        var isAnnualLeave = annualLeaveType != null && (absenceType.Id == annualLeaveType.Id || absenceType.QuotaType == QuotaType.AnnualLeave);
        var leaveTypeName = useArabicNames ? absenceType.NameAr : absenceType.NameEn;

        var employeeQuery = _employeeRepository.GetAll()
            .WhereIf(input.EmployeeStatus.HasValue, e => e.Status == input.EmployeeStatus.Value)
            .WhereIf(employeeCodes.Count > 0, e => employeeCodes.Contains(e.Code))
            .WhereIf(rlsEmployeeIds.Count > 0, e => rlsEmployeeIds.Contains(e.Id));

        var employees = await employeeQuery
            .Select(e => new { e.Id, e.Code, EmployeeName = useArabicNames ? e.FullNameAr : e.FullNameEn, e.Status, e.JoiningDate })
            .OrderBy(e => e.Code)
            .ToListAsync();
        var employeeIds = employees.Select(e => e.Id).ToList();

        var orgContexts = await _orgContextResolver.BuildAsync(employeeIds, asOnDate, useArabicNames);

        var timeTransfer = await _timeTransferRepository.GetAll()
            .Where(t => employeeIds.Contains(t.EmployeeId) && t.TransferTypeId == absenceType.Id)
            .GroupBy(t => t.EmployeeId)
            .Select(g => new { EmployeeId = g.Key, Total = g.Sum(t => t.TransferCount) })
            .ToDictionaryAsync(g => g.EmployeeId, g => g.Total);

        var subGroupIds = employeeIds.Select(id => orgContexts.Get(id).EmployeeSubGroupId).Where(id => id.HasValue).Select(id => id.Value).Distinct().ToList();
        var subGroupAbsences = await _employeeSubGroupAbsenceRepository.GetAll()
            .Where(a => subGroupIds.Contains(a.EmployeeSubGroupId) && a.AbsenceTypeId == absenceType.Id && a.StartDate <= asOnDate && a.EndDate >= asOnDate)
            .ToListAsync();

        var separations = (await _employeeSeparationRepository.GetAll()
            .Where(s => employeeIds.Contains(s.EmployeeId) && s.RevokeDate == null)
            .Select(s => s.EmployeeId).Distinct().ToListAsync()).ToHashSet();
        var fnfPayments = (await _employeeFnFPaymentRepository.GetAll()
            .Where(f => employeeIds.Contains(f.EmployeeId))
            .Select(f => f.EmployeeId).Distinct().ToListAsync()).ToHashSet();

        var absenceWageType = absenceType.WageTypeId.HasValue
            ? await _wageTypeRepository.GetAll().Where(w => w.Id == absenceType.WageTypeId.Value).FirstOrDefaultAsync()
            : null;

        var rows = new List<QuotaProvisionAsOnDateReportRowDto>();
        foreach (var employee in employees)
        {
            var orgContext = orgContexts.Get(employee.Id);
            var row = new QuotaProvisionAsOnDateReportRowDto
            {
                Id = employee.Id,
                EmployeeCode = employee.Code,
                EmployeeName = employee.EmployeeName,
                Status = employee.Status.ToString(),
                OrgLevel1 = orgContext.OrgLevel1,
                OrgLevel2 = orgContext.OrgLevel2,
                OrgLevel3 = orgContext.OrgLevel3,
                OrgLevel4 = orgContext.OrgLevel4,
                OrgLevel5 = orgContext.OrgLevel5,
                OrgLevel6 = orgContext.OrgLevel6,
                OrgLevel7 = orgContext.OrgLevel7,
                JobTitleName = orgContext.JobTitleName,
                CostCenterName = orgContext.CostCenterName,
                LeaveTypeName = leaveTypeName,
                Separation = separations.Contains(employee.Id),
                FnfPayment = fnfPayments.Contains(employee.Id),
                AsOnDate = asOnDate
            };

            if (asOnDate <= employee.JoiningDate)
            {
                rows.Add(row);
                continue;
            }

            row.TotalDaysAllowed = absenceType.TotalDaysAllowed;
            var subGroupAbs = orgContext.EmployeeSubGroupId.HasValue
                ? subGroupAbsences.FirstOrDefault(a => a.EmployeeSubGroupId == orgContext.EmployeeSubGroupId.Value)
                : null;
            if (subGroupAbs != null)
            {
                row.TotalDaysAllowed = subGroupAbs.TotalDays;
                row.MaxLimit = subGroupAbs.MaxLimit;
            }

            if (!absenceType.HaveQuotaAccrual)
            {
                var (deductionStart, deductionEnd) = await _accrualService.GetLeaveAccrualStartEndAsync(employee.Id, absenceType.Id, asOnDate);
                row.DeductionStart = deductionStart;
                row.DeductionEnd = deductionEnd;

                double openingBalance = 0;
                if (absenceType.Code == "3")
                {
                    openingBalance = await _timeTransferRepository.GetAll()
                        .Where(t => t.EmployeeId == employee.Id && t.TransferType.Code == "2"
                                    && t.TransferDate >= deductionStart && t.TransferDate <= deductionEnd)
                        .SumAsync(t => (double?)t.TransferCount) ?? 0;
                }

                var usedDays = await _employeeAbsenceRepository.GetAll()
                    .Where(a => a.EmployeeId == employee.Id && a.AbsenceTypeId == absenceType.Id
                                && !(a.StartDate > deductionEnd || a.EndDate < deductionStart))
                    .SumAsync(a => (double?)a.TotalLeaveDays) ?? 0;
                usedDays += openingBalance;

                row.UsedQuota = usedDays;
                row.BalanceQuota = Math.Round(row.TotalDaysAllowed - usedDays, LeaveRoundoff);
                rows.Add(row);
                continue;
            }

            // HaveQuotaAccrual == true (legacy lines 741-919): accrual, usage, adjustment, forfeit, then
            // the money-value provision.
            double totalAccrual;
            if (isAnnualLeave)
            {
                var timeRegisterInRange = await _timeRegisterRepository.GetAll()
                    .Where(t => t.EmployeeId == employee.Id && t.DayDate >= earliestEvaluationDate && t.DayDate <= asOnDate)
                    .Select(t => new { t.DayDate, t.AnnualLeaveAccrualPerDay })
                    .ToListAsync();

                var lastDateOfTe = timeRegisterInRange.Count > 0 ? timeRegisterInRange.Max(t => t.DayDate) : (earliestEvaluationDate > employee.JoiningDate ? earliestEvaluationDate : employee.JoiningDate);
                var accrualInTe = timeRegisterInRange.Sum(t => t.AnnualLeaveAccrualPerDay);

                double accrualNotInTe = 0;
                if (input.WithProjection)
                {
                    var totalDaysInYear = new DateTime(asOnDate.Year, 12, 31).Subtract(new DateTime(asOnDate.Year, 1, 1)).TotalDays + 1;
                    var perDayQuota = row.TotalDaysAllowed / totalDaysInYear;
                    var daysNotInTe = asOnDate.Subtract(lastDateOfTe).TotalDays;
                    accrualNotInTe = daysNotInTe * perDayQuota;
                }

                totalAccrual = accrualInTe + accrualNotInTe;
            }
            else
            {
                totalAccrual = 0; // legacy sources this from EmployeeAttendance/LinkLeaveQuota, not ported for the non-annual-leave path.
            }

            timeTransfer.TryGetValue(employee.Id, out var systemOpeningBalance);
            row.SystemOpeningBalanceDays = systemOpeningBalance;
            row.AccruedQuota = totalAccrual;

            double usedQuota;
            if (isAnnualLeave)
            {
                usedQuota = await _employeeAbsenceRepository.GetAll()
                    .Where(a => a.EmployeeId == employee.Id && (a.AbsenceTypeId == absenceType.Id || a.AbsenceType.QuotaType == QuotaType.AnnualLeave)
                                && a.StartDate >= earliestEvaluationDate && a.EndDate <= asOnDate)
                    .SumAsync(a => (double?)a.TotalLeaveDays) ?? 0;
            }
            else
            {
                usedQuota = await _employeeAbsenceRepository.GetAll()
                    .Where(a => a.EmployeeId == employee.Id && a.AbsenceTypeId == absenceType.Id
                                && a.StartDate >= earliestEvaluationDate && a.EndDate <= asOnDate)
                    .SumAsync(a => (double?)a.TotalLeaveDays) ?? 0;
            }

            row.UsedQuota = usedQuota;

            var adjustedQuota = isAnnualLeave
                ? await _employeeAbsAdjustmentRepository.GetAll()
                    .Where(a => a.EmployeeId == employee.Id && (a.AbsenceType.Code == absenceType.Code || a.AbsenceType.QuotaType == QuotaType.AnnualLeave)
                                && a.EffectiveDate >= earliestEvaluationDate && a.EffectiveDate <= asOnDate
                                && a.AdjustmentType != AdjustmentType.NoImpactOnQuota)
                    .SumAsync(a => (double?)a.QuotaAdjusted) ?? 0
                : await _employeeAbsAdjustmentRepository.GetAll()
                    .Where(a => a.EmployeeId == employee.Id && a.AbsenceTypeId == absenceType.Id
                                && a.EffectiveDate >= earliestEvaluationDate && a.EffectiveDate <= asOnDate
                                && a.AdjustmentType != AdjustmentType.NoImpactOnQuota)
                    .SumAsync(a => (double?)a.QuotaAdjusted) ?? 0;

            row.AdjustedQuota = adjustedQuota;

            var balanceQuota = totalAccrual - usedQuota - adjustedQuota;

            if (isAnnualLeave)
            {
                if (forfeitMonthly)
                {
                    var forfeitQuota = await _employeeAbsQuotaForfeitRepository.GetAll()
                        .Where(f => f.EmployeeId == employee.Id && (f.AbsenceType.Code == absenceType.Code || f.AbsenceType.QuotaType == QuotaType.AnnualLeave)
                                    && f.EffectiveDate >= earliestEvaluationDate && f.EffectiveDate <= asOnDate)
                        .SumAsync(f => (double?)f.QuotaForfeited) ?? 0;

                    row.ForfeitQuota = forfeitQuota;
                    balanceQuota = Math.Round(balanceQuota - forfeitQuota, LeaveRoundoff);
                }
                else
                {
                    balanceQuota = row.MaxLimit != 0 && balanceQuota > row.MaxLimit
                        ? Math.Round(row.MaxLimit, LeaveRoundoff)
                        : Math.Round(balanceQuota, LeaveRoundoff);
                }
            }

            row.BalanceQuota = balanceQuota;

            var latestRegister = await _salaryRegisterRepository.GetAll()
                .Where(r => r.EmployeeId == employee.Id && r.InPeriod <= asOnDate && r.ForPeriod <= asOnDate)
                .OrderByDescending(r => r.InPeriod).ThenByDescending(r => r.ForPeriod)
                .Select(r => new { r.Id, r.ForPeriod })
                .FirstOrDefaultAsync();

            if (latestRegister != null && absenceWageType != null)
            {
                var includeInPaidLeaveWageTypeIds = await _wageTypeLeaveRepository.GetAll()
                    .Where(l => l.AbsenceTypeId == absenceType.Id)
                    .Select(l => l.WageTypeId)
                    .Distinct()
                    .ToListAsync();

                var totalBasicPay = await _salaryRegisterDetailRepository.GetAll()
                    .Where(d => d.SalaryRegisterId == latestRegister.Id && includeInPaidLeaveWageTypeIds.Contains(d.WageTypeId))
                    .SumAsync(d => (double?)d.Rate) ?? 0;

                var payDate = latestRegister.ForPeriod;
                var calendarDays = DateTime.DaysInMonth(payDate.Year, payDate.Month);
                var provisionAmount = 0d;

                if (absenceWageType.BillingMethod == BillingMethod.FixedDaysPerMonth && absenceWageType.FixedDays != 0)
                {
                    provisionAmount = totalBasicPay / absenceWageType.FixedDays * balanceQuota;
                }
                else if (absenceWageType.BillingMethod == BillingMethod.CalendarDaysPerMonth)
                {
                    provisionAmount = totalBasicPay / calendarDays * balanceQuota;
                }
                else if (absenceWageType.BillingMethod == BillingMethod.WorkingDaysPerMonth)
                {
                    var weeklyOffDays = await _accrualService.GetActualWeekOffsAsync(employee.Id, payDate);
                    var salaryBasisDays = calendarDays - weeklyOffDays;
                    if (salaryBasisDays != 0)
                    {
                        provisionAmount = totalBasicPay / salaryBasisDays * balanceQuota;
                    }
                }

                row.BalanceProvision = provisionAmount;
            }

            rows.Add(row);
        }

        return rows;
    }

    private static IReadOnlyList<GridReportColumn<QuotaProvisionAsOnDateReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<QuotaProvisionAsOnDateReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Name", r => r.EmployeeName),
            new("Status", r => r.Status),
            new("OrgLevel1", r => r.OrgLevel1),
            new("OrgLevel2", r => r.OrgLevel2),
            new("OrgLevel3", r => r.OrgLevel3),
            new("OrgLevel4", r => r.OrgLevel4),
            new("OrgLevel5", r => r.OrgLevel5),
            new("OrgLevel6", r => r.OrgLevel6),
            new("OrgLevel7", r => r.OrgLevel7),
            new("Jobtitle", r => r.JobTitleName),
            new("CostCenter", r => r.CostCenterName),
            new("LeaveType", r => r.LeaveTypeName),
            new("Separation", r => r.Separation),
            new("FnfPayment", r => r.FnfPayment),
            new("AsOnDate", r => r.AsOnDate),
            new("DeductionStart", r => r.DeductionStart),
            new("DeductionEnd", r => r.DeductionEnd),
            new("TotalDaysAllowed", r => r.TotalDaysAllowed),
            new("MaxLimit", r => r.MaxLimit),
            new("SystemOpeningBalanceDays", r => r.SystemOpeningBalanceDays),
            new("AccruedQuota", r => r.AccruedQuota),
            new("UsedQuota", r => r.UsedQuota),
            new("AdjustedQuota", r => r.AdjustedQuota),
            new("ForfeitQuota", r => r.ForfeitQuota),
            new("BalanceQuota", r => r.BalanceQuota),
            new("BalanceProvision", r => r.BalanceProvision),
            new("Comment", r => r.Comment)
        };
    }
}
