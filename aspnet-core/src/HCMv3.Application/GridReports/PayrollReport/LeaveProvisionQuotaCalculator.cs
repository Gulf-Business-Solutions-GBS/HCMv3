using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSettings.Services;
using HCMv3.TimeManagement;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// SHARED CORE of the three "quota accrual difference" Leave Provision variants of legacy
/// <c>uscRptPayrollJV_Custom042.ascx.cs</c> -- "Public Holiday Leave Provision JV" (lines 1928-2216),
/// "Ramadan Time Back Leave Provision JV" (lines 2217-2503) and "Lieu Day Leave Provision JV" (lines
/// 2505-2791). All three branches are byte-for-byte identical except for three constants (the leave's
/// <c>AbsenceType.Code</c>, the per-month provision wage-type code and the "Total" wage-type code), so
/// this class is that shared body -- once -- exactly the way <see cref="PayrollJVPostingLineLoader"/> is
/// shared for the wage-account-driven variants.
///
/// ALGORITHM (per employee): compute the leave QUOTA the employee accrued as of last month-end
/// (Prev_QuotaAccrued - Prev_QuotaUsed - Prev_QuotaAdjusted) and as of this month-end (the Curr_* triple),
/// take the DIFFERENCE, and -- when positive -- provision <c>Difference * Rate</c>, where Rate is the sum
/// of the employee's salary-register lines on wage types linked to the leave type (<c>WageTypeLeave</c>)
/// divided by a per-day rate basis that depends on the provision wage type's <c>BillingMethod</c>.
///
/// SIMPLIFICATION FLAGGED: legacy's <c>WorkingDaysPerMonth</c> billing method calls
/// <c>Program.GetActualWeekOffs(object_Space, null, null, EndDate, PayrollRunType.Regular, EndDate, null)</c>
/// with a NULL employee -- an organisation-wide weekly-off count with a duty-period day-by-day back-fill
/// that has no direct HCMv3 port exposed at that (employee-less) granularity (the closest port,
/// <c>EmployeeLeavePayCalculationService</c>'s private <c>GetActualWeekOffsAsync</c>, is per-employee and
/// not shared). This port instead treats <c>WorkingDaysPerMonth</c> the same as
/// <c>CalendarDaysPerMonth</c> (weekly-off days = 0), which is exact whenever the tenant configures these
/// specific provision wage types with <c>FixedDaysPerMonth</c> or <c>CalendarDaysPerMonth</c> (the common
/// configuration for this wage-type family) and only diverges for a tenant that configures
/// <c>WorkingDaysPerMonth</c>. Flagged for a follow-up wave.
/// </summary>
public class LeaveProvisionQuotaCalculator : ITransientDependency
{
    private readonly IRepository<AbsenceType> _absenceTypeRepository;
    private readonly IRepository<WageType> _wageTypeRepository;
    private readonly IRepository<WageTypeLeave> _wageTypeLeaveRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeAttendance> _employeeAttendanceRepository;
    private readonly IRepository<TimeRegister> _timeRegisterRepository;
    private readonly IRepository<EmployeeAbsAdjustment> _employeeAbsAdjustmentRepository;
    private readonly IRepository<SalaryRegisterDetail> _salaryRegisterDetailRepository;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;

    public LeaveProvisionQuotaCalculator(
        IRepository<AbsenceType> absenceTypeRepository,
        IRepository<WageType> wageTypeRepository,
        IRepository<WageTypeLeave> wageTypeLeaveRepository,
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeAttendance> employeeAttendanceRepository,
        IRepository<TimeRegister> timeRegisterRepository,
        IRepository<EmployeeAbsAdjustment> employeeAbsAdjustmentRepository,
        IRepository<SalaryRegisterDetail> salaryRegisterDetailRepository,
        ISysSettingsAccessor sysSettingsAccessor)
    {
        _absenceTypeRepository = absenceTypeRepository;
        _wageTypeRepository = wageTypeRepository;
        _wageTypeLeaveRepository = wageTypeLeaveRepository;
        _employeeRepository = employeeRepository;
        _employeeAttendanceRepository = employeeAttendanceRepository;
        _timeRegisterRepository = timeRegisterRepository;
        _employeeAbsAdjustmentRepository = employeeAbsAdjustmentRepository;
        _salaryRegisterDetailRepository = salaryRegisterDetailRepository;
        _sysSettingsAccessor = sysSettingsAccessor;
    }

