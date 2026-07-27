using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HCMv3.TimeManagement.Services;

/// <summary>
/// Shared plumbing for the four wave-T3 <see cref="IPunchResolutionStrategy"/> implementations, factored out of
/// the heavy near-duplication between the legacy <c>ActualTimeService</c> / <c>FlexiTimeService</c> /
/// <c>PlannedTimeService</c> / <c>TimesheetService</c> static classes.
///
/// <para><b>Only genuinely identical behaviour lives here.</b> Concretely:</para>
/// <list type="bullet">
/// <item><description><see cref="SelectInPunch"/> / <see cref="SelectOutPunch"/> -- the
/// <see cref="TimeEventRule"/> first/last selection ternaries, byte-for-byte identical in all four legacy
/// files (note the IN and OUT variants pair the four rules DIFFERENTLY -- that asymmetry is legacy behaviour,
/// see each method's remarks).</description></item>
/// <item><description><see cref="ResolveBreakOutFromPolicyWindow"/> /
/// <see cref="ResolveBreakInFromPolicyWindow"/> -- legacy <c>ActualTimeService.GetBreakOut/GetBreakIn</c> and
/// <c>FlexiTimeService.GetBreakOut/GetBreakIn</c> are character-identical.</description></item>
/// <item><description><see cref="ResolveShiftBoundedActualStartTime"/> and its three siblings -- legacy
/// <c>ActualTimeService</c> writes these as nested ternaries and <c>FlexiTimeService</c> as
/// <c>if/else if</c>, but the resulting value is identical for every input (the third ternary arm /
/// missing <c>else</c> is unreachable: <c>&lt;=</c> and <c>&gt;</c> are exhaustive).</description></item>
/// </list>
///
/// <para><c>PlannedTimeService</c> and <c>TimesheetService</c> deliberately do NOT reuse the
/// <c>ResolveShiftBounded*</c> helpers: their versions differ in real ways (Planned returns the shift time
/// unconditionally; Timesheet omits the <c>ShouldSignIn</c>/<c>ShouldSignOut</c>/<c>ShouldBreakSign*</c> gate
/// and compares full <see cref="DateTime"/>s rather than <see cref="DateTime.TimeOfDay"/>).</para>
///
/// <para><b>Sentinel.</b> Legacy's <c>DateTimeExtension.DBDtNull</c> "no punch" sentinel is
/// <c>default(DateTime)</c> here, per <see cref="IPunchResolutionStrategy"/>'s documented convention.</para>
///
/// <para><b>Null <c>TimeEvents</c>.</b> <see cref="TimeEngineEmployeeContext.TimeEvents"/> is null when legacy's
/// <c>loadTimeEvent</c> guard was false. <see cref="GetPunchEvents"/> treats null as "no punches" (returns an
/// empty list) rather than throwing; legacy never reached the punch-event strategies in that configuration
/// (the guard is false only when every policy is PlannedTime with
/// <see cref="TimeEngineSettings.UseActualTransInPlannedTimePolicy"/> off, i.e. exactly the branch that skips
/// the event query), so an empty result is the behaviour-preserving choice.</para>
/// </summary>
public abstract class PunchResolutionStrategyBase : HCMv3DomainServiceBase, IPunchResolutionStrategy
{
    /// <summary>Legacy "no punch" sentinel -- see class remarks.</summary>
    protected static readonly DateTime NoPunch = default;

    /// <inheritdoc/>
    public abstract TimeMgmtStatus TimeMgmtStatus { get; }

    /// <inheritdoc/>
    public abstract Task<DateTime> GetActualInAsync(TimeRegisterDayContext context);

    /// <inheritdoc/>
    public abstract Task<DateTime> GetBreakOutAsync(TimeRegisterDayContext context);

    /// <inheritdoc/>
    public abstract Task<DateTime> GetBreakInAsync(TimeRegisterDayContext context);

    /// <inheritdoc/>
    public abstract Task<DateTime> GetActualOutAsync(TimeRegisterDayContext context);

    /// <inheritdoc/>
    public abstract Task<DateTime> GetActualStartTimeAsync(TimeRegisterDayContext context);

    /// <inheritdoc/>
    public abstract Task<DateTime> GetActualBreakStartTimeAsync(TimeRegisterDayContext context);

