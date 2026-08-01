using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Services;
using HCMv3.PersonnelAdministration;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Services;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// <b>WAVE P9.</b> Legacy <c>Program.GetActualWeekOffs</c> (legacy <c>HCM10.PayrollEngine/Program.cs</c> line
/// 2858) -- the PAYROLL-ENGINE variant, which is NOT the XAF <c>AccrualService.GetActualWeekOffs</c>
/// (<see cref="IAccrualService.GetActualWeekOffsAsync(int, DateTime)"/>): the two differ in window, back-fill,
/// counting rule and failure mode.
///
/// <para><b>Why this type exists.</b> By wave P9 this body existed in the port THREE times: privately in
/// <c>PayrollDayCountService.GetPayrollActualWeekOffsAsync</c> (not exposed on
/// <see cref="IPayrollDayCountService"/>), privately in <see cref="EmployeeBasicPayCalculationService"/>, and
/// it was needed twice more by wave P9 (<see cref="EmployeeOvertimePayCalculationService"/> for
/// <c>BillingMethod.WorkingDaysPerMonth</c>, legacy <c>EmployeeOvertimePayService.cs</c> line 212, and
/// <see cref="EmployeePaymentCalculationService"/>, legacy <c>EmployeePaymentService.cs</c> line 358). Rather
/// than add a fourth and fifth copy, wave P9 factored ONE copy here and both new services use it.</para>
///
/// <para><b>Reported contract debt:</b> the right home is
/// <see cref="IPayrollDayCountService"/> -- promote <c>GetPayrollActualWeekOffsAsync</c> onto that interface
/// and collapse this type plus <see cref="EmployeeBasicPayCalculationService"/>'s private copy into it. That
/// was not done in wave P9 because <c>PayrollDayCountService</c>/<c>IPayrollDayCountService</c> are owned by a
/// concurrent wave.</para>
/// </summary>
public interface IPayrollWeekOffCalculator : IDomainService
{
    /// <summary>
    /// The number of weekly-off days in <paramref name="payDate"/>'s CALENDAR MONTH for the context's
    /// employee. Days that have a <c>TimeRegister</c> row are counted by
    /// <see cref="IPayrollDayCountService.GetWeeklyOffDaysAsync"/>; the remaining days are back-filled one at a
    /// time from the roster (when <see cref="PayrollEngineSettings.IsRosterActive"/>) or the employee's duty
    /// period.
    /// </summary>
    Task<double> GetActualWeekOffsAsync(PayrollCalculationContext context, DateTime payDate);
}

/// <inheritdoc cref="IPayrollWeekOffCalculator"/>
public class PayrollWeekOffCalculator : HCMv3DomainServiceBase, IPayrollWeekOffCalculator, ITransientDependency
{
    private readonly IRepository<EmployeeDutyPeriod> _employeeDutyPeriodRepository;
    private readonly IRepository<TimeRegister> _timeRegisterRepository;
    private readonly IPayrollDayCountService _dayCountService;
    private readonly IRosterService _rosterService;

    public PayrollWeekOffCalculator(
        IRepository<EmployeeDutyPeriod> employeeDutyPeriodRepository,
        IRepository<TimeRegister> timeRegisterRepository,
        IPayrollDayCountService dayCountService,
        IRosterService rosterService)
    {
        _employeeDutyPeriodRepository = employeeDutyPeriodRepository;
        _timeRegisterRepository = timeRegisterRepository;
        _dayCountService = dayCountService;
        _rosterService = rosterService;
    }

    /// <inheritdoc/>
    public async Task<double> GetActualWeekOffsAsync(PayrollCalculationContext context, DateTime payDate)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var startDate = new DateTime(payDate.Year, payDate.Month, 1);
        var endDate = new DateTime(payDate.Year, payDate.Month, DateTime.DaysInMonth(payDate.Year, payDate.Month));
        double daysInMonth = DateTime.DaysInMonth(payDate.Year, payDate.Month);

        var query = context.TimeRegisterQuery ?? _timeRegisterRepository.GetAll();
        var daysWithTimeRegister = query
            .Where(r => r.EmployeeId == context.EmployeeId && r.DayDate >= startDate && r.DayDate <= endDate)
            .Select(r => r.DayDate)
            .ToList();

        // Legacy line 2871 passes EndDate as BOTH payDate and offCycleEndDate.
        var weeklyOffDays = await _dayCountService.GetWeeklyOffDaysAsync(
            context.EmployeeId, endDate, context.PayrollRunType, endDate, context.OffCycleReason);

        if (Math.Abs(daysInMonth - daysWithTimeRegister.Count) <= double.Epsilon)
        {
            return weeklyOffDays;
        }

        List<EmployeeDutyPeriod> employeeDutyPeriods = null;

        for (var dayDate = startDate; dayDate <= endDate; dayDate = dayDate.AddDays(1))
        {
            if (daysWithTimeRegister.Contains(dayDate))
            {
                continue;
            }

            if (context.Settings.IsRosterActive)
            {
                var rosterDuty = await _rosterService.GetRosterDutyAsync(context.EmployeeId, dayDate, null, false);
                weeklyOffDays += rosterDuty != null && rosterDuty.IsWeeklyOff ? 1 : 0;
                continue;
            }

            employeeDutyPeriods ??= _employeeDutyPeriodRepository.GetAll()
                .Where(r => r.EmployeeId == context.EmployeeId)
                .ToList();

            var dutyPeriod = employeeDutyPeriods
                .Where(r => r.StartDate <= dayDate && r.EndDate >= dayDate)
                .Select(r => r.DutyPeriod)
                .FirstOrDefault();

            if (dutyPeriod == null)
            {
                throw new InvalidOperationException(
                    $"DutyPeriod not Found for Employee Id '{context.EmployeeId}' For '{dayDate:dd/MM/yyyy}'");
            }

            var duty = GetDutyForDayOfWeek(dutyPeriod, dayDate);
            weeklyOffDays += duty != null && duty.IsWeeklyOff ? 1 : 0;
        }

        return weeklyOffDays;
    }

    private static Duty GetDutyForDayOfWeek(DutyPeriod dutyPeriod, DateTime dayDate)
    {
        return dayDate.DayOfWeek switch
        {
            DayOfWeek.Sunday => dutyPeriod.SundayDuty,
            DayOfWeek.Monday => dutyPeriod.MondayDuty,
            DayOfWeek.Tuesday => dutyPeriod.TuesdayDuty,
            DayOfWeek.Wednesday => dutyPeriod.WednesdayDuty,
            DayOfWeek.Thursday => dutyPeriod.ThursdayDuty,
            DayOfWeek.Friday => dutyPeriod.FridayDuty,
            _ => dutyPeriod.SaturdayDuty
        };
    }
}
