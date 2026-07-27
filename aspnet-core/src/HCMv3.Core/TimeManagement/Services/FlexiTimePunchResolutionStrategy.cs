using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Uow;

namespace HCMv3.TimeManagement.Services;

/// <summary>
/// <see cref="TimeMgmtStatus.FlexiTime"/> punch resolution -- ported from legacy
/// <c>HCM10.TimeEngine.FlexiTimeService</c>. This is the only strategy that also implements
/// <see cref="IFlexiTimeCalculationOverride"/>.
///
/// <para>Flexi punches are looked up inside the DUTY's flexi window (rather than a tolerance band around the
/// shift boundaries as <see cref="ActualTimePunchResolutionStrategy"/> does); break punches and the four
/// <c>Actual*Time</c> clamps are identical to ActualTime's and live on
/// <see cref="PunchResolutionStrategyBase"/>.</para>
/// </summary>
[UnitOfWork]
public class FlexiTimePunchResolutionStrategy : PunchResolutionStrategyBase, IFlexiTimeCalculationOverride
{
    /// <inheritdoc/>
    public override TimeMgmtStatus TimeMgmtStatus => TimeMgmtStatus.FlexiTime;

    /// <summary>Legacy <c>FlexiTimeService.GetActualIn</c> -- first/last clock-in inside the flexi window.</summary>
    public override Task<DateTime> GetActualInAsync(TimeRegisterDayContext context)
    {
        GetFlexiWindow(context, out var windowStart, out var windowEnd);

        var timeEvents = GetPunchEvents(
            context, context.MasterData.ClockInTransTypeId, windowStart, windowEnd);

        return Task.FromResult(SelectInPunch(timeEvents, RuleOf(context), NoPunch));
    }

    /// <summary>Legacy <c>FlexiTimeService.GetBreakOut</c> (identical to ActualTime's).</summary>
    public override Task<DateTime> GetBreakOutAsync(TimeRegisterDayContext context)
    {
        return Task.FromResult(ResolveBreakOutFromPolicyWindow(context));
    }

    /// <summary>Legacy <c>FlexiTimeService.GetBreakIn</c> (identical to ActualTime's).</summary>
    public override Task<DateTime> GetBreakInAsync(TimeRegisterDayContext context)
    {
        return Task.FromResult(ResolveBreakInFromPolicyWindow(context));
    }