    public async Task<LeaveProvisionQuotaContext> ResolveContextAsync(
        DateTime endDate, string leaveAbsenceTypeCode, string provisionWageTypeCode, string totalProvisionWageTypeCode)
    {
        var absType = await _absenceTypeRepository.GetAll()
            .Where(a => a.Code == leaveAbsenceTypeCode)
            .Select(a => new { a.NameEn, a.WageTypeId })
            .FirstOrDefaultAsync();

        var leaveTypeName = absType?.NameEn ?? string.Empty;

        var provisionWageType = await _wageTypeRepository.GetAll()
            .Where(w => w.Code == provisionWageTypeCode)
            .FirstOrDefaultAsync();

        var totalWageType = await _wageTypeRepository.GetAll()
            .Where(w => w.Code == totalProvisionWageTypeCode)
            .FirstOrDefaultAsync();

        double perDayRateBasis = 0;
        if (provisionWageType != null)
        {
            if (provisionWageType.BillingMethod == BillingMethod.FixedDaysPerMonth)
            {
                perDayRateBasis = provisionWageType.FixedDays;
            }
            else
            {
                // CalendarDaysPerMonth, and WorkingDaysPerMonth per the class-level SIMPLIFICATION note.
                perDayRateBasis = DateTime.DaysInMonth(endDate.Year, endDate.Month);
            }
        }

        var includeInLeaveWageTypeIds = await (
                from wtl in _wageTypeLeaveRepository.GetAll()
                where wtl.AbsenceType.NameEn == leaveTypeName
                select wtl.WageTypeId)
            .Distinct()
            .ToListAsync();

        return new LeaveProvisionQuotaContext
        {
            LeaveTypeName = leaveTypeName,
            ProvisionWageTypeId = provisionWageType?.Id,
            ProvisionWageTypeCode = provisionWageType?.Code,
            ProvisionWageTypeName = provisionWageType?.NameEn,
            ProvisionWageTypeBillingType = provisionWageType?.BillingType ?? BillingType.Provision,
            TotalWageTypeName = totalWageType?.NameEn,
            PerDayRateBasis = perDayRateBasis,
            IncludeInLeaveWageTypeIds = includeInLeaveWageTypeIds
        };
    }

