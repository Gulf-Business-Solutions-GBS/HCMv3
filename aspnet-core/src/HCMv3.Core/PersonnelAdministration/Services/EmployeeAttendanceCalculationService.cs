using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.SystemSettings.Services;
using HCMv3.TimeManagement;

namespace HCMv3.PersonnelAdministration.Services;

/// <inheritdoc cref="IEmployeeAttendanceCalculationService"/>
/// <remarks>
/// Legacy line numbers below are in <c>HCM10.Module/BusinessObjects/PersonnelAdministration/EmployeeAttendance.cs</c>
/// unless stated otherwise. Ported as-is under the payroll-parity rule; the oddities kept are pinned by tests in
/// <c>EmployeeAttendanceCalculationService_Tests</c>:
/// <list type="bullet">
/// <item><description>The quota row is located through the employee's <b>JoiningDate</b>, not the attendance
/// date (lines 394-395, 419; the attendance date is commented out in legacy).</description></item>
/// <item><description>Giving back (lines 392-396) looks for the <b>linked</b> type's row whose
/// <b>entitlement</b> period covers the joining date; accruing (line 419, legacy
/// <c>EmployeeAbsenceQuota.GetEmployeeAbsenceQuota(emp, type, start, end, session)</c>) looks for the
/// <b>deduction</b> period, and swaps an annual-group type (<c>QuotaType.AnnualLeave</c>) for the Code "1"
/// type.</description></item>
/// <item><description>Giving back runs whenever the previous type has a linked leave type, even when that type
/// no longer has <c>HaveQuotaAccrual</c> (line 390 tests only <c>LinkLeaveQuota</c>).</description></item>
/// <item><description>No floor and no rounding on either side (lines 403, 431).</description></item>
/// <item><description>A missing row on accrual is CREATED (lines 421-422, <c>AddEmployeeAbsQuota</c>
/// 652-733); with <c>ReInitialize</c> on, legacy left its dates unset, so the next save cannot find it again
/// and creates another.</description></item>
/// </list>
/// <para>
/// One deliberate difference: legacy <c>OnDeleting</c> (lines 316-326) does not test
/// <c>recPrevAbsenceType != null</c> as the save path does (line 390), so deleting an attendance whose type has no
/// linked leave type fails with a NullReferenceException while formatting its own error message. That is a crash,
/// not a rule; this port does what the save path does and skips the give-back.
/// </para>
/// <para>
/// Not ported: <c>IsSystemGenerated = true</c> on the quota row (lines 432, 724) -- HCMv3's
/// <see cref="EmployeeAbsenceQuota"/> has no such column.
/// </para>
/// </remarks>
[UnitOfWork]
public class EmployeeAttendanceCalculationService : HCMv3DomainServiceBase, IEmployeeAttendanceCalculationService
{
    /// <summary>Legacy <c>AbsenceType.Code == "1"</c> for annual leave (EmployeeAbsenceQuota.cs GetEmployeeAbsenceQuota).</summary>
    private const string AnnualLeaveAbsenceTypeCode = "1";

    /// <summary>Legacy <c>DateTimeExtension.DBDtNull</c> -- still found on rows migrated from legacy.</summary>
    private static readonly DateTime LegacyDbDateNull = new(1800, 1, 1);

    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<AttendanceType> _attendanceTypeRepository;
    private readonly IRepository<AbsenceType> _absenceTypeRepository;
    private readonly IRepository<EmployeeAbsenceQuota> _quotaRepository;
    private readonly IRepository<BreakShift> _breakShiftRepository;
    private readonly ISysSettingsAccessor _settings;
    private readonly IEmployeeAbsenceCalculationService _absenceCalculation;

    public EmployeeAttendanceCalculationService(
        IRepository<Employee> employeeRepository,
        IRepository<AttendanceType> attendanceTypeRepository,
        IRepository<AbsenceType> absenceTypeRepository,
        IRepository<EmployeeAbsenceQuota> quotaRepository,
        IRepository<BreakShift> breakShiftRepository,
        ISysSettingsAccessor settings,
        IEmployeeAbsenceCalculationService absenceCalculation)
    {
        _employeeRepository = employeeRepository;
        _attendanceTypeRepository = attendanceTypeRepository;
        _absenceTypeRepository = absenceTypeRepository;
        _quotaRepository = quotaRepository;
        _breakShiftRepository = breakShiftRepository;
        _settings = settings;
        _absenceCalculation = absenceCalculation;
    }