    /// <summary>
    /// Legacy <c>FlexiTimeService.GetActualOut</c> -- clock-OUT punch inside the SAME flexi window as
    /// <see cref="GetActualInAsync"/> (legacy anchors both ends of the window on <c>ShiftStartTime1</c>'s
    /// date, not on <c>ShiftEndTime1</c>'s; preserved).
    /// </summary>
    public override Task<DateTime> GetActualOutAsync(TimeRegisterDayContext context)
    {
        GetFlexiWindow(context, out var windowStart, out var windowEnd);

        var timeEvents = GetPunchEvents(
            context, context.MasterData.ClockOutTransTypeId, windowStart, windowEnd);

        return Task.FromResult(SelectOutPunch(timeEvents, RuleOf(context), NoPunch));
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

    #region << IFlexiTimeCalculationOverride >>

    /// <summary>
    /// Legacy <c>FlexiTimeService.GetTotalWorkingMinutes</c>. Walks EVERY punch in the flexi window
    /// (both directions, and note the legacy window filter here does NOT restrict by trans type) pairing
    /// in/out punches and summing their spans.
    ///
    /// <para>LEGACY QUIRK, PRESERVED: the "find the in-punch" arm only accepts a clock-in whose immediately
    /// following punch in the window is NOT another clock-in (or which has no follower at all). With a run of
    /// consecutive clock-ins, only the LAST of the run opens the interval -- the earlier ones are discarded.
    /// This looks like a de-duplication heuristic rather than a bug, but it is subtle enough to call out.</para>
    ///
    /// <para>LEGACY QUIRK, PRESERVED: the whole calculation is skipped (returns 0) unless BOTH
    /// <c>TimeRegister.ActualIn</c> and <c>TimeRegister.ActualOut</c> are set, even though the pairing loop
    /// itself does not otherwise depend on them.</para>
    /// </summary>
    public Task<double> GetTotalWorkingMinutesAsync(TimeRegisterDayContext context)
    {
        var timeRegister = context.TimeRegister;
        double totalWorkingMins = 0;

        if (timeRegister.ActualIn != NoPunch && timeRegister.ActualOut != NoPunch)
        {
            GetFlexiWindow(context, out var windowStart, out var windowEnd);

            var allEvents = context.EmployeeContext?.TimeEvents;
            var timeEvents = allEvents == null
                ? new List<TimePunchEvent>()
                : allEvents
                    .Where(r => r.TransDateTime >= windowStart && r.TransDateTime <= windowEnd)
                    .OrderBy(r => r.TransDateTime)
                    .ToList();

            var clockInId = context.MasterData.ClockInTransTypeId;
            var clockOutId = context.MasterData.ClockOutTransTypeId;

            var lastPunchIn = NoPunch;
            var lastPunchOut = NoPunch;
            var foundIn = false;
            var foundOut = false;

            foreach (var te in timeEvents)
            {
                // First find PunchIn Entry
                if (!foundIn && te.TransTypeId == clockInId)
                {
                    var teNext = timeEvents.FirstOrDefault(next => next.TransDateTime > te.TransDateTime);
                    if (teNext == null || teNext.TransTypeId != clockInId)
                    {
                        foundIn = true;
                        lastPunchIn = te.TransDateTime;
                    }
                }

                // If PunchIn Entry Found, then find PunchOut Entry
                else if (foundIn && !foundOut && te.TransTypeId == clockOutId)
                {
                    foundOut = true;
                    lastPunchOut = te.TransDateTime;
                }

                // If PunchIn Entry & PunchOut Entry both Found, then Calculate Duration Minutes
                if (foundIn && foundOut)
                {
                    totalWorkingMins += lastPunchOut.Subtract(lastPunchIn).TotalMinutes;

                    foundIn = false;
                    foundOut = false;
                    lastPunchIn = NoPunch;
                    lastPunchOut = NoPunch;
                }
            }

            if (totalWorkingMins < 0)
            {
                totalWorkingMins = 0;
            }
        }

        return Task.FromResult(Math.Round(totalWorkingMins, 3));
    }

    /// <summary>
    /// Legacy <c>FlexiTimeService.GetActualAbsenceMinutes</c>. Legacy's <c>IsNotReturnFromLeave</c> parameter
    /// is <see cref="TimeRegisterDayContext.IsNotReturnFromLeave"/>.
    ///
    /// <para>LEGACY QUIRK, PRESERVED: the middle block's guard is
    /// <c>absMinutes != PlannedWorkingMins</c>, so when a missing punch has already set
    /// <c>absMinutes = PlannedWorkingMins</c> the planned-minus-worked recomputation is skipped -- and a
    /// coincidental equality (planned == planned-minus-worked, i.e. worked == 0) makes the subsequent unpaid
    /// -leave top-up silently not apply either.</para>
    ///
    /// <para>PORT NOTE: legacy read <c>timeRegister.LeaveType.IsPaid</c> through an XPO navigation property.
    /// <see cref="TimeRegister.LeaveTypeId"/> in this port is a bare nullable id with no navigation, so the
    /// <see cref="PayrollManagement.AbsenceType"/> is looked up in
    /// <see cref="TimeEngineMasterData.AbsenceTypes"/>. When the id is null or unresolvable the unpaid-leave
    /// top-up is skipped (legacy would have thrown a <c>NullReferenceException</c>; that path is unreachable
    /// in practice because the guard requires <c>LeaveDay == 1</c>, which implies a leave type was set).</para>
    /// </summary>
    public Task<double> GetActualAbsenceMinutesAsync(TimeRegisterDayContext context)
    {
        var timeRegister = context.TimeRegister;
        double absMinutes = 0;

        if (timeRegister.PlannedWorkingMins > 0
            && (timeRegister.ActualIn == NoPunch || timeRegister.ActualOut == NoPunch))
        {
            absMinutes = timeRegister.PlannedWorkingMins;
        }

        // ReSharper disable once CompareOfFloatsByEqualityOperator -- legacy comparison, preserved verbatim.
        if (absMinutes != timeRegister.PlannedWorkingMins)
        {
            absMinutes = timeRegister.PlannedWorkingMins - timeRegister.TotalWorkingMins;
            if (absMinutes < 0)
            {
                // If PlannedWorkingMins = 8 Hrs, TotalWorkingMins = 9 Hrs then No AbsenceMinute
                absMinutes = 0;
            }
        }

        var leaveType = timeRegister.LeaveTypeId.HasValue
            ? context.MasterData.AbsenceTypes.FirstOrDefault(t => t.Id == timeRegister.LeaveTypeId.Value)
            : null;

        // ReSharper disable once CompareOfFloatsByEqualityOperator -- legacy comparison, preserved verbatim.
        if (absMinutes != timeRegister.PlannedWorkingMins
            && timeRegister.LeaveDay == 1
            && leaveType != null
            && !leaveType.IsPaid)
        {
            absMinutes += timeRegister.LeaveMinutes;
        }

        #region << Logic for IsNotReturnFromLeave >>

        if (timeRegister.PlannedWorkingMins > 0 && context.IsNotReturnFromLeave)
        {
            absMinutes = timeRegister.PlannedWorkingMins;
        }

        #endregion

        return Task.FromResult(Math.Round(absMinutes, 3));
    }

    /// <summary>
    /// Legacy <c>FlexiTimeService.GetOvertimeMinutes</c>: worked minus planned, zeroed when negative or below
    /// the policy's <see cref="TimePolicy.OTMinLimit"/> threshold.
    /// </summary>
    public Task<double> GetOvertimeMinutesAsync(TimeRegisterDayContext context)
    {
        var timeRegister = context.TimeRegister;
        var otMinLimit = Policy(context)?.OTMinLimit ?? 0;

        var overtimeMins = timeRegister.TotalWorkingMins - timeRegister.PlannedWorkingMins;

        if ((overtimeMins > 0 && overtimeMins < otMinLimit) || overtimeMins < 0)
        {
            overtimeMins = 0;
        }

        return Task.FromResult(Math.Round(overtimeMins, 3));
    }

    /// <summary>
    /// Legacy <c>FlexiTimeService.GetAbsentDay</c>. DEAD CODE in the orchestrator -- legacy
    /// <c>CreateTimeRegister</c> always calls <c>GeneralTimeService.GetAbsentDay</c> (legacy
    /// <c>Program.cs</c> line ~1368) and never this one; ported for completeness of wave T3 only.
    /// </summary>
    public Task<double> GetAbsentDayAsync(TimeRegisterDayContext context)
    {
        var timeRegister = context.TimeRegister;

        var absentDay = timeRegister.PlannedWorkingDay > 0
                        && timeRegister.ActualWorkingMins == 0
                        && timeRegister.LeaveDay == 0
            ? 1d
            : 0d;

        return Task.FromResult(absentDay);
    }

    #endregion
}
