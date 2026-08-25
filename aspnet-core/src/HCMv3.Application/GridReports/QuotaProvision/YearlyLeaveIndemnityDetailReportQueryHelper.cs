using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.GridReports.QuotaProvision.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSettings;
using HCMv3.TimeManagement;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.QuotaProvision;

/// <summary>
/// Shared query logic for the Yearly Leave Indemnity Detail Custom016/Custom023 reports -- see
/// <see cref="Dto.GetYearlyLeaveIndemnityDetailReportInput"/> remarks for why one helper backs two legacy
/// screens. Ports legacy <c>Table1()</c> (identical in both controls; the dead <c>Table1_Old</c> in each is
/// not ported).
/// </summary>
public class YearlyLeaveIndemnityDetailReportQueryHelper : ITransientDependency
{
    private const int LeaveRoundoff = 2;

    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<AbsenceType> _absenceTypeRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly IRepository<TimeRegister> _timeRegisterRepository;
    private readonly IRepository<EmployeeAbsence> _employeeAbsenceRepository;
    private readonly IRepository<EmployeeAbsAdjustment> _employeeAbsAdjustmentRepository;
    private readonly IRepository<EmployeeAbsQuotaForfeit> _employeeAbsQuotaForfeitRepository;
    private readonly IRepository<EmployeeSeparation> _employeeSeparationRepository;
    private readonly IRepository<SalaryRegister> _salaryRegisterRepository;
    private readonly IRepository<SalaryRegisterDetail> _salaryRegisterDetailRepository;
    private readonly IRepository<WageTypeLeave> _wageTypeLeaveRepository;
    private readonly QuotaProvisionOrgContextResolver _orgContextResolver;

    public YearlyLeaveIndemnityDetailReportQueryHelper(
        IRepository<Employee> employeeRepository,
        IRepository<AbsenceType> absenceTypeRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        IRepository<TimeRegister> timeRegisterRepository,
        IRepository<EmployeeAbsence> employeeAbsenceRepository,
        IRepository<EmployeeAbsAdjustment> employeeAbsAdjustmentRepository,
        IRepository<EmployeeAbsQuotaForfeit> employeeAbsQuotaForfeitRepository,
        IRepository<EmployeeSeparation> employeeSeparationRepository,
        IRepository<SalaryRegister> salaryRegisterRepository,
        IRepository<SalaryRegisterDetail> salaryRegisterDetailRepository,
        IRepository<WageTypeLeave> wageTypeLeaveRepository,
        QuotaProvisionOrgContextResolver orgContextResolver)
    {
        _employeeRepository = employeeRepository;
        _absenceTypeRepository = absenceTypeRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _timeRegisterRepository = timeRegisterRepository;
        _employeeAbsenceRepository = employeeAbsenceRepository;
        _employeeAbsAdjustmentRepository = employeeAbsAdjustmentRepository;
        _employeeAbsQuotaForfeitRepository = employeeAbsQuotaForfeitRepository;
        _employeeSeparationRepository = employeeSeparationRepository;
        _salaryRegisterRepository = salaryRegisterRepository;
        _salaryRegisterDetailRepository = salaryRegisterDetailRepository;
        _wageTypeLeaveRepository = wageTypeLeaveRepository;
        _orgContextResolver = orgContextResolver;
    }

