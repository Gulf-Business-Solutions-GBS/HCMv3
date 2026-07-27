using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Uow;
using HCMv3.PersonnelAdministration;

namespace HCMv3.TimeManagement.Services;

/// <summary>
/// <see cref="TimeMgmtStatus.ActualTime"/> punch resolution -- ported from legacy
/// <c>HCM10.TimeEngine.ActualTimeService</c>.
///
/// <para>Punches are taken from the employee's clock transactions inside the policy's clock-in/clock-out
/// tolerance windows around the shift boundaries. On public holidays / weekly-offs / rest days the window is
/// widened to the whole calendar day (00:00:00 .. 23:59:59) because there is no meaningful shift boundary to
/// centre it on -- legacy behaviour.</para>
///
/// <para>The break punches and the four <c>Actual*Time</c> clamps are identical to
/// <see cref="FlexiTimePunchResolutionStrategy"/>'s in legacy and therefore live on
/// <see cref="PunchResolutionStrategyBase"/>; only <c>GetActualIn</c>/<c>GetActualOut</c> are genuinely
/// specific to this strategy.</para>
/// </summary>
/// <remarks>
/// This strategy is pure computation over <see cref="TimeRegisterDayContext"/> (all punch/absence data is
/// pre-loaded on the context by the orchestrator), so it injects no repositories. <see cref="UnitOfWorkAttribute"/>
/// is applied for consistency with the sibling Time Engine domain services.
/// </remarks>
[UnitOfWork]
public class ActualTimePunchResolutionStrategy : PunchResolutionStrategyBase
{
    /// <inheritdoc/>
    public override TimeMgmtStatus TimeMgmtStatus => TimeMgmtStatus.ActualTime;

    /// <summary>
    /// Legacy <c>ActualTimeService.GetActualIn</c>.
    /// 1) build the clock-in window from the policy's <c>ClockInLimit</c>; 2) pick a clock-in punch inside it;
    /// 3) if none, fall back to the hourly-based-leave logic.
    /// </summary>
    public override Task<DateTime> GetActualInAsync(TimeRegisterDayContext context)
    {
        var timeRegister = context.TimeRegister;
        var policy = Policy(context);
        var rule = RuleOf(context);

        GetShiftPunchWindow(
            context,
            timeRegister.ShiftStartTime1,
            policy?.ClockInLimit ?? 0,
            out var windowStart,
            out var windowEnd);

        var timeEvents = GetPunchEvents(
            context, context.MasterData.ClockInTransTypeId, windowStart, windowEnd);

        var actualIn = SelectInPunch(timeEvents, rule, NoPunch);

        #region << Logic for Hourly Bases Leave (legacy: Maisam 15.06.2019) >>

        if (actualIn == NoPunch
            && context.Settings.IsHourlyLeaveLogicEnabled
            && policy != null
            && policy.ShouldSignIn)
        {
            var punchInTime = timeRegister.ShiftStartTime1;
            var absenceAtPunchIn = LeaveAtPunchIn(context, punchInTime);
            var isFullDayHours = IsFullDayHours(context);

            if (absenceAtPunchIn != null)
            {
                if (!isFullDayHours && context.Settings.ShouldSignInAfterHourlyLeave)
                {
                    // Legacy widens to "any clock-in punch whose TransDate falls on the day", ignoring the
                    // tolerance window entirely, and substitutes the shift start when no punch matches.
                    var dayPunches = GetPunchEventsOnDay(
                        context, context.MasterData.ClockInTransTypeId, timeRegister.DayDate);

                    actualIn = SelectInPunch(dayPunches, rule, punchInTime);
                }
                else
                {
                    actualIn = punchInTime;
                }
            }
        }

        #endregion

        return Task.FromResult(actualIn);
    }

    /// <summary>Legacy <c>ActualTimeService.GetBreakOut</c> (identical to FlexiTime's).</summary>
    public override Task<DateTime> GetBreakOutAsync(TimeRegisterDayContext context)
    {
        return Task.FromResult(ResolveBreakOutFromPolicyWindow(context));
    }

    /// <summary>Legacy <c>ActualTimeService.GetBreakIn</c> (identical to FlexiTime's).</summary>
    public override Task<DateTime> GetBreakInAsync(TimeRegisterDayContext context)
    {
        return Task.FromResult(ResolveBreakInFromPolicyWindow(context));
    }

