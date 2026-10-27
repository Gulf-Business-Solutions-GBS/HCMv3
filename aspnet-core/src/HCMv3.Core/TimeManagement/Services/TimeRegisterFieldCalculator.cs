using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;

namespace HCMv3.TimeManagement.Services;

/// <inheritdoc cref="ITimeRegisterFieldCalculator"/>
/// <remarks>
/// Ported 1:1 from legacy <c>HCM10.TimeEngine.GeneralTimeService</c> (a 1,197-line static class).
/// Split across partial files by legacy <c>#region</c>:
/// <list type="bullet">
/// <item><description><c>TimeRegisterFieldCalculator.cs</c> -- ctor/fields, "General Calculation" and
/// "Planned Calculation" regions.</description></item>
/// <item><description><c>TimeRegisterFieldCalculator.Leave.cs</c> -- "Leave Calculation",
/// "AccrualPerDay Calculation", "AirTicket Calculation".</description></item>
/// <item><description><c>TimeRegisterFieldCalculator.Attendance.cs</c> -- "Attendance Calculation".</description></item>
/// <item><description><c>TimeRegisterFieldCalculator.Deductions.cs</c> -- "LateInEarlyOut Calculation".</description></item>
/// <item><description><c>TimeRegisterFieldCalculator.Actual.cs</c> -- "Actual Calculation",
/// "AbsentDay Calculation".</description></item>
/// <item><description><c>TimeRegisterFieldCalculator.Overtime.cs</c> -- "Overtime Calculation".</description></item>
/// <item><description><c>TimeRegisterFieldCalculator.Helpers.cs</c> -- the legacy <c>Program</c> static helpers
/// GeneralTimeService called into (<c>HasPunchIn</c>/<c>HasPunchOut</c>/<c>HasBreakPunchIn</c>/
/// <c>HasBreakPunchOut</c>/<c>IsFullDayHours</c>/<c>GetTotalFullDayHours</c>/<c>LeaveAtPunchIn</c>/
/// <c>LeaveAtPunchOut</c>/<c>GetAbsenceType</c>), plus entity-resolution shims.</description></item>
/// </list>
///
/// <para><b>DELIBERATE DEVIATIONS FROM LEGACY -- read before extending.</b></para>
/// <list type="number">
/// <item><description><b>Legacy <c>Program</c> static helpers are re-hosted as private helpers here</b>
/// (<c>TimeRegisterFieldCalculator.Helpers.cs</c>) rather than on the orchestrator. They are not on any ported
/// interface, and <c>TimeEngineService</c> (the port of legacy <c>Program</c>) has no equivalent members --
/// verified by search. Wave T3's punch strategies will need the same four <c>HasPunch*</c> predicates; when
/// they land, these should be promoted to a shared internal helper rather than copied a third time.
/// <b>Reported, not silently duplicated.</b></description></item>
/// <item><description><b>"No punch" sentinel</b>: legacy <c>DateTimeExtension.DBDtNull</c> is
/// <c>new DateTime(1800,1,1)</c>; this port uses <c>default(DateTime)</c>, per the convention already fixed by
/// <see cref="IPunchResolutionStrategy"/>'s remarks and by <c>TimeEngineService.ApplyDayFieldsAsync</c>
/// (which assigns <c>default</c> when punches are not resolved).</description></item>
/// <item><description><b>Time-of-day-only <c>DateTime</c> columns</b>: legacy
/// <c>DateTimeExtension.GetTime()</c> returned <c>new DateTime(1800,1,1,h,m,s)</c> -- i.e. the DBDtNull date
/// part plus the time. Following deviation 2, this port composes them on <c>default(DateTime).Date</c>
/// (0001-01-01). Affects only <see cref="EmployeeAttendance.StartTime"/>/<see cref="EmployeeAttendance.EndTime"/>
/// written by <c>CalculateAutoAttendanceAsync</c>.</description></item>
/// <item><description><b>Navigation properties are not assumed loaded.</b> HCMv3.Core has no EF Core reference
/// (no <c>.Include()</c>), and the orchestrator pre-loads its lists with plain <c>IRepository&lt;T&gt;.GetAll()</c>.
/// Everywhere legacy walked a navigation property on a pre-loaded row (<c>r.AbsenceType.Oid</c>,
/// <c>r.AttendanceType</c>, <c>r.Holiday.OvertimeType</c>, <c>r.TicketType.Code</c>), this port resolves the
/// row by FK id against <see cref="TimeEngineMasterData"/> first and an injected repository second --
/// see the <c>Resolve*</c> helpers. The ONE exception is <see cref="Duty.BreakShift"/>, which stays a
/// navigation-property null-check because <c>TimeEngineService.ApplyDayFieldsAsync</c> already computes
/// <see cref="TimeRegister.BreakStartTime"/>/<see cref="TimeRegister.BreakEndTime"/> from that same navigation
/// property -- diverging here would make the two disagree.</description></item>
/// <item><description><b><c>TimeRegister.LeaveType</c>/<c>AttendanceType</c> are bare FK ids in this port</b>
/// (documented on <see cref="TimeRegister"/>), so every legacy <c>timeRegister.LeaveType.X</c> becomes
/// <c>ResolveAbsenceType(context, timeRegister.LeaveTypeId)?.X</c>. Where legacy would have thrown a
/// <c>NullReferenceException</c> on an unresolvable id, this port throws an explicit
/// <see cref="InvalidOperationException"/> naming the id -- same failure, better diagnostics.</description></item>
/// <item><description><b><c>AbsenceType.WageType</c>/<c>AttendanceType.WageType</c> have no navigation property</b>
/// in this port (only <c>WageTypeId</c>), so <c>GetIncludeInLeaveDayAsync</c> resolves the WageType from
/// <see cref="TimeEngineMasterData.WageTypes"/>.</description></item>
/// <item><description><b>SysCreator/SysCreatedOn/SysModifier/SysModifiedOn and <c>IsSystemGenerated</c> NOT
/// ported</b> -- same decision already documented on <c>TimeEngineService</c> and <c>AccrualService</c>
/// (ABP <c>FullAuditedEntity</c> auditing is automatic; no ported entity carries <c>IsSystemGenerated</c>).</description></item>
/// <item><description><b>No EF Core in HCMv3.Core</b> -- all queries use the synchronous <c>IQueryable</c> from
/// <c>IRepository&lt;T&gt;.GetAll()</c>; async signatures are kept for interface consistency. Same constraint
/// documented at <c>AccrualService.cs</c> line ~164.</description></item>
/// </list>
/// </remarks>
[UnitOfWork]
public partial class TimeRegisterFieldCalculator : HCMv3DomainServiceBase, ITimeRegisterFieldCalculator
{
    private readonly IRepository<HolidayPeriod> _holidayPeriodRepository;
    private readonly IRepository<Holiday> _holidayRepository;
    private readonly IRepository<AbsenceType> _absenceTypeRepository;
    private readonly IRepository<AttendanceType> _attendanceTypeRepository;
    private readonly IRepository<WageType> _wageTypeRepository;
    private readonly IRepository<EmployeeAttendance> _employeeAttendanceRepository;
    private readonly IRepository<EmployeeOvertime> _employeeOvertimeRepository;
    private readonly IRepository<EmployeeTimesheet> _employeeTimesheetRepository;
    private readonly IRepository<EmployeeSubGroupTicket> _employeeSubGroupTicketRepository;
    private readonly IRepository<TimeEvent> _timeEventRepository;
    private readonly IRepository<TransType> _transTypeRepository;
    private readonly IPunchResolutionStrategyResolver _punchStrategyResolver;