    /// <summary>
    /// Legacy lines 2040-2102 (Public Holiday; byte-identical in the other two branches): the per-employee
    /// quota-accrual difference and the amount to provision for those with a positive difference.
    /// </summary>
    public async Task<List<(int EmployeeId, double Amount)>> CalculateAmountsAsync(
        LeaveProvisionQuotaContext context,
        DateTime endDate,
        DateTime earliestEvaluationDate,
        IReadOnlyCollection<int> rlsEmployeeIds,
        IReadOnlyCollection<string> employeeCodes)
    {
        var endDatePrevMonth = new DateTime(endDate.Year, endDate.Month, 1).AddDays(-1);
        var codes = (employeeCodes ?? Array.Empty<string>()).ToArray();
        var rlsIds = (rlsEmployeeIds ?? Array.Empty<int>()).ToArray();

        var employeeIds = await _employeeRepository.GetAll()
            .WhereIf(codes.Length > 0, e => codes.Contains(e.Code))
            .WhereIf(rlsIds.Length > 0, e => rlsIds.Contains(e.Id))
            .Select(e => e.Id)
            .ToListAsync();

        if (employeeIds.Count == 0 || context.ProvisionWageTypeId == null)
        {
            return new List<(int, double)>();
        }

        var attendance = await _employeeAttendanceRepository.GetAll()
            .Where(a => employeeIds.Contains(a.EmployeeId)
                        && a.AttendanceType.LinkLeaveQuotaId != null
                        && a.AttendanceType.LinkLeaveQuota.NameEn == context.LeaveTypeName)
            .Select(a => new { a.EmployeeId, a.StartDate, a.EndDate, a.TotalAttendDays })
            .ToListAsync();

        var timeRegister = await _timeRegisterRepository.GetAll()
            .Where(t => employeeIds.Contains(t.EmployeeId)
                        && t.LeaveTypeId != null
                        && _absenceTypeRepository.GetAll()
                            .Any(a => a.Id == t.LeaveTypeId && a.NameEn == context.LeaveTypeName))
            .Select(t => new { t.EmployeeId, t.DayDate, t.LeaveDay })
            .ToListAsync();

        var adjustments = await _employeeAbsAdjustmentRepository.GetAll()
            .Where(a => employeeIds.Contains(a.EmployeeId)
                        && a.AbsenceType.NameEn == context.LeaveTypeName
                        && a.AdjustmentType != AdjustmentType.NoImpactOnQuota)
            .Select(a => new { a.EmployeeId, a.EffectiveDate, a.QuotaAdjusted })
            .ToListAsync();

        // Legacy "Rate" (lines 2075-2076) sums SalaryRegisterDetail.Rate off the SAME <c>dsSalaryRegister</c>
        // filtered set used by the wage-account-driven variants (ForPeriod == EndDate, InPeriod == ForPeriod,
        // IncludeInFIPosting) -- reapplied here since this calculator does not share PayrollJVPostingLineLoader's
        // salary-line query (that one selects Amount, not Rate). NOTE: this port does not re-apply the
        // RunType/PayrollArea narrowing legacy's dsSalaryRegister also carries -- a documented simplification.
        var rateByEmployee = new Dictionary<int, double>();
        if (context.IncludeInLeaveWageTypeIds.Count > 0)
        {
            var rates = await _salaryRegisterDetailRepository.GetAll()
                .Where(d => employeeIds.Contains(d.SalaryRegister.EmployeeId)
                            && d.SalaryRegister.ForPeriod == endDate
                            && d.SalaryRegister.ForPeriod == d.SalaryRegister.InPeriod
                            && d.WageType.IncludeInFIPosting
                            && context.IncludeInLeaveWageTypeIds.Contains(d.WageTypeId))
                .GroupBy(d => d.SalaryRegister.EmployeeId)
                .Select(g => new { EmployeeId = g.Key, Rate = g.Sum(x => x.Rate) })
                .ToListAsync();

            foreach (var r in rates)
            {
                rateByEmployee[r.EmployeeId] = r.Rate;
            }
        }

        var results = new List<(int, double)>();

        foreach (var employeeId in employeeIds)
        {
            double prevAccrued = attendance.Where(a => a.EmployeeId == employeeId
                    && a.StartDate >= earliestEvaluationDate && a.EndDate <= endDatePrevMonth)
                .Sum(a => a.TotalAttendDays);
            double prevUsed = timeRegister.Where(t => t.EmployeeId == employeeId
                    && t.DayDate >= earliestEvaluationDate && t.DayDate <= endDatePrevMonth)
                .Sum(t => t.LeaveDay);
            double prevAdjusted = adjustments.Where(a => a.EmployeeId == employeeId
                    && a.EffectiveDate >= earliestEvaluationDate && a.EffectiveDate <= endDatePrevMonth)
                .Sum(a => a.QuotaAdjusted);

            double currAccrued = attendance.Where(a => a.EmployeeId == employeeId
                    && a.StartDate >= earliestEvaluationDate && a.EndDate <= endDate)
                .Sum(a => a.TotalAttendDays);
            // Legacy lines 2063-2065: the Curr_QuotaUsed upper bound is endDatePrevMonth too (a legacy
            // quirk -- NOT endDate). Reproduced verbatim.
            double currUsed = timeRegister.Where(t => t.EmployeeId == employeeId
                    && t.DayDate >= earliestEvaluationDate && t.DayDate <= endDatePrevMonth)
                .Sum(t => t.LeaveDay);
            double currAdjusted = adjustments.Where(a => a.EmployeeId == employeeId
                    && a.EffectiveDate >= earliestEvaluationDate && a.EffectiveDate <= endDate)
                .Sum(a => a.QuotaAdjusted);

            var opening = prevAccrued - prevUsed - prevAdjusted;
            var closing = currAccrued - currUsed - currAdjusted;
            var difference = closing - opening;

            if (difference <= 0)
            {
                continue;
            }

            rateByEmployee.TryGetValue(employeeId, out var totalRate);
            var rate = context.PerDayRateBasis == 0 ? 0 : totalRate / context.PerDayRateBasis;
            var amount = difference * rate;

            if (amount != 0)
            {
                results.Add((employeeId, amount));
            }
        }

        return results;
    }
}

public class LeaveProvisionQuotaContext
{
    public string LeaveTypeName { get; set; }

    public int? ProvisionWageTypeId { get; set; }

    public string ProvisionWageTypeCode { get; set; }

    public string ProvisionWageTypeName { get; set; }

    public BillingType ProvisionWageTypeBillingType { get; set; }

    public string TotalWageTypeName { get; set; }

    public double PerDayRateBasis { get; set; }

    public List<int> IncludeInLeaveWageTypeIds { get; set; } = new();
}