    /// <inheritdoc/>
    public abstract Task<DateTime> GetActualBreakEndTimeAsync(TimeRegisterDayContext context);

    /// <inheritdoc/>
    public abstract Task<DateTime> GetActualEndTimeAsync(TimeRegisterDayContext context);

    #region << Context accessors >>

    /// <summary>
    /// The day's policy. Legacy read <c>timeRegister.TimePolicy</c>; this port reads
    /// <see cref="TimeRegisterDayContext.TimePolicy"/> because <c>TimeRegister</c>'s navigation property is not
    /// guaranteed to be loaded by EF (same reasoning already documented for
    /// <see cref="TimeRegisterDayContext.Duty"/>).
    /// </summary>
    protected static TimePolicy Policy(TimeRegisterDayContext context)
    {
        return context.TimePolicy;
    }

    /// <summary>
    /// The day's duty -- <see cref="TimeRegisterDayContext.Duty"/> rather than <c>TimeRegister.Duty</c>, for the
    /// reason documented on that property.
    /// </summary>
    protected static Duty DutyOf(TimeRegisterDayContext context)
    {
        return context.Duty;
    }

    /// <summary>
    /// <see cref="TimeEventRule"/> for the day. A null policy (only reachable via the resolver's
    /// TimeSheet fall-through) yields <see cref="TimeEventRule.NA"/>, which makes both selectors return the
    /// "no punch" sentinel -- matching legacy's final <c>: DBDtNull</c> ternary arm.
    /// </summary>
    protected static TimeEventRule RuleOf(TimeRegisterDayContext context)
    {
        return context.TimePolicy?.TimeEventRule ?? TimeEventRule.NA;
    }

    #endregion

    #region << Punch event querying / selection >>

    /// <summary>
    /// Legacy's repeated
    /// <c>empTimeEvent.Where(r =&gt; r.TransType.Oid == {clockIn|clockOut}Oid &amp;&amp; r.TransDateTime &gt;= start
    /// &amp;&amp; r.TransDateTime &lt;= end).OrderBy(r =&gt; r.TransDateTime)</c> block. Window bounds are
    /// INCLUSIVE at both ends, exactly as legacy.
    /// </summary>
    protected static List<TimePunchEvent> GetPunchEvents(
        TimeRegisterDayContext context,
        int transTypeId,
        DateTime windowStart,
        DateTime windowEnd)
    {
        var timeEvents = context.EmployeeContext?.TimeEvents;
        if (timeEvents == null)
        {
            return new List<TimePunchEvent>();
        }

        return timeEvents
            .Where(r => r.TransTypeId == transTypeId
                        && r.TransDateTime >= windowStart
                        && r.TransDateTime <= windowEnd)
            .OrderBy(r => r.TransDateTime)
            .ToList();
    }

    /// <summary>
    /// Legacy's IN-punch selection ternary: <c>FirstInFirstOut</c>/<c>FirstInLastOut</c> take the FIRST punch,
    /// <c>LastInFirstOut</c>/<c>LastInLastOut</c> take the LAST, anything else falls through to
    /// <paramref name="noMatchValue"/> (legacy <c>DBDtNull</c>, except in the hourly-leave branch of
    /// <c>ActualTimeService.GetActualIn</c> where legacy substitutes the shift start time -- hence the
    /// parameter).
    /// </summary>
    protected static DateTime SelectInPunch(
        List<TimePunchEvent> timeEvents,
        TimeEventRule rule,
        DateTime noMatchValue)
    {
        if (timeEvents.Count == 0)
        {
            return noMatchValue;
        }

        if (rule == TimeEventRule.FirstInFirstOut || rule == TimeEventRule.FirstInLastOut)
        {
            return timeEvents.First().TransDateTime;
        }

        if (rule == TimeEventRule.LastInFirstOut || rule == TimeEventRule.LastInLastOut)
        {
            return timeEvents.Last().TransDateTime;
        }

        return noMatchValue;
    }