    #region << TotalAttendDays >>

    public async Task ApplyTotalAttendDaysAsync(EmployeeAttendance attendance)
    {
        if (attendance == null)
        {
            throw new ArgumentNullException(nameof(attendance));
        }

        if (attendance.AttendanceUnit == AttendanceUnit.Period)
        {
            // Line 351: both times present -> hourly-based attendance on one day.
            if (!IsUnsetTime(attendance.StartTime) && !IsUnsetTime(attendance.EndTime))
            {
                // Line 353: the entered hours are overwritten by the times.
                attendance.AttendanceHours = Math.Round(attendance.EndTime.Subtract(attendance.StartTime).TotalHours, 3);

                var totalDays = await GetHoursToAttendanceDayAsync(attendance.EmployeeId, attendance.StartDate, attendance.AttendanceHours);

                // Lines 356-359.
                if (totalDays < 0)
                {
                    totalDays = 0;
                }
                else if (totalDays > 1)
                {
                    totalDays = 1;
                }

                attendance.TotalAttendDays = totalDays;
            }
            else
            {
                // Line 365.
                attendance.TotalAttendDays = attendance.EndDate.Subtract(attendance.StartDate).TotalDays + 1;
            }
        }
        else if (attendance.AttendanceUnit == AttendanceUnit.Hours)
        {
            // Lines 374-379: floored at 0 but -- unlike Period -- never capped at 1.
            var totalDays = await GetHoursToAttendanceDayAsync(attendance.EmployeeId, attendance.StartDate, attendance.AttendanceHours);
            if (totalDays < 0)
            {
                totalDays = 0;
            }

            attendance.TotalAttendDays = totalDays;
        }

        // AttendanceUnit.Day: lines 368-371 -- nothing (the calculation is commented out in legacy).
    }

    public async Task<double> GetHoursToAttendanceDayAsync(int employeeId, DateTime attendanceDayDate, double attendanceHours)
    {
        double attendanceDay = 0;

        var plannedMinutes = await GetPlannedMinutesAsync(employeeId, attendanceDayDate);
        var attendanceMinutes = attendanceHours * 60;

        // Line 475-476 (Zoho #2748): no planned minutes -> the payroll default working day.
        if (plannedMinutes == 0)
        {
            var payrollSettings = await _settings.GetPayrollSettingsAsync();
            plannedMinutes = (payrollSettings?.DefaultWorkingHours ?? 0) * 60;
        }

        // Lines 478-481.
        if (plannedMinutes > 0)
        {
            attendanceDay = Math.Round(attendanceMinutes / plannedMinutes, 3);
        }

        return attendanceDay;
    }

    public async Task<double> GetPlannedMinutesAsync(int employeeId, DateTime attendanceDayDate)
    {
        var date = attendanceDayDate.Date;

        // Lines 496-499.
        Duty duty;
        try
        {
            duty = await _absenceCalculation.GetEmployeeDutyAsync(employeeId, date);
        }
        catch (InvalidOperationException)
        {
            duty = null;
        }

        if (duty == null)
        {
            throw new UserFriendlyException(L("AttendanceDutyNotFoundForDate", FormatDate(date)));
        }

        // Lines 501-531 (AMK #2267).
        var timeSettings = await _settings.GetTimeSettingsAsync();
        if (timeSettings != null)
        {
            if (duty.IsWeeklyOff && !timeSettings.EnableAttendanceOnWeekOff)
            {
                throw new UserFriendlyException(L("AttendanceCannotStartOnWeekOff", FormatDate(date), duty.NameEn));
            }

            if (duty.IsRestDay && !timeSettings.EnableAttendanceOnRestDay)
            {
                throw new UserFriendlyException(L("AttendanceCannotStartOnRestDay", FormatDate(date), duty.NameEn));
            }
        }

        // Lines 533-536.
        var shiftStart = date + duty.StartTime;
        var shiftEnd = duty.IsNightShift ? date.AddDays(1) + duty.EndTime : date + duty.EndTime;

        double plannedMinutes;
        var breakShift = duty.BreakShiftId == null
            ? null
            : _breakShiftRepository.GetAll().FirstOrDefault(b => b.Id == duty.BreakShiftId.Value);

        if (breakShift == null)
        {
            // Line 556.
            plannedMinutes = (shiftEnd - shiftStart).TotalMinutes;
        }
        else
        {
            // Lines 537-551, 560-561.
            var breakStart = breakShift.IsBreakStartNextDay
                ? date.AddDays(1) + breakShift.StartTime
                : date + breakShift.StartTime;

            var breakEnd = (breakShift.IsBreakStartNextDay || breakShift.IsBreakEndNextDay)
                ? date.AddDays(1) + breakShift.EndTime
                : date + breakShift.EndTime;

            plannedMinutes = (breakStart - shiftStart).TotalMinutes;
            plannedMinutes += (shiftEnd - breakEnd).TotalMinutes;
        }

        // Lines 564-565.
        if (plannedMinutes < 0)
        {
            plannedMinutes = 0;
        }

        return plannedMinutes;
    }

