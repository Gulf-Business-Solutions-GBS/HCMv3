using System;
using System.Threading.Tasks;
using Abp.Domain.Services;

namespace HCMv3.TimeManagement.Services;

/// <summary>
/// Resolves the eight "actual punch" <see cref="DateTime"/> fields of a <see cref="TimeRegister"/> row for one
/// day. This is the ONLY part of the Time Engine that dispatches on
/// <see cref="TimePolicy.TimeMgmtStatus"/> (legacy <c>Program.cs</c> lines ~1263-1316, the
/// <c>TimeMgmtStatus == ActualTime ? ActualTimeService.X : ... : TimesheetService.X</c> ternary chains).
///
/// <para>Wave T3 supplies four implementations, ported from the four legacy static classes:</para>
/// <list type="bullet">
/// <item><description><see cref="TimeMgmtStatus.ActualTime"/> -- legacy <c>ActualTimeService</c></description></item>
/// <item><description><see cref="TimeMgmtStatus.PlannedTime"/> -- legacy <c>PlannedTimeService</c></description></item>
/// <item><description><see cref="TimeMgmtStatus.FlexiTime"/> -- legacy <c>FlexiTimeService</c> (this one also
/// implements <see cref="IFlexiTimeCalculationOverride"/>)</description></item>
/// <item><description><see cref="TimeMgmtStatus.TimeSheet"/> -- legacy <c>TimesheetService</c>. Per legacy this
/// is the <c>else</c> fall-through branch, so it also serves <see cref="TimeMgmtStatus.NA"/>; see
/// <see cref="IPunchResolutionStrategyResolver"/>.</description></item>
/// </list>
///
/// <para><see cref="TimeMgmtStatus.VariableTime"/> deliberately has NO strategy: legacy
/// <c>CreateTimeRegister</c> throws "Employee has VariableTime Policy found on {date}" and skips the day
/// before ever reaching punch resolution (legacy <c>Program.cs</c> lines ~506-515).</para>
///
/// <para><b>Return-value convention.</b> Legacy returned <c>DateTimeExtension.DBDtNull</c> (an XPO
/// "unset datetime" sentinel) for "no punch". This port uses <c>default(DateTime)</c>
/// (<see cref="DateTime.MinValue"/>) for the same meaning, matching how <see cref="TimeRegister"/>'s
/// non-nullable <c>DateTime</c> columns behave when never assigned.</para>
///
/// <para>The four legacy list parameters (<c>empTimeEvent</c>, <c>empAbsenceList</c>) live on
/// <see cref="TimeEngineEmployeeContext.TimeEvents"/>/<see cref="TimeEngineEmployeeContext.Absences"/>.
/// Note <c>TimeEvents</c> may be <c>null</c> (legacy's <c>loadTimeEvent</c> guard) -- handle that.</para>
/// </summary>
public interface IPunchResolutionStrategy : IDomainService
{
    /// <summary>The policy mode this strategy serves; used by <see cref="IPunchResolutionStrategyResolver"/>.</summary>
    TimeMgmtStatus TimeMgmtStatus { get; }

    /// <summary>Legacy <c>{Xxx}TimeService.GetActualIn</c>.</summary>
    Task<DateTime> GetActualInAsync(TimeRegisterDayContext context);

    /// <summary>Legacy <c>{Xxx}TimeService.GetBreakOut</c>.</summary>
    Task<DateTime> GetBreakOutAsync(TimeRegisterDayContext context);

    /// <summary>Legacy <c>{Xxx}TimeService.GetBreakIn</c>.</summary>
    Task<DateTime> GetBreakInAsync(TimeRegisterDayContext context);

    /// <summary>Legacy <c>{Xxx}TimeService.GetActualOut</c>.</summary>
    Task<DateTime> GetActualOutAsync(TimeRegisterDayContext context);