    /// <summary>
    /// Legacy's OUT-punch selection ternary. NOTE the pairing is deliberately different from
    /// <see cref="SelectInPunch"/>: here <c>FirstInFirstOut</c>/<c>LastInFirstOut</c> take the FIRST punch and
    /// <c>FirstInLastOut</c>/<c>LastInLastOut</c> take the LAST -- i.e. the rule's "...Out" half decides.
    /// </summary>
    protected static DateTime SelectOutPunch(
        List<TimePunchEvent> timeEvents,
        TimeEventRule rule,
        DateTime noMatchValue)
    {
        if (timeEvents.Count == 0)
        {
            return noMatchValue;
        }

        if (rule == TimeEventRule.FirstInFirstOut || rule == TimeEventRule.LastInFirstOut)
        {
            return timeEvents.First().TransDateTime;
        }

        if (rule == TimeEventRule.FirstInLastOut || rule == TimeEventRule.LastInLastOut)
        {
            return timeEvents.Last().TransDateTime;
        }

        return noMatchValue;
    }

    #endregion

    #region << Break punch windows (ActualTime + FlexiTime, identical in legacy) >>

    /// <summary>
    /// Legacy <c>ActualTimeService.GetBreakOut</c> == <c>FlexiTimeService.GetBreakOut</c>: a clock-OUT punch
    /// inside <c>BreakStartTime +/- TimePolicy.BreakOutLimit</c> minutes, picked with the OUT rule.
    /// </summary>
    protected static DateTime ResolveBreakOutFromPolicyWindow(TimeRegisterDayContext context)
    {
        var timeRegister = context.TimeRegister;
        var policy = Policy(context);
        var limit = policy?.BreakOutLimit ?? 0;

        var windowStart = timeRegister.BreakStartTime.AddMinutes(-limit);
        var windowEnd = timeRegister.BreakStartTime.AddMinutes(limit);

        var timeEvents = GetPunchEvents(
            context, context.MasterData.ClockOutTransTypeId, windowStart, windowEnd);

        return SelectOutPunch(timeEvents, RuleOf(context), NoPunch);
    }

    /// <summary>
    /// Legacy <c>ActualTimeService.GetBreakIn</c> == <c>FlexiTimeService.GetBreakIn</c>: a clock-IN punch
    /// inside <c>BreakEndTime +/- TimePolicy.BreakInLimit</c> minutes, picked with the IN rule.
    /// </summary>
    protected static DateTime ResolveBreakInFromPolicyWindow(TimeRegisterDayContext context)
    {
        var timeRegister = context.TimeRegister;
        var policy = Policy(context);
        var limit = policy?.BreakInLimit ?? 0;

        var windowStart = timeRegister.BreakEndTime.AddMinutes(-limit);
        var windowEnd = timeRegister.BreakEndTime.AddMinutes(limit);

        var timeEvents = GetPunchEvents(
            context, context.MasterData.ClockInTransTypeId, windowStart, windowEnd);

        return SelectInPunch(timeEvents, RuleOf(context), NoPunch);
    }

    #endregion

    #region << Shift-bounded actual times (ActualTime + FlexiTime, identical in legacy) >>

    /// <summary>
    /// Legacy <c>ActualTimeService.GetActualStartTime</c> / <c>FlexiTimeService.GetActualStartTime</c>.
    /// When the employee must sign in, clamp <c>ActualIn</c> UP to the shift start (early punches do not earn
    /// extra time); otherwise the shift start is taken as-is. Note the comparison is on
    /// <see cref="DateTime.TimeOfDay"/>, not the full timestamp -- legacy behaviour, preserved.
    /// </summary>
    protected static DateTime ResolveShiftBoundedActualStartTime(TimeRegisterDayContext context)
    {
        var timeRegister = context.TimeRegister;
        var policy = Policy(context);

        if (policy != null && policy.ShouldSignIn)
        {
            if (timeRegister.ActualIn == NoPunch)
            {
                return NoPunch;
            }

            return timeRegister.ActualIn.TimeOfDay <= timeRegister.ShiftStartTime1.TimeOfDay
                ? timeRegister.ShiftStartTime1
                : timeRegister.ActualIn;
        }

        return timeRegister.ShiftStartTime1;
    }