    #endregion

    #region << Quota accrual >>

    public async Task ReleaseAttendanceQuotaAsync(AttendanceQuotaSnapshot previous)
    {
        if (previous == null)
        {
            return;
        }

        // Lines 387-389.
        var employee = _employeeRepository.GetAll().FirstOrDefault(e => e.Id == previous.EmployeeId);
        var previousType = _attendanceTypeRepository.GetAll().FirstOrDefault(t => t.Id == previous.AttendanceTypeId);
        var linkedTypeId = previousType?.LinkLeaveQuotaId;

        // Line 390 (and see class remarks for OnDeleting, which lacks this test).
        if (linkedTypeId == null || employee == null)
        {
            return;
        }

        // Lines 392-396: the ENTITLEMENT period covering the JOINING date, the linked type as-is.
        var joiningDate = employee.JoiningDate;
        var quota = _quotaRepository.GetAll()
            .Where(q => q.EmployeeId == previous.EmployeeId
                        && q.AbsenceTypeId == linkedTypeId.Value
                        && q.EntitlementFrom <= joiningDate
                        && q.EntitlementTo >= joiningDate)
            .OrderBy(q => q.Id)
            .FirstOrDefault();

        if (quota == null)
        {
            // Lines 398-399.
            var linkedType = _absenceTypeRepository.GetAll().FirstOrDefault(t => t.Id == linkedTypeId.Value);
            throw new UserFriendlyException(L("NoLeaveQuotaFoundForEmployeeAndAbsenceType",
                CodeWithFullNameEn(employee), linkedType?.NameEn));
        }

        // Line 403: no floor, no rounding.
        quota.QuotaAccrued = quota.QuotaAccrued - previous.TotalAttendDays;
        await _quotaRepository.UpdateAsync(quota);
    }

    public async Task AccrueAttendanceQuotaAsync(EmployeeAttendance attendance)
    {
        if (attendance == null)
        {
            throw new ArgumentNullException(nameof(attendance));
        }

        // Line 412.
        var attendanceType = _attendanceTypeRepository.GetAll().FirstOrDefault(t => t.Id == attendance.AttendanceTypeId);
        if (attendanceType == null || !attendanceType.HaveQuotaAccrual)
        {
            return;
        }

        // Lines 414-417.
        var absenceType = attendanceType.LinkLeaveQuotaId == null
            ? null
            : _absenceTypeRepository.GetAll().FirstOrDefault(t => t.Id == attendanceType.LinkLeaveQuotaId.Value);
        if (absenceType == null)
        {
            throw new UserFriendlyException(L("NoAbsenceTypeAssignedToAttendanceType", attendanceType.NameEn));
        }

        var employee = _employeeRepository.GetAll().FirstOrDefault(e => e.Id == attendance.EmployeeId);
        if (employee == null)
        {
            return;
        }

        // Line 419: legacy EmployeeAbsenceQuota.GetEmployeeAbsenceQuota(emp, type, JoiningDate, JoiningDate).
        // An EF tracking query hands back the instance a give-back above already changed, which is what legacy's
        // "same Oid -> use prevEmployeeAbsenceQuota" (lines 426-429) arranges explicitly.
        var quota = FindQuotaForAccrual(employee, absenceType);

        // Lines 421-422: a missing row is created, and line 431 then adds the days to its 0 accrual. Done before
        // the insert -- an update of a row not yet written would be sent as an UPDATE of Id 0.
        if (quota == null)
        {
            await AddEmployeeAbsQuotaAsync(employee, absenceType, attendance.TotalAttendDays);
            return;
        }

        // Line 431: no rounding.
        quota.QuotaAccrued = quota.QuotaAccrued + attendance.TotalAttendDays;
        await _quotaRepository.UpdateAsync(quota);
    }