    public async Task<List<YearlyLeaveIndemnityDetailReportRowDto>> BuildRowsAsync(
        GetYearlyLeaveIndemnityDetailReportInput input,
        DateTime today,
        bool useArabicNames,
        ForfeitRule forfeitRule,
        DateTime earliestEvaluationDate,
        Func<Task<List<int>>> getRlsEmployeeIdsAsync)
    {
        var endDate = input.ResolveToDate(today);
        var startDate = new DateTime(endDate.Year, 1, 1);
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await getRlsEmployeeIdsAsync();

        var employeeQuery = _employeeRepository.GetAll()
            .WhereIf(input.EmployeeStatus.HasValue, e => e.Status == input.EmployeeStatus.Value)
            .WhereIf(employeeCodes.Count > 0, e => employeeCodes.Contains(e.Code))
            .WhereIf(rlsEmployeeIds.Count > 0, e => rlsEmployeeIds.Contains(e.Id));

        if (!string.IsNullOrWhiteSpace(input.PayrollAreaCode))
        {
            var payrollAreaEmployeeIds = _employeeOrgAssignmentRepository.GetAll()
                .Where(o => o.StartDate <= today && o.EndDate >= today
                            && o.PayrollArea != null && o.PayrollArea.Code == input.PayrollAreaCode)
                .Select(o => o.EmployeeId);
            employeeQuery = employeeQuery.Where(e => payrollAreaEmployeeIds.Contains(e.Id));
        }

        var employees = await employeeQuery
            .Select(e => new { e.Id, e.Code, EmployeeName = useArabicNames ? e.FullNameAr : e.FullNameEn, e.Status, e.JoiningDate })
            .OrderBy(e => e.Code)
            .ToListAsync();
        var employeeIds = employees.Select(e => e.Id).ToList();

        var annualLeaveType = await _absenceTypeRepository.GetAll().Where(a => a.Code == "1").FirstOrDefaultAsync();
        var annualLeaveTypeIds = await _absenceTypeRepository.GetAll()
            .Where(a => a.Code == "1" || a.QuotaType == QuotaType.AnnualLeave)
            .Select(a => a.Id)
            .ToListAsync();

        var openingDate = startDate.AddDays(-1);

        var orgContextsOpening = await _orgContextResolver.BuildAsync(employeeIds, openingDate, useArabicNames);
        var orgContextsCurrent = await _orgContextResolver.BuildAsync(employeeIds, today, useArabicNames);

        var lastDayInService = (await _employeeSeparationRepository.GetAll()
                .Where(s => employeeIds.Contains(s.EmployeeId))
                .Select(s => new { s.EmployeeId, s.LastDayInService })
                .ToListAsync())
            .GroupBy(s => s.EmployeeId)
            .ToDictionary(g => g.Key, g => g.Max(s => s.LastDayInService));

        var openingAccrual = await _timeRegisterRepository.GetAll()
            .Where(t => employeeIds.Contains(t.EmployeeId) && t.DayDate >= earliestEvaluationDate && t.DayDate <= openingDate)
            .GroupBy(t => t.EmployeeId)
            .Select(g => new { EmployeeId = g.Key, Total = g.Sum(t => t.AnnualLeaveAccrualPerDay) })
            .ToDictionaryAsync(g => g.EmployeeId, g => g.Total);

        var openingUsed = await _employeeAbsenceRepository.GetAll()
            .Where(a => employeeIds.Contains(a.EmployeeId) && annualLeaveTypeIds.Contains(a.AbsenceTypeId)
                        && a.StartDate >= earliestEvaluationDate && a.EndDate <= openingDate)
            .GroupBy(a => a.EmployeeId)
            .Select(g => new { EmployeeId = g.Key, Total = g.Sum(a => a.TotalLeaveDays) })
            .ToDictionaryAsync(g => g.EmployeeId, g => g.Total);

        var openingAdjust = await _employeeAbsAdjustmentRepository.GetAll()
            .Where(a => employeeIds.Contains(a.EmployeeId) && annualLeaveTypeIds.Contains(a.AbsenceTypeId)
                        && a.EffectiveDate >= earliestEvaluationDate && a.EffectiveDate <= openingDate)
            .GroupBy(a => a.EmployeeId)
            .Select(g => new { EmployeeId = g.Key, Total = g.Sum(a => a.QuotaAdjusted) })
            .ToDictionaryAsync(g => g.EmployeeId, g => g.Total);

        var openingForfeited = await _employeeAbsQuotaForfeitRepository.GetAll()
            .Where(f => employeeIds.Contains(f.EmployeeId) && annualLeaveTypeIds.Contains(f.AbsenceTypeId)
                        && f.EffectiveDate >= earliestEvaluationDate && f.EffectiveDate <= openingDate)
            .GroupBy(f => f.EmployeeId)
            .Select(g => new { EmployeeId = g.Key, Total = g.Sum(f => f.QuotaForfeited) })
            .ToDictionaryAsync(g => g.EmployeeId, g => g.Total);

        var currentAccrual = await _timeRegisterRepository.GetAll()
            .Where(t => employeeIds.Contains(t.EmployeeId) && t.DayDate >= startDate && t.DayDate <= endDate)
            .GroupBy(t => t.EmployeeId)
            .Select(g => new { EmployeeId = g.Key, Total = g.Sum(t => t.AnnualLeaveAccrualPerDay) })
            .ToDictionaryAsync(g => g.EmployeeId, g => g.Total);

        var currentUsed = await _employeeAbsenceRepository.GetAll()
            .Where(a => employeeIds.Contains(a.EmployeeId) && annualLeaveTypeIds.Contains(a.AbsenceTypeId)
                        && a.StartDate >= startDate && a.EndDate <= endDate)
            .GroupBy(a => a.EmployeeId)
            .Select(g => new { EmployeeId = g.Key, Total = g.Sum(a => a.TotalLeaveDays) })
            .ToDictionaryAsync(g => g.EmployeeId, g => g.Total);

        var currentAdjust = await _employeeAbsAdjustmentRepository.GetAll()
            .Where(a => employeeIds.Contains(a.EmployeeId) && annualLeaveTypeIds.Contains(a.AbsenceTypeId)
                        && a.EffectiveDate >= startDate && a.EffectiveDate <= endDate)
            .GroupBy(a => a.EmployeeId)
            .Select(g => new { EmployeeId = g.Key, Total = g.Sum(a => a.QuotaAdjusted) })
            .ToDictionaryAsync(g => g.EmployeeId, g => g.Total);

        var currentForfeited = await _employeeAbsQuotaForfeitRepository.GetAll()
            .Where(f => employeeIds.Contains(f.EmployeeId) && annualLeaveTypeIds.Contains(f.AbsenceTypeId)
                        && f.EffectiveDate >= startDate && f.EffectiveDate <= endDate)
            .GroupBy(f => f.EmployeeId)
            .Select(g => new { EmployeeId = g.Key, Total = g.Sum(f => f.QuotaForfeited) })
            .ToDictionaryAsync(g => g.EmployeeId, g => g.Total);

        var includeInPaidLeaveWageTypeIds = annualLeaveType == null
            ? new List<int>()
            : await _wageTypeLeaveRepository.GetAll()
                .Where(l => l.AbsenceTypeId == annualLeaveType.Id)
                .Select(l => l.WageTypeId)
                .Distinct()
                .ToListAsync();

        var latestRegisters = await _salaryRegisterRepository.GetAll()
            .Where(r => employeeIds.Contains(r.EmployeeId) && r.PayrollRunType == PayrollRunType.Regular
                        && r.InPeriod == r.ForPeriod && r.InPeriod <= endDate && r.ForPeriod <= endDate)
            .GroupBy(r => r.EmployeeId)
            .Select(g => new { EmployeeId = g.Key, MaxInPeriod = g.Max(r => r.InPeriod) })
            .ToListAsync();

        var latestRegisterIds = await _salaryRegisterRepository.GetAll()
            .Where(r => employeeIds.Contains(r.EmployeeId) && r.PayrollRunType == PayrollRunType.Regular
                        && r.InPeriod == r.ForPeriod && r.InPeriod <= endDate && r.ForPeriod <= endDate)
            .Select(r => new { r.Id, r.EmployeeId, r.InPeriod, r.ForPeriod })
            .ToListAsync();

        var latestRegisterByEmployee = new Dictionary<int, (int RegisterId, DateTime PayDate)>();
        foreach (var period in latestRegisters)
        {
            var match = latestRegisterIds.FirstOrDefault(r => r.EmployeeId == period.EmployeeId && r.InPeriod == period.MaxInPeriod);
            if (match != null)
            {
                latestRegisterByEmployee[period.EmployeeId] = (match.Id, match.ForPeriod);
            }
        }

        var registerIds = latestRegisterByEmployee.Values.Select(v => v.RegisterId).ToList();
        var registerDetails = registerIds.Count == 0
            ? new List<(int RegisterId, string Code, double Amount, double Number)>()
            : (await _salaryRegisterDetailRepository.GetAll()
                .Where(d => registerIds.Contains(d.SalaryRegisterId) && (d.WageType.Code == "3003" || includeInPaidLeaveWageTypeIds.Contains(d.WageTypeId)))
                .Select(d => new { d.SalaryRegisterId, d.WageType.Code, d.Amount, d.Number, d.Rate })
                .ToListAsync())
                .Select(d => (RegisterId: d.SalaryRegisterId, Code: d.Code, Amount: d.Amount, Number: d.Number))
                .ToList();

        var registerRates = registerIds.Count == 0
            ? new List<(int RegisterId, int WageTypeId, double Rate)>()
            : (await _salaryRegisterDetailRepository.GetAll()
                .Where(d => registerIds.Contains(d.SalaryRegisterId) && includeInPaidLeaveWageTypeIds.Contains(d.WageTypeId))
                .Select(d => new { d.SalaryRegisterId, d.WageTypeId, d.Rate })
                .ToListAsync())
                .Select(d => (RegisterId: d.SalaryRegisterId, WageTypeId: d.WageTypeId, Rate: d.Rate))
                .ToList();

        var rows = new List<YearlyLeaveIndemnityDetailReportRowDto>();
        foreach (var employee in employees)
        {
            var openingOrgContext = orgContextsOpening.Get(employee.Id);
            var currentOrgContext = orgContextsCurrent.Get(employee.Id);

            openingAccrual.TryGetValue(employee.Id, out var empOpeningAccrual);
            openingUsed.TryGetValue(employee.Id, out var empOpeningUsed);
            openingAdjust.TryGetValue(employee.Id, out var empOpeningAdjust);
            openingForfeited.TryGetValue(employee.Id, out var empOpeningForfeited);
            var openingBalance = empOpeningAccrual - empOpeningUsed - empOpeningAdjust - empOpeningForfeited;

            currentAccrual.TryGetValue(employee.Id, out var empCurrentAccrual);
            currentUsed.TryGetValue(employee.Id, out var empCurrentUsed);
            currentAdjust.TryGetValue(employee.Id, out var empCurrentAdjust);
            currentForfeited.TryGetValue(employee.Id, out var empCurrentForfeited);

            var closingBalance = Math.Round(openingBalance + empCurrentAccrual - empCurrentUsed - empCurrentAdjust - empCurrentForfeited, LeaveRoundoff);

            double salaryInProvision = 0;
            double closingProvision = 0;
            double indemnityClosingBalance = 0;
            double indemnityClosingBalanceAmount = 0;

            if (latestRegisterByEmployee.TryGetValue(employee.Id, out var latestRegister))
            {
                var totalBasicPay = registerRates.Where(r => r.RegisterId == latestRegister.RegisterId).Sum(r => r.Rate);
                salaryInProvision = totalBasicPay;

                indemnityClosingBalance = registerDetails.Where(d => d.RegisterId == latestRegister.RegisterId && d.Code == "3003").Select(d => d.Number).FirstOrDefault();
                indemnityClosingBalanceAmount = registerDetails.Where(d => d.RegisterId == latestRegister.RegisterId && d.Code == "3003").Select(d => d.Amount).FirstOrDefault();

                if (annualLeaveType?.WageTypeId != null)
                {
                    var payDate = latestRegister.PayDate;
                    var calendarDays = DateTime.DaysInMonth(payDate.Year, payDate.Month);
                    // BillingMethod/FixedDays need the AbsenceType's linked WageType; resolved once per report
                    // run by the caller via includeInPaidLeaveWageTypeIds (rate) -- the per-employee
                    // provision amount mirrors the flat CalendarDaysPerMonth path used by every other report
                    // in this wave when the specific WageType lookup is unavailable at this call site.
                    closingProvision = calendarDays != 0 ? totalBasicPay / calendarDays * closingBalance : 0;
                }
            }

            lastDayInService.TryGetValue(employee.Id, out var empLastDayInService);

            rows.Add(new YearlyLeaveIndemnityDetailReportRowDto
            {
                Id = employee.Id,
                EmployeeCode = employee.Code,
                EmployeeName = employee.EmployeeName,
                Status = employee.Status.ToString(),
                JoiningDate = employee.JoiningDate,
                LastDayInService = empLastDayInService == default ? null : empLastDayInService,
                JobTitleName = currentOrgContext.JobTitleName,
                OrgLevel1 = currentOrgContext.OrgLevel1,
                OrgLevel2 = currentOrgContext.OrgLevel2,
                OrgLevel3 = currentOrgContext.OrgLevel3,
                OrgLevel4 = currentOrgContext.OrgLevel4,
                OrgLevel5 = currentOrgContext.OrgLevel5,
                OrgLevel6 = currentOrgContext.OrgLevel6,
                OrgLevel7 = currentOrgContext.OrgLevel7,
                CostCenterCode = openingOrgContext.CostCenterCode,
                CostCenterName = openingOrgContext.CostCenterName,
                CostCenterGroupName = openingOrgContext.CostCenterGroupName,
                TotalDaysAllowed = annualLeaveType?.TotalDaysAllowed ?? 0,
                OpeningBalance = Math.Round(openingBalance, LeaveRoundoff),
                AccruedQuota = Math.Round(empCurrentAccrual, LeaveRoundoff),
                UsedQuota = Math.Round(empCurrentUsed, LeaveRoundoff),
                AdjustedQuota = Math.Round(empCurrentAdjust, LeaveRoundoff),
                ForfeitQuota = forfeitRule == ForfeitRule.Monthly ? Math.Round(empCurrentForfeited, LeaveRoundoff) : 0,
                SalaryInProvision = salaryInProvision,
                ClosingBalance = closingBalance,
                ClosingProvision = closingProvision,
                IndemnityClosingBalance = indemnityClosingBalance,
                IndemnityClosingBalanceAmount = indemnityClosingBalanceAmount
            });
        }

        return rows;
    }
}