    /// <summary>
    /// Legacy <c>ActualTimeService.GetActualEndTime</c> / <c>FlexiTimeService.GetActualEndTime</c>.
    /// Clamps <c>ActualOut</c> DOWN to the shift end when the employee must sign out.
    /// </summary>
    protected static DateTime ResolveShiftBoundedActualEndTime(TimeRegisterDayContext context)
    {
        var timeRegister = context.TimeRegister;
        var policy = Policy(context);

        if (policy != null && policy.ShouldSignOut)
        {
            if (timeRegister.ActualOut == NoPunch)
            {
                return NoPunch;
            }

            return timeRegister.ActualOut.TimeOfDay <= timeRegister.ShiftEndTime1.TimeOfDay
                ? timeRegister.ActualOut
                : timeRegister.ShiftEndTime1;
        }

        return timeRegister.ShiftEndTime1;
    }

    /// <summary>
    /// Legacy <c>ActualTimeService.GetActualBreakStartTime</c> / <c>FlexiTimeService.GetActualBreakStartTime</c>.
    /// Unlike the shift variants above, these compare FULL <see cref="DateTime"/> values, not
    /// <see cref="DateTime.TimeOfDay"/> -- legacy asymmetry, preserved.
    /// </summary>
    protected static DateTime ResolveShiftBoundedActualBreakStartTime(TimeRegisterDayContext context)
    {
        var timeRegister = context.TimeRegister;
        var policy = Policy(context);

        if (policy != null && policy.ShouldBreakSignOut)
        {
            if (timeRegister.BreakOut == NoPunch)
            {
                return NoPunch;
            }

            return timeRegister.BreakOut <= timeRegister.BreakStartTime
                ? timeRegister.BreakOut
                : timeRegister.BreakStartTime;
        }

        return timeRegister.BreakStartTime;
    }

    /// <summary>
    /// Legacy <c>ActualTimeService.GetActualBreakEndTime</c> / <c>FlexiTimeService.GetActualBreakEndTime</c>.
    /// </summary>
    protected static DateTime ResolveShiftBoundedActualBreakEndTime(TimeRegisterDayContext context)
    {
        var timeRegister = context.TimeRegister;
        var policy = Policy(context);

        if (policy != null && policy.ShouldBreakSignIn)
        {
            if (timeRegister.BreakIn == NoPunch)
            {
                return NoPunch;
            }

            return timeRegister.BreakIn <= timeRegister.BreakEndTime
                ? timeRegister.BreakEndTime
                : timeRegister.BreakIn;
        }

        return timeRegister.BreakEndTime;
    }

    #endregion

    #region << Flexi window (FlexiTime strategy: punch resolution + total working minutes) >>

    /// <summary>
    /// Legacy <c>FlexiTimeService</c>'s repeated flexi window:
    /// <c>new DateTime(ShiftStartTime1.Y/M/D, Duty.FlexiStartTime.Hour, .Minute, 00)</c> ..
    /// <c>new DateTime(ShiftStartTime1.Y/M/D, Duty.FlexiEndTime.Hour, .Minute, 59)</c>, with the end rolled
    /// forward a day when it would otherwise precede the start (overnight flexi window).
    ///
    /// <para>PORT NOTE: legacy's <c>Duty.FlexiStartTime</c>/<c>FlexiEndTime</c> were non-nullable XPO
    /// <c>DateTime</c>s that defaulted to <c>DateTime.MinValue</c> (hour 0, minute 0) when unset; this port's
    /// <see cref="Duty.FlexiStartTime"/>/<see cref="Duty.FlexiEndTime"/> are <c>TimeSpan?</c>, so null is
    /// mapped to <see cref="TimeSpan.Zero"/> -- the same 00:00 legacy produced. Only Hours/Minutes are used
    /// (seconds are hard-coded 00/59), exactly as legacy.</para>
    /// </summary>
    protected static void GetFlexiWindow(
        TimeRegisterDayContext context,
        out DateTime windowStart,
        out DateTime windowEnd)
    {
        var duty = DutyOf(context);
        var shiftStart = context.TimeRegister.ShiftStartTime1;

        var flexiStart = duty?.FlexiStartTime ?? TimeSpan.Zero;
        var flexiEnd = duty?.FlexiEndTime ?? TimeSpan.Zero;

        windowStart = shiftStart.Date.Add(new TimeSpan(flexiStart.Hours, flexiStart.Minutes, 0));
        windowEnd = shiftStart.Date.Add(new TimeSpan(flexiEnd.Hours, flexiEnd.Minutes, 59));

        if (windowStart > windowEnd)
        {
            windowEnd = windowEnd.AddDays(1);
        }
    }

    #endregion
}