    /// <summary>
    /// Legacy <c>EmployeeAbsenceQuota.GetEmployeeAbsenceQuota(Employee, AbsenceType, DateTime, DateTime, Session)</c>
    /// (EmployeeAbsenceQuota.cs): an annual-group type (<c>QuotaType.AnnualLeave</c>) is swapped for the Code "1"
    /// type, and the row is the one whose DEDUCTION period covers the date.
    /// </summary>
    private EmployeeAbsenceQuota FindQuotaForAccrual(Employee employee, AbsenceType absenceType)
    {
        var quotaTypeId = absenceType.QuotaType != QuotaType.AnnualLeave
            ? absenceType.Id
            : _absenceTypeRepository.GetAll().Where(t => t.Code == AnnualLeaveAbsenceTypeCode).Select(t => (int?)t.Id).FirstOrDefault();

        if (quotaTypeId == null)
        {
            return null;
        }

        var joiningDate = employee.JoiningDate;
        return _quotaRepository.GetAll()
            .Where(q => q.EmployeeId == employee.Id
                        && q.AbsenceTypeId == quotaTypeId.Value
                        && q.DeductionFrom <= joiningDate
                        && q.DeductionTo >= joiningDate)
            .OrderBy(q => q.Id)
            .FirstOrDefault();
    }

    /// <summary>Legacy <c>AddEmployeeAbsQuota</c> (lines 652-733).</summary>
    private async Task AddEmployeeAbsQuotaAsync(Employee employee, AbsenceType absenceType, double totalAttendDays)
    {
        // Line 658: the deduction start month is hard-coded to 0 in legacy (the setting is commented out).
        const int dedStartMonth = 0;
        var openEnded = new DateTime(9999, 12, 31);

        var quota = new EmployeeAbsenceQuota
        {
            TenantId = employee.TenantId,
            EmployeeId = employee.Id,
            AbsenceTypeId = absenceType.Id
        };

        // Line 663: with ReInitialize on, legacy set no dates at all.
        if (!absenceType.ReInitialize)
        {
            if (absenceType.AccrualPeriod == AccrualPeriod.WorkYear || absenceType.AccrualPeriod == AccrualPeriod.CalendarYear)
            {
                // Lines 666-689: both branches are identical in legacy.
                EnsureJoiningDate(employee);
                quota.EntitlementFrom = employee.JoiningDate.Date;
            }
            else if (absenceType.AccrualPeriod == AccrualPeriod.Custom)
            {
                // Lines 692-712.
                if (absenceType.CustomStartMonth == CustomStartMonth.None)
                {
                    throw new UserFriendlyException(L("CustomStartMonthIsNoneForLeaveType", absenceType.NameEn));
                }

                EnsureJoiningDate(employee);
                quota.EntitlementFrom = new DateTime(employee.JoiningDate.Year, (int)absenceType.CustomStartMonth, 1);
            }
            else
            {
                // Line 721.
                throw new UserFriendlyException(L("AbsenceTypeAccrualPeriodNotConfigured"));
            }

            quota.EntitlementTo = openEnded;
            quota.DeductionFrom = quota.EntitlementFrom.AddMonths(dedStartMonth);
            quota.DeductionTo = openEnded;
        }

        // Line 723: legacy did not seed QuotaAccrued here; it started at 0 and line 431 added TotalAttendDays.
        quota.QuotaAccrued = 0 + totalAttendDays;
        await _quotaRepository.InsertAsync(quota);
    }

    private void EnsureJoiningDate(Employee employee)
    {
        // Lines 668-669 / 681-682 / 697-698.
        if (employee.JoiningDate == default)
        {
            throw new UserFriendlyException(L("JoiningDateIsEmptyForEmployee", employee.Code));
        }
    }

    #endregion

    /// <summary>
    /// Legacy <c>!= DateTimeExtension.DBDtNull</c> (line 351). HCMv3 sends an unset time as <c>default</c>;
    /// rows migrated from legacy still carry the 1800-01-01 sentinel.
    /// </summary>
    private static bool IsUnsetTime(DateTime value) => value == default || value == LegacyDbDateNull;

    private static string FormatDate(DateTime date) =>
        date.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>Legacy XPO computed <c>Employee.CodeWithFullNameEn</c> = <c>Code + ' ' + FullNameEn</c>.</summary>
    private static string CodeWithFullNameEn(Employee employee)
    {
        return string.IsNullOrWhiteSpace(employee?.FullNameEn)
            ? employee?.Code
            : employee.Code + " " + employee.FullNameEn;
    }
}