    public TimeRegisterFieldCalculator(
        IRepository<HolidayPeriod> holidayPeriodRepository,
        IRepository<Holiday> holidayRepository,
        IRepository<AbsenceType> absenceTypeRepository,
        IRepository<AttendanceType> attendanceTypeRepository,
        IRepository<WageType> wageTypeRepository,
        IRepository<EmployeeAttendance> employeeAttendanceRepository,
        IRepository<EmployeeOvertime> employeeOvertimeRepository,
        IRepository<EmployeeTimesheet> employeeTimesheetRepository,
        IRepository<EmployeeSubGroupTicket> employeeSubGroupTicketRepository,
        IRepository<TimeEvent> timeEventRepository,
        IRepository<TransType> transTypeRepository,
        IPunchResolutionStrategyResolver punchStrategyResolver)
    {
        _holidayPeriodRepository = holidayPeriodRepository;
        _holidayRepository = holidayRepository;
        _absenceTypeRepository = absenceTypeRepository;
        _attendanceTypeRepository = attendanceTypeRepository;
        _wageTypeRepository = wageTypeRepository;
        _employeeAttendanceRepository = employeeAttendanceRepository;
        _employeeOvertimeRepository = employeeOvertimeRepository;
        _employeeTimesheetRepository = employeeTimesheetRepository;
        _employeeSubGroupTicketRepository = employeeSubGroupTicketRepository;
        _timeEventRepository = timeEventRepository;
        _transTypeRepository = transTypeRepository;
        _punchStrategyResolver = punchStrategyResolver;
    }

    #region << General Calculation >>