    /// <summary>
    /// Legacy <c>ActualTimeService.GetActualOut</c> -- mirror image of <see cref="GetActualInAsync"/> using
    /// <c>ClockOutLimit</c>, the OUT selection rule, and the "leave at punch out" hourly-leave fallback.
    /// </summary>
    public override Task<DateTime> GetActualOutAsync(TimeRegisterDayContext context)
    {
        var timeRegister = context.TimeRegister;
        var policy = Policy(context);
        var rule = RuleOf(context);

        GetShiftPunchWindow(
            context,
            timeRegister.ShiftEndTime1,
            policy?.ClockOutLimit ?? 0,
            out var windowStart,
            out var windowEnd);

        var timeEvents = GetPunchEvents(
            context, context.MasterData.ClockOutTransTypeId, windowStart, windowEnd);

        var actualOut = SelectOutPunch(timeEvents, rule, NoPunch);

        #region << Logic for Hourly Bases Leave >>

        // NOTE (legacy quirk, preserved): this guard tests ShouldSignIn -- NOT ShouldSignOut -- exactly as
        // legacy ActualTimeService.GetActualOut line ~118 does.
        if (actualOut == NoPunch
            && context.Settings.IsHourlyLeaveLogicEnabled
            && policy != null
            && policy.ShouldSignIn)
        {
            var punchOutTime = timeRegister.ShiftEndTime1;
            var absenceAtPunchOut = LeaveAtPunchOut(context, punchOutTime);
            var isFullDayHours = IsFullDayHours(context);

            if (absenceAtPunchOut != null)
            {
                if (!isFullDayHours && context.Settings.ShouldSignOutBeforeHourlyLeave)
                {
                    var dayPunches = GetPunchEventsOnDay(
                        context, context.MasterData.ClockOutTransTypeId, timeRegister.DayDate);

                    actualOut = SelectOutPunch(dayPunches, rule, punchOutTime);
                }
                else
                {
                    actualOut = punchOutTime;
                }
            }
        }

        #endregion

        return Task.FromResult(actualOut);
    }

    /// <inheritdoc/>
    public override Task<DateTime> GetActualStartTimeAsync(TimeRegisterDayContext context)
    {
        return Task.FromResult(ResolveShiftBoundedActualStartTime(context));
    }

    /// <inheritdoc/>
    public override Task<DateTime> GetActualBreakStartTimeAsync(TimeRegisterDayContext context)
    {
        return Task.FromResult(ResolveShiftBoundedActualBreakStartTime(context));
    }

    /// <inheritdoc/>
    public override Task<DateTime> GetActualBreakEndTimeAsync(TimeRegisterDayContext context)
    {
        return Task.FromResult(ResolveShiftBoundedActualBreakEndTime(context));
    }

    /// <inheritdoc/>
    public override Task<DateTime> GetActualEndTimeAsync(TimeRegisterDayContext context)
    {
        return Task.FromResult(ResolveShiftBoundedActualEndTime(context));
    }

    #region << Helpers >>

    /// <summary>
    /// Legacy's <c>actualInTimeWindowStart/End</c> (and the ActualOut twin): the tolerance window centred on
    /// <paramref name="shiftBoundary"/>, EXCEPT on a public holiday / weekly-off / rest day, where legacy
    /// replaces it with the whole calendar day of that boundary (00:00:00 .. 23:59:59).
    /// </summary>
    private static void GetShiftPunchWindow(
        TimeRegisterDayContext context,
        DateTime shiftBoundary,
        double limitMinutes,
        out DateTime windowStart,
        out DateTime windowEnd)
    {
        var timeRegister = context.TimeRegister;
        var duty = DutyOf(context);

        var isNonWorkingDay = timeRegister.PublicHoliday == 1
                              || (duty != null && duty.IsWeeklyOff)
                              || (duty != null && duty.IsRestDay);

        if (isNonWorkingDay)
        {
            windowStart = shiftBoundary.Date;
            windowEnd = shiftBoundary.Date.Add(new TimeSpan(23, 59, 59));
        }
        else
        {
            windowStart = shiftBoundary.AddMinutes(-limitMinutes);
            windowEnd = shiftBoundary.AddMinutes(limitMinutes);
        }
    }