    /// <summary>Legacy <c>{Xxx}TimeService.GetActualStartTime</c> (assigned to <see cref="TimeRegister.ActualStartTime1"/>).</summary>
    Task<DateTime> GetActualStartTimeAsync(TimeRegisterDayContext context);

    /// <summary>Legacy <c>{Xxx}TimeService.GetActualBreakStartTime</c>.</summary>
    Task<DateTime> GetActualBreakStartTimeAsync(TimeRegisterDayContext context);

    /// <summary>Legacy <c>{Xxx}TimeService.GetActualBreakEndTime</c>.</summary>
    Task<DateTime> GetActualBreakEndTimeAsync(TimeRegisterDayContext context);

    /// <summary>Legacy <c>{Xxx}TimeService.GetActualEndTime</c> (assigned to <see cref="TimeRegister.ActualEndTime1"/>).</summary>
    Task<DateTime> GetActualEndTimeAsync(TimeRegisterDayContext context);
}

/// <summary>
/// Picks the <see cref="IPunchResolutionStrategy"/> for a day's <see cref="TimeMgmtStatus"/>, reproducing the
/// legacy ternary chain's fall-through: ActualTime / PlannedTime / FlexiTime get their own strategy, and
/// EVERYTHING ELSE falls through to the TimeSheet strategy (legacy's final <c>: TimesheetService.X</c> arm).
/// </summary>
public interface IPunchResolutionStrategyResolver : IDomainService
{
    /// <summary>Never returns null; unknown/NA statuses resolve to the TimeSheet strategy (legacy fall-through).</summary>
    IPunchResolutionStrategy Resolve(TimeMgmtStatus timeMgmtStatus);
}

/// <summary>
/// The three (plus one unused) working-time calculations that legacy overrides for FlexiTime ONLY -- implemented
/// exclusively by the <see cref="TimeMgmtStatus.FlexiTime"/> strategy in wave T3.
///
/// <para>Legacy expresses these as inline <c>TimeMgmtStatus == FlexiTime ? FlexiTimeService.X(...) :
/// GeneralTimeService.X(...)</c> ternaries at legacy <c>Program.cs</c> lines ~1345-1355 (TotalWorkingMins,
/// ActualAbsenceMins) and ~1377-1380 (OvertimeMinutes). Keeping them off
/// <see cref="IPunchResolutionStrategy"/> avoids forcing the other three strategies to write
/// never-called stubs; the orchestrator does
/// <c>strategy as IFlexiTimeCalculationOverride</c> guarded by the same <c>TimeMgmtStatus == FlexiTime</c>
/// check legacy uses, so behavior is identical.</para>
/// </summary>
public interface IFlexiTimeCalculationOverride
{
    /// <summary>Legacy <c>FlexiTimeService.GetTotalWorkingMinutes(objectSpace, timeRegister, empTimeEvent)</c>.</summary>
    Task<double> GetTotalWorkingMinutesAsync(TimeRegisterDayContext context);

    /// <summary>Legacy <c>FlexiTimeService.GetActualAbsenceMinutes(objectSpace, timeRegister, IsNotReturnFromLeave)</c>.</summary>
    Task<double> GetActualAbsenceMinutesAsync(TimeRegisterDayContext context);

    /// <summary>Legacy <c>FlexiTimeService.GetOvertimeMinutes(objectSpace, timeRegister)</c>.</summary>
    Task<double> GetOvertimeMinutesAsync(TimeRegisterDayContext context);

    /// <summary>
    /// Legacy <c>FlexiTimeService.GetAbsentDay(objectSpace, timeRegister)</c>. DEAD IN THE ORCHESTRATOR:
    /// legacy <c>CreateTimeRegister</c> calls <c>GeneralTimeService.GetAbsentDay</c> unconditionally (line
    /// ~1368) and never this one. Declared for completeness of the T3 port; the orchestrator does not call it.
    /// </summary>
    Task<double> GetAbsentDayAsync(TimeRegisterDayContext context);
}