    /// <summary>
    /// Legacy <c>GeneralTimeService.GetActiveDay</c> (legacy lines 21-28). Legacy comment, preserved:
    /// "21.02.2018 Mustafa: Active days will come from Contract, if you dont have contract, you are inactive".
    /// </summary>
    public Task<double> GetActiveDayAsync(TimeRegisterDayContext context)
    {
        var dayDate = context.TimeRegister.DayDate;
        var contracts = context.EmployeeContext?.Contracts;

        var active = contracts != null
                     && contracts.Any(r => r.StartDate <= dayDate && r.EndDate >= dayDate);

        return Task.FromResult(active ? 1d : 0d);
    }

    /// <summary>Legacy <c>GeneralTimeService.GetPublicHoliday</c> (legacy lines 30-64).</summary>
    public Task<double> GetPublicHolidayAsync(TimeRegisterDayContext context)
    {
        var timeRegister = context.TimeRegister;
        var dutyPeriod = context.DutyPeriod ?? timeRegister.DutyPeriod;

        if (dutyPeriod == null)
        {
            // Legacy dereferenced timeRegister.DutyPeriod.HolidayCalendar unguarded, so a missing DutyPeriod
            // was a NullReferenceException there. Made explicit here.
            throw new Exception("Holiday Calendar not found in DutyPeriod : (no DutyPeriod resolved for the day)");
        }

        var holidayCalendarId = dutyPeriod.HolidayCalendarId;

        double publicHoliday = 0;

        if (holidayCalendarId.HasValue)
        {
            // Legacy: Program.holidayPeriodList.Where(r => r.Holiday.HolidayCalendar.Oid == rec.Oid
            //                                           && r.HolidayDate == timeRegister.DayDate)
            // Two-step id join instead of a navigation-property walk -- see class remarks, deviation 4.
            var calendarHolidayIds = _holidayRepository.GetAll()
                .Where(h => h.HolidayCalendarId == holidayCalendarId.Value)
                .Select(h => h.Id)
                .ToList();

            var dayDate = timeRegister.DayDate;
            var isHolidayDate = calendarHolidayIds.Count > 0
                                && _holidayPeriodRepository.GetAll()
                                    .Any(r => calendarHolidayIds.Contains(r.HolidayId) && r.HolidayDate == dayDate);

            if (isHolidayDate)
            {
                publicHoliday = 1;

                var duty = DutyOf(context);
                var timePolicy = TimePolicyOf(context);

                if (duty != null && duty.IsWeeklyOff && timePolicy != null && timePolicy.MarkWeekoffOnHoliday)
                {
                    var wageTypes = context.MasterData?.WageTypes;
                    if (wageTypes != null
                        && wageTypes.Any(r => r.Code == "1000" && r.BillingMethod == BillingMethod.FixedDaysPerMonth))
                    {
                        publicHoliday = 0;
                    }
                }
            }
        }
        else
        {
            // Legacy: if (rec == null) throw new Exception("Holiday Calendar not found in DutyPeriod : {0}").
            throw new Exception(string.Format("Holiday Calendar not found in DutyPeriod : {0}", dutyPeriod.NameEn));
        }

        return Task.FromResult(publicHoliday);
    }

    #endregion

    #region << Planned Calculation >>

    /// <summary>Legacy <c>GeneralTimeService.GetPlannedWorkingMinutes</c> (legacy lines 70-81).</summary>
    public Task<double> GetPlannedWorkingMinutesAsync(TimeRegisterDayContext context)
    {
        var timeRegister = context.TimeRegister;
        var duty = DutyOf(context);

        var plannedMinutes =
            (timeRegister.PublicHoliday == 1 || timeRegister.WeeklyOff == 1 || timeRegister.RestDay == 1)
                ? 0
                : (duty == null || duty.BreakShift == null)
                    ? timeRegister.ShiftEndTime1.Subtract(timeRegister.ShiftStartTime1).TotalMinutes
                    : timeRegister.BreakStartTime.Subtract(timeRegister.ShiftStartTime1).TotalMinutes
                      + timeRegister.ShiftEndTime1.Subtract(timeRegister.BreakEndTime).TotalMinutes;

        return Task.FromResult(plannedMinutes < 0 ? 0 : plannedMinutes);
    }

    /// <summary>Legacy <c>GeneralTimeService.GetPlannedWorkingDay</c> (legacy lines 83-86).</summary>
    public Task<double> GetPlannedWorkingDayAsync(TimeRegisterDayContext context)
    {
        return Task.FromResult(context.TimeRegister.PlannedWorkingMins > 0 ? 1d : 0d);
    }

    #endregion
}
