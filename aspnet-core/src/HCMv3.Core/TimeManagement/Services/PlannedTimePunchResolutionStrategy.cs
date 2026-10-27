using System;
using System.Threading.Tasks;
using Abp.Domain.Uow;

namespace HCMv3.TimeManagement.Services;

/// <summary>
/// <see cref="TimeMgmtStatus.PlannedTime"/> punch resolution -- ported from legacy
/// <c>HCM10.TimeEngine.PlannedTimeService</c>.
///
/// <para>"Planned time" means the employee is paid to plan, not to punch: the four <c>Actual*Time</c> values
/// are simply the planned shift/break boundaries, and the break punches are always "no punch". The only
/// punch lookup that happens at all is the in/out pair, and ONLY when
/// <see cref="TimeEngineSettings.UseActualTransInPlannedTimePolicy"/> is on; otherwise both return the
/// sentinel without touching <see cref="TimeEngineEmployeeContext.TimeEvents"/> (which is exactly the
/// configuration where legacy's <c>loadTimeEvent</c> guard leaves that list null).</para>
/// </summary>
[UnitOfWork]
public class PlannedTimePunchResolutionStrategy : PunchResolutionStrategyBase
{
    /// <inheritdoc/>
    public override TimeMgmtStatus TimeMgmtStatus => TimeMgmtStatus.PlannedTime;

    /// <summary>
    /// Legacy <c>PlannedTimeService.GetActualIn</c>. Window is the duty's flexi range applied to the
    /// register's <c>DayDate</c> (NOT to <c>ShiftStartTime1</c> as FlexiTime does, and with no overnight
    /// roll-forward) -- legacy difference, preserved.
    /// </summary>
    public override Task<DateTime> GetActualInAsync(TimeRegisterDayContext context)
    {
        if (!context.Settings.UseActualTransInPlannedTimePolicy)
        {
            return Task.FromResult(NoPunch);
        }

        GetPlannedFlexiWindow(context, out var windowStart, out var windowEnd);

        var timeEvents = GetPunchEvents(
            context, context.MasterData.ClockInTransTypeId, windowStart, windowEnd);

        return Task.FromResult(SelectInPunch(timeEvents, RuleOf(context), NoPunch));
    }

    /// <summary>Legacy <c>PlannedTimeService.GetBreakOut</c> -- unconditional "no punch".</summary>
    public override Task<DateTime> GetBreakOutAsync(TimeRegisterDayContext context)
    {
        return Task.FromResult(NoPunch);
    }

    /// <summary>Legacy <c>PlannedTimeService.GetBreakIn</c> -- unconditional "no punch".</summary>
    public override Task<DateTime> GetBreakInAsync(TimeRegisterDayContext context)
    {
        return Task.FromResult(NoPunch);
    }

    /// <summary>
    /// Legacy <c>PlannedTimeService.GetActualOut</c>. Same window as <see cref="GetActualInAsync"/> (legacy
    /// even reuses the <c>actualInTimeWindowStart/End</c> variable names), but selects a clock-OUT punch with
    /// the OUT rule.
    /// </summary>
    public override Task<DateTime> GetActualOutAsync(TimeRegisterDayContext context)
    {
        if (!context.Settings.UseActualTransInPlannedTimePolicy)
        {
            return Task.FromResult(NoPunch);
        }

        GetPlannedFlexiWindow(context, out var windowStart, out var windowEnd);

        var timeEvents = GetPunchEvents(
            context, context.MasterData.ClockOutTransTypeId, windowStart, windowEnd);

        return Task.FromResult(SelectOutPunch(timeEvents, RuleOf(context), NoPunch));
    }

    /// <summary>Legacy <c>PlannedTimeService.GetActualStartTime</c> -- the planned shift start, always.</summary>
    public override Task<DateTime> GetActualStartTimeAsync(TimeRegisterDayContext context)
    {
        return Task.FromResult(context.TimeRegister.ShiftStartTime1);
    }

    /// <summary>Legacy <c>PlannedTimeService.GetActualBreakStartTime</c> -- the planned break start, always.</summary>
    public override Task<DateTime> GetActualBreakStartTimeAsync(TimeRegisterDayContext context)
    {
        return Task.FromResult(context.TimeRegister.BreakStartTime);
    }

    /// <summary>Legacy <c>PlannedTimeService.GetActualBreakEndTime</c> -- the planned break end, always.</summary>
    public override Task<DateTime> GetActualBreakEndTimeAsync(TimeRegisterDayContext context)
    {
        return Task.FromResult(context.TimeRegister.BreakEndTime);
    }

    /// <summary>Legacy <c>PlannedTimeService.GetActualEndTime</c> -- the planned shift end, always.</summary>
    public override Task<DateTime> GetActualEndTimeAsync(TimeRegisterDayContext context)
    {
        return Task.FromResult(context.TimeRegister.ShiftEndTime1);
    }

    /// <summary>
    /// Legacy <c>timeRegister.DayDate.Add(Duty.FlexiStartTime.TimeOfDay)</c> ..
    /// <c>timeRegister.DayDate.Add(Duty.FlexiEndTime.TimeOfDay)</c>.
    ///
    /// <para>PORT NOTES: (a) <see cref="Duty.FlexiStartTime"/>/<see cref="Duty.FlexiEndTime"/> are
    /// <c>TimeSpan?</c> here where legacy had non-nullable <c>DateTime</c>s defaulting to
    /// <c>DateTime.MinValue</c>, so null maps to <see cref="TimeSpan.Zero"/> (== legacy's 00:00);
    /// (b) legacy used the full <c>TimeOfDay</c> including seconds, so unlike
    /// <see cref="PunchResolutionStrategyBase.GetFlexiWindow"/> the <c>TimeSpan</c> is added whole;
    /// (c) <c>.Date</c> is applied to <c>DayDate</c> defensively -- <c>DayDate</c> is a date-only value in
    /// this port, so this is a no-op, but legacy's bare <c>.Add</c> would have shifted the window had it
    /// ever carried a time component.</para>
    /// </summary>
    private static void GetPlannedFlexiWindow(
        TimeRegisterDayContext context,
        out DateTime windowStart,
        out DateTime windowEnd)
    {
        var duty = DutyOf(context);
        var dayDate = context.TimeRegister.DayDate.Date;

        windowStart = dayDate.Add(duty?.FlexiStartTime ?? TimeSpan.Zero);
        windowEnd = dayDate.Add(duty?.FlexiEndTime ?? TimeSpan.Zero);
    }
}