    /// <summary>
    /// Legacy's hourly-leave fallback query: every punch of the given type whose <c>TransDate</c> is the
    /// register's day, ordered by timestamp -- note it filters on <c>TransDate</c> (date only), not on
    /// <c>TransDateTime</c>, so a night shift's next-day punches are excluded. Preserved.
    /// </summary>
    private static List<TimePunchEvent> GetPunchEventsOnDay(
        TimeRegisterDayContext context,
        int transTypeId,
        DateTime dayDate)
    {
        var timeEvents = context.EmployeeContext?.TimeEvents;
        if (timeEvents == null)
        {
            return new List<TimePunchEvent>();
        }

        return timeEvents
            .Where(r => r.TransTypeId == transTypeId && r.TransDate.Date == dayDate.Date)
            .OrderBy(r => r.TransDateTime)
            .ToList();
    }

    /// <summary>
    /// Ported from legacy <c>Program.LeaveAtPunchIn</c> (legacy <c>Program.cs</c> line ~2454): the first
    /// hourly absence (<c>AbsenceHours != 0</c>) starting on the day whose time-of-day range contains the
    /// punch-in time.
    /// </summary>
    private static EmployeeAbsence LeaveAtPunchIn(TimeRegisterDayContext context, DateTime punchInTime)
    {
        return AbsencesOfDay(context)
            .FirstOrDefault(r => r.StartTime.TimeOfDay <= punchInTime.TimeOfDay
                                 && r.EndTime.TimeOfDay >= punchInTime.TimeOfDay
                                 && r.AbsenceHours != 0);
    }

    /// <summary>
    /// Ported from legacy <c>Program.LeaveAtPunchOut</c> (legacy <c>Program.cs</c> line ~2470). Legacy's
    /// comment claims "StartTime &gt;= punchOutTime" but the code is the same containment test as
    /// <see cref="LeaveAtPunchIn"/> -- the code, not the comment, is ported.
    /// </summary>
    private static EmployeeAbsence LeaveAtPunchOut(TimeRegisterDayContext context, DateTime punchOutTime)
    {
        return AbsencesOfDay(context)
            .FirstOrDefault(r => r.StartTime.TimeOfDay <= punchOutTime.TimeOfDay
                                 && r.EndTime.TimeOfDay >= punchOutTime.TimeOfDay
                                 && r.AbsenceHours != 0);
    }

    /// <summary>
    /// Ported from legacy <c>Program.IsFullDayHours</c> (legacy <c>Program.cs</c> line ~2492): true when the
    /// day's hourly leave covers (or exceeds) the duty's total working hours.
    /// </summary>
    private static bool IsFullDayHours(TimeRegisterDayContext context)
    {
        var leaveHours = AbsencesOfDay(context)
            .Where(r => r.AbsenceHours != 0)
            .Sum(r => r.AbsenceHours);

        return leaveHours > 0 && GetTotalFullDayHours(context) <= leaveHours;
    }

    /// <summary>
    /// Ported from legacy <c>Program.GetTotalFullDayHours</c> (legacy <c>Program.cs</c> line ~2486): shift
    /// span, minus the break span when the duty has a break shift.
    /// </summary>
    private static double GetTotalFullDayHours(TimeRegisterDayContext context)
    {
        var timeRegister = context.TimeRegister;
        var duty = DutyOf(context);

        if (duty?.BreakShift == null)
        {
            return timeRegister.ShiftEndTime1.Subtract(timeRegister.ShiftStartTime1).TotalHours;
        }

        return timeRegister.BreakStartTime.Subtract(timeRegister.ShiftStartTime1).TotalHours
               + timeRegister.ShiftEndTime1.Subtract(timeRegister.BreakEndTime).TotalHours;
    }

    /// <summary>Legacy's shared <c>empAbsenceList.Where(r =&gt; r.StartDate == timeRegister.DayDate ...)</c> prefix.</summary>
    private static IEnumerable<EmployeeAbsence> AbsencesOfDay(TimeRegisterDayContext context)
    {
        var absences = context.EmployeeContext?.Absences;
        if (absences == null)
        {
            return Enumerable.Empty<EmployeeAbsence>();
        }

        var dayDate = context.TimeRegister.DayDate;
        return absences.Where(r => r.StartDate == dayDate);
    }

    #endregion
}
