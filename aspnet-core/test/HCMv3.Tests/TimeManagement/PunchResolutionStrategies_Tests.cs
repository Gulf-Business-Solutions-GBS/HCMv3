using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Services;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TimeManagement;

/// <summary>
/// Wave T3 -- unit tests for the four <see cref="IPunchResolutionStrategy"/> implementations and the
/// <see cref="IPunchResolutionStrategyResolver"/>, ported alongside legacy <c>ActualTimeService</c>,
/// <c>PlannedTimeService</c>, <c>FlexiTimeService</c> and <c>TimesheetService</c>.
///
/// Only <see cref="TimeSheetPunchResolutionStrategy"/> touches the database (via
/// <see cref="EmployeeTimesheet"/>); the other three are pure functions of
/// <see cref="TimeRegisterDayContext"/>, so most tests build a context in memory and assert directly.
/// </summary>
// ReSharper disable once InconsistentNaming
public class PunchResolutionStrategies_Tests : TimeManagementAppServiceTestBase
{
    private const int ClockInId = 11;
    private const int ClockOutId = 22;

    private static readonly DateTime Day = new DateTime(2025, 3, 10);

    #region << Context builders >>

    private static TimePolicy BuildPolicy(
        TimeEventRule rule = TimeEventRule.FirstInLastOut,
        TimeMgmtStatus status = TimeMgmtStatus.ActualTime,
        bool shouldSignIn = true,
        bool shouldSignOut = true,
        bool shouldBreakSignIn = true,
        bool shouldBreakSignOut = true)
    {
        return new TimePolicy
        {
            TenantId = 1,
            Code = "TP1",
            NameEn = "Policy",
            NameAr = "Policy",
            TimeEventRule = rule,
            TimeMgmtStatus = status,
            ShouldSignIn = shouldSignIn,
            ShouldSignOut = shouldSignOut,
            ShouldBreakSignIn = shouldBreakSignIn,
            ShouldBreakSignOut = shouldBreakSignOut,
            ClockInLimit = 60,
            ClockOutLimit = 60,
            BreakInLimit = 30,
            BreakOutLimit = 30,
            OTMinLimit = 0
        };
    }

    private static Duty BuildDuty(bool isWeeklyOff = false, bool isRestDay = false, BreakShift breakShift = null)
    {
        return new Duty
        {
            TenantId = 1,
            Code = "D1",
            NameEn = "Duty",
            NameAr = "Duty",
            StartTime = new TimeSpan(8, 0, 0),
            EndTime = new TimeSpan(16, 0, 0),
            IsWeeklyOff = isWeeklyOff,
            IsRestDay = isRestDay,
            BreakShift = breakShift,
            FlexiStartTime = new TimeSpan(6, 0, 0),
            FlexiEndTime = new TimeSpan(20, 0, 0)
        };
    }

    private static TimePunchEvent Punch(int transTypeId, DateTime at)
    {
        return new TimePunchEvent
        {
            EmployeeId = 1,
            TransTypeId = transTypeId,
            TransDate = at.Date,
            TransTime = at,
            TransDateTime = at
        };
    }

    /// <summary>
    /// Builds a day context with an 08:00-16:00 shift and a 12:00-13:00 break, mirroring what the
    /// orchestrator would have already assigned to <see cref="TimeRegister"/> before punch resolution runs.
    /// </summary>
    private static TimeRegisterDayContext BuildContext(
        TimePolicy policy = null,
        Duty duty = null,
        List<TimePunchEvent> punches = null,
        List<EmployeeAbsence> absences = null,
        TimeEngineSettings settings = null,
        List<AbsenceType> absenceTypes = null)
    {
        policy ??= BuildPolicy();
        duty ??= BuildDuty();

        var timeRegister = new TimeRegister
        {
            TenantId = 1,
            DayDate = Day,
            ShiftStartTime1 = Day.AddHours(8),
            ShiftEndTime1 = Day.AddHours(16),
            BreakStartTime = Day.AddHours(12),
            BreakEndTime = Day.AddHours(13)
        };

        return new TimeRegisterDayContext
        {
            TimeRegister = timeRegister,
            DayDate = Day,
            Duty = duty,
            TimePolicy = policy,
            Settings = settings ?? new TimeEngineSettings(),
            MasterData = new TimeEngineMasterData
            {
                ClockInTransTypeId = ClockInId,
                ClockOutTransTypeId = ClockOutId,
                AbsenceTypes = absenceTypes ?? new List<AbsenceType>()
            },
            EmployeeContext = new TimeEngineEmployeeContext
            {
                TimeEvents = punches,
                Absences = absences ?? new List<EmployeeAbsence>()
            }
        };
    }

    #endregion

    #region << Resolver >>

    private PunchResolutionStrategyResolver BuildResolver()
    {
        return new PunchResolutionStrategyResolver(new List<IPunchResolutionStrategy>
        {
            new ActualTimePunchResolutionStrategy(),
            new PlannedTimePunchResolutionStrategy(),
            new FlexiTimePunchResolutionStrategy(),
            new TimeSheetPunchResolutionStrategy(FakeRepository.For<EmployeeTimesheet>(Context))
        });
    }

    [Fact]
    public void Resolver_Should_Return_Matching_Strategy_For_The_Three_Named_Statuses()
    {
        var resolver = BuildResolver();

        resolver.Resolve(TimeMgmtStatus.ActualTime).ShouldBeOfType<ActualTimePunchResolutionStrategy>();
        resolver.Resolve(TimeMgmtStatus.PlannedTime).ShouldBeOfType<PlannedTimePunchResolutionStrategy>();
        resolver.Resolve(TimeMgmtStatus.FlexiTime).ShouldBeOfType<FlexiTimePunchResolutionStrategy>();
    }

    [Theory]
    [InlineData(TimeMgmtStatus.TimeSheet)]
    [InlineData(TimeMgmtStatus.NA)]
    [InlineData(TimeMgmtStatus.VariableTime)]
    [InlineData((TimeMgmtStatus)999)]
    public void Resolver_Should_Fall_Through_To_TimeSheet_For_Everything_Else(TimeMgmtStatus status)
    {
        var resolver = BuildResolver();

        resolver.Resolve(status).ShouldBeOfType<TimeSheetPunchResolutionStrategy>();
    }

    [Fact]
    public void Resolver_Should_Throw_When_The_Mandatory_TimeSheet_Fall_Through_Strategy_Is_Missing()
    {
        var resolver = new PunchResolutionStrategyResolver(new List<IPunchResolutionStrategy>
        {
            new ActualTimePunchResolutionStrategy()
        });

        Should.Throw<InvalidOperationException>(() => resolver.Resolve(TimeMgmtStatus.NA));
    }

    #endregion

    #region << ActualTime strategy >>

    [Fact]
    public async Task ActualTime_GetActualIn_Should_Take_First_Punch_Inside_ClockIn_Window_For_FirstIn_Rules()
    {
        var strategy = new ActualTimePunchResolutionStrategy();
        var context = BuildContext(
            policy: BuildPolicy(TimeEventRule.FirstInLastOut),
            punches: new List<TimePunchEvent>
            {
                Punch(ClockInId, Day.AddHours(7).AddMinutes(30)),
                Punch(ClockInId, Day.AddHours(8).AddMinutes(20))
            });

        var actualIn = await strategy.GetActualInAsync(context);

        actualIn.ShouldBe(Day.AddHours(7).AddMinutes(30));
    }

    [Fact]
    public async Task ActualTime_GetActualIn_Should_Take_Last_Punch_For_LastIn_Rules()
    {
        var strategy = new ActualTimePunchResolutionStrategy();
        var context = BuildContext(
            policy: BuildPolicy(TimeEventRule.LastInFirstOut),
            punches: new List<TimePunchEvent>
            {
                Punch(ClockInId, Day.AddHours(7).AddMinutes(30)),
                Punch(ClockInId, Day.AddHours(8).AddMinutes(20))
            });

        var actualIn = await strategy.GetActualInAsync(context);

        actualIn.ShouldBe(Day.AddHours(8).AddMinutes(20));
    }

    [Fact]
    public async Task ActualTime_GetActualIn_Should_Ignore_Punches_Outside_The_ClockIn_Window()
    {
        var strategy = new ActualTimePunchResolutionStrategy();

        // ClockInLimit is 60 minutes around the 08:00 shift start -> 06:30 is outside.
        var context = BuildContext(
            punches: new List<TimePunchEvent> { Punch(ClockInId, Day.AddHours(6).AddMinutes(30)) });

        var actualIn = await strategy.GetActualInAsync(context);

        actualIn.ShouldBe(default(DateTime));
    }

    [Fact]
    public async Task ActualTime_GetActualIn_Should_Widen_The_Window_To_The_Whole_Day_On_A_WeeklyOff()
    {
        var strategy = new ActualTimePunchResolutionStrategy();
        var context = BuildContext(
            duty: BuildDuty(isWeeklyOff: true),
            punches: new List<TimePunchEvent> { Punch(ClockInId, Day.AddHours(6).AddMinutes(30)) });

        var actualIn = await strategy.GetActualInAsync(context);

        actualIn.ShouldBe(Day.AddHours(6).AddMinutes(30));
    }

    [Fact]
    public async Task ActualTime_GetActualIn_Should_Return_No_Punch_When_TimeEvents_Is_Null()
    {
        var strategy = new ActualTimePunchResolutionStrategy();
        var context = BuildContext(punches: null);

        (await strategy.GetActualInAsync(context)).ShouldBe(default(DateTime));
        (await strategy.GetActualOutAsync(context)).ShouldBe(default(DateTime));
        (await strategy.GetBreakInAsync(context)).ShouldBe(default(DateTime));
        (await strategy.GetBreakOutAsync(context)).ShouldBe(default(DateTime));
    }

    [Fact]
    public async Task ActualTime_GetActualIn_Should_Fall_Back_To_Shift_Start_When_An_Hourly_Leave_Covers_The_Punch_In()
    {
        var strategy = new ActualTimePunchResolutionStrategy();
        var context = BuildContext(
            punches: new List<TimePunchEvent>(),
            absences: new List<EmployeeAbsence>
            {
                new EmployeeAbsence
                {
                    TenantId = 1,
                    StartDate = Day,
                    StartTime = Day.AddHours(7),
                    EndTime = Day.AddHours(12),
                    AbsenceHours = 4
                }
            },
            settings: new TimeEngineSettings
            {
                IsHourlyLeaveLogicEnabled = true,
                ShouldSignInAfterHourlyLeave = false
            });

        var actualIn = await strategy.GetActualInAsync(context);

        actualIn.ShouldBe(Day.AddHours(8));
    }

    [Fact]
    public async Task ActualTime_GetActualIn_Should_Use_Any_Punch_Of_The_Day_When_ShouldSignInAfterHourlyLeave_Is_On()
    {
        var strategy = new ActualTimePunchResolutionStrategy();

        // 06:30 is outside the tolerance window, so the primary lookup finds nothing; the hourly-leave
        // branch then re-queries with a whole-day (TransDate) filter and picks it up.
        var context = BuildContext(
            punches: new List<TimePunchEvent> { Punch(ClockInId, Day.AddHours(6).AddMinutes(30)) },
            absences: new List<EmployeeAbsence>
            {
                new EmployeeAbsence
                {
                    TenantId = 1,
                    StartDate = Day,
                    StartTime = Day.AddHours(7),
                    EndTime = Day.AddHours(12),
                    AbsenceHours = 4
                }
            },
            settings: new TimeEngineSettings
            {
                IsHourlyLeaveLogicEnabled = true,
                ShouldSignInAfterHourlyLeave = true
            });

        var actualIn = await strategy.GetActualInAsync(context);

        actualIn.ShouldBe(Day.AddHours(6).AddMinutes(30));
    }

    [Fact]
    public async Task ActualTime_GetActualOut_Should_Take_Last_Punch_For_LastOut_Rules()
    {
        var strategy = new ActualTimePunchResolutionStrategy();
        var context = BuildContext(
            policy: BuildPolicy(TimeEventRule.FirstInLastOut),
            punches: new List<TimePunchEvent>
            {
                Punch(ClockOutId, Day.AddHours(15).AddMinutes(30)),
                Punch(ClockOutId, Day.AddHours(16).AddMinutes(10))
            });

        var actualOut = await strategy.GetActualOutAsync(context);

        actualOut.ShouldBe(Day.AddHours(16).AddMinutes(10));
    }

    [Fact]
    public async Task ActualTime_GetBreakOut_And_GetBreakIn_Should_Use_The_Policy_Break_Windows()
    {
        var strategy = new ActualTimePunchResolutionStrategy();
        var context = BuildContext(
            punches: new List<TimePunchEvent>
            {
                Punch(ClockOutId, Day.AddHours(11).AddMinutes(50)),
                Punch(ClockInId, Day.AddHours(13).AddMinutes(5))
            });

        (await strategy.GetBreakOutAsync(context)).ShouldBe(Day.AddHours(11).AddMinutes(50));
        (await strategy.GetBreakInAsync(context)).ShouldBe(Day.AddHours(13).AddMinutes(5));
    }

    [Fact]
    public async Task ActualTime_GetActualStartTime_Should_Clamp_Early_Punches_Up_To_The_Shift_Start()
    {
        var strategy = new ActualTimePunchResolutionStrategy();
        var context = BuildContext();
        context.TimeRegister.ActualIn = Day.AddHours(7).AddMinutes(45);

        (await strategy.GetActualStartTimeAsync(context)).ShouldBe(Day.AddHours(8));
    }

    [Fact]
    public async Task ActualTime_GetActualStartTime_Should_Keep_Late_Punches()
    {
        var strategy = new ActualTimePunchResolutionStrategy();
        var context = BuildContext();
        context.TimeRegister.ActualIn = Day.AddHours(8).AddMinutes(45);

        (await strategy.GetActualStartTimeAsync(context)).ShouldBe(Day.AddHours(8).AddMinutes(45));
    }

    [Fact]
    public async Task ActualTime_GetActualStartTime_Should_Return_Shift_Start_When_SignIn_Is_Not_Required()
    {
        var strategy = new ActualTimePunchResolutionStrategy();
        var context = BuildContext(policy: BuildPolicy(shouldSignIn: false));

        (await strategy.GetActualStartTimeAsync(context)).ShouldBe(Day.AddHours(8));
    }

    [Fact]
    public async Task ActualTime_GetActualEndTime_Should_Clamp_Late_Punches_Down_To_The_Shift_End()
    {
        var strategy = new ActualTimePunchResolutionStrategy();
        var context = BuildContext();
        context.TimeRegister.ActualOut = Day.AddHours(16).AddMinutes(45);

        (await strategy.GetActualEndTimeAsync(context)).ShouldBe(Day.AddHours(16));
    }

    [Fact]
    public async Task ActualTime_Break_Clamps_Should_Follow_Legacy_Direction()
    {
        var strategy = new ActualTimePunchResolutionStrategy();
        var context = BuildContext();
        context.TimeRegister.BreakOut = Day.AddHours(12).AddMinutes(15);
        context.TimeRegister.BreakIn = Day.AddHours(12).AddMinutes(45);

        // Late break-out is clamped back to the planned break start.
        (await strategy.GetActualBreakStartTimeAsync(context)).ShouldBe(Day.AddHours(12));

        // Early break-in is clamped forward to the planned break end.
        (await strategy.GetActualBreakEndTimeAsync(context)).ShouldBe(Day.AddHours(13));
    }

    #endregion

    #region << PlannedTime strategy >>

    [Fact]
    public async Task PlannedTime_Should_Not_Look_At_Punches_When_UseActualTransInPlannedTimePolicy_Is_Off()
    {
        var strategy = new PlannedTimePunchResolutionStrategy();
        var context = BuildContext(
            policy: BuildPolicy(status: TimeMgmtStatus.PlannedTime),
            punches: new List<TimePunchEvent> { Punch(ClockInId, Day.AddHours(8)) });

        (await strategy.GetActualInAsync(context)).ShouldBe(default(DateTime));
        (await strategy.GetActualOutAsync(context)).ShouldBe(default(DateTime));
    }

    [Fact]
    public async Task PlannedTime_Should_Use_The_Duty_Flexi_Window_When_UseActualTransInPlannedTimePolicy_Is_On()
    {
        var strategy = new PlannedTimePunchResolutionStrategy();
        var context = BuildContext(
            policy: BuildPolicy(TimeEventRule.FirstInFirstOut, TimeMgmtStatus.PlannedTime),
            punches: new List<TimePunchEvent>
            {
                Punch(ClockInId, Day.AddHours(7)),
                Punch(ClockOutId, Day.AddHours(17))
            },
            settings: new TimeEngineSettings { UseActualTransInPlannedTimePolicy = true });

        (await strategy.GetActualInAsync(context)).ShouldBe(Day.AddHours(7));
        (await strategy.GetActualOutAsync(context)).ShouldBe(Day.AddHours(17));
    }

    [Fact]
    public async Task PlannedTime_Break_Punches_Are_Always_Empty_And_Actual_Times_Are_The_Planned_Ones()
    {
        var strategy = new PlannedTimePunchResolutionStrategy();
        var context = BuildContext(policy: BuildPolicy(status: TimeMgmtStatus.PlannedTime));

        (await strategy.GetBreakOutAsync(context)).ShouldBe(default(DateTime));
        (await strategy.GetBreakInAsync(context)).ShouldBe(default(DateTime));
        (await strategy.GetActualStartTimeAsync(context)).ShouldBe(Day.AddHours(8));
        (await strategy.GetActualEndTimeAsync(context)).ShouldBe(Day.AddHours(16));
        (await strategy.GetActualBreakStartTimeAsync(context)).ShouldBe(Day.AddHours(12));
        (await strategy.GetActualBreakEndTimeAsync(context)).ShouldBe(Day.AddHours(13));
    }

    #endregion

    #region << FlexiTime strategy >>

    [Fact]
    public async Task FlexiTime_GetActualIn_Should_Use_The_Duty_Flexi_Window_Not_The_Policy_Tolerance()
    {
        var strategy = new FlexiTimePunchResolutionStrategy();

        // 06:30 is outside the 60-minute ClockIn tolerance but inside the 06:00-20:00 flexi window.
        var context = BuildContext(
            policy: BuildPolicy(TimeEventRule.FirstInFirstOut, TimeMgmtStatus.FlexiTime),
            punches: new List<TimePunchEvent> { Punch(ClockInId, Day.AddHours(6).AddMinutes(30)) });

        (await strategy.GetActualInAsync(context)).ShouldBe(Day.AddHours(6).AddMinutes(30));
    }

    [Fact]
    public async Task FlexiTime_GetActualIn_Should_Ignore_Punches_Before_The_Flexi_Window()
    {
        var strategy = new FlexiTimePunchResolutionStrategy();
        var context = BuildContext(
            policy: BuildPolicy(TimeEventRule.FirstInFirstOut, TimeMgmtStatus.FlexiTime),
            punches: new List<TimePunchEvent> { Punch(ClockInId, Day.AddHours(5)) });

        (await strategy.GetActualInAsync(context)).ShouldBe(default(DateTime));
    }

    [Fact]
    public async Task FlexiTime_GetTotalWorkingMinutes_Should_Sum_Paired_Punches()
    {
        var strategy = new FlexiTimePunchResolutionStrategy();
        var context = BuildContext(
            policy: BuildPolicy(TimeEventRule.FirstInFirstOut, TimeMgmtStatus.FlexiTime),
            punches: new List<TimePunchEvent>
            {
                Punch(ClockInId, Day.AddHours(8)),
                Punch(ClockOutId, Day.AddHours(12)),
                Punch(ClockInId, Day.AddHours(13)),
                Punch(ClockOutId, Day.AddHours(17))
            });

        context.TimeRegister.ActualIn = Day.AddHours(8);
        context.TimeRegister.ActualOut = Day.AddHours(17);

        (await strategy.GetTotalWorkingMinutesAsync(context)).ShouldBe(480);
    }

    [Fact]
    public async Task FlexiTime_GetTotalWorkingMinutes_Should_Be_Zero_When_Either_Actual_Punch_Is_Missing()
    {
        var strategy = new FlexiTimePunchResolutionStrategy();
        var context = BuildContext(
            policy: BuildPolicy(TimeEventRule.FirstInFirstOut, TimeMgmtStatus.FlexiTime),
            punches: new List<TimePunchEvent>
            {
                Punch(ClockInId, Day.AddHours(8)),
                Punch(ClockOutId, Day.AddHours(12))
            });

        context.TimeRegister.ActualIn = Day.AddHours(8);
        context.TimeRegister.ActualOut = default;

        (await strategy.GetTotalWorkingMinutesAsync(context)).ShouldBe(0);
    }

    [Fact]
    public async Task FlexiTime_GetTotalWorkingMinutes_Should_Only_Open_An_Interval_On_The_Last_Of_Consecutive_ClockIns()
    {
        // LEGACY QUIRK under test: with 08:00-in, 09:00-in, 12:00-out legacy discards the 08:00 punch
        // because its immediate successor is also a clock-in -- so the day is 3 hours, not 4.
        var strategy = new FlexiTimePunchResolutionStrategy();
        var context = BuildContext(
            policy: BuildPolicy(TimeEventRule.FirstInFirstOut, TimeMgmtStatus.FlexiTime),
            punches: new List<TimePunchEvent>
            {
                Punch(ClockInId, Day.AddHours(8)),
                Punch(ClockInId, Day.AddHours(9)),
                Punch(ClockOutId, Day.AddHours(12))
            });

        context.TimeRegister.ActualIn = Day.AddHours(8);
        context.TimeRegister.ActualOut = Day.AddHours(12);

        (await strategy.GetTotalWorkingMinutesAsync(context)).ShouldBe(180);
    }

    [Fact]
    public async Task FlexiTime_GetOvertimeMinutes_Should_Zero_Out_Below_The_Policy_Threshold()
    {
        var strategy = new FlexiTimePunchResolutionStrategy();
        var policy = BuildPolicy(status: TimeMgmtStatus.FlexiTime);
        policy.OTMinLimit = 30;

        var context = BuildContext(policy: policy);
        context.TimeRegister.PlannedWorkingMins = 480;
        context.TimeRegister.TotalWorkingMins = 500;

        (await strategy.GetOvertimeMinutesAsync(context)).ShouldBe(0);

        context.TimeRegister.TotalWorkingMins = 520;
        (await strategy.GetOvertimeMinutesAsync(context)).ShouldBe(40);
    }

    [Fact]
    public async Task FlexiTime_GetOvertimeMinutes_Should_Never_Be_Negative()
    {
        var strategy = new FlexiTimePunchResolutionStrategy();
        var context = BuildContext(policy: BuildPolicy(status: TimeMgmtStatus.FlexiTime));
        context.TimeRegister.PlannedWorkingMins = 480;
        context.TimeRegister.TotalWorkingMins = 100;

        (await strategy.GetOvertimeMinutesAsync(context)).ShouldBe(0);
    }

    [Fact]
    public async Task FlexiTime_GetActualAbsenceMinutes_Should_Be_The_Full_Planned_Day_When_A_Punch_Is_Missing()
    {
        var strategy = new FlexiTimePunchResolutionStrategy();
        var context = BuildContext(policy: BuildPolicy(status: TimeMgmtStatus.FlexiTime));
        context.TimeRegister.PlannedWorkingMins = 480;
        context.TimeRegister.ActualIn = Day.AddHours(8);
        context.TimeRegister.ActualOut = default;

        (await strategy.GetActualAbsenceMinutesAsync(context)).ShouldBe(480);
    }

    [Fact]
    public async Task FlexiTime_GetActualAbsenceMinutes_Should_Be_Planned_Minus_Worked()
    {
        var strategy = new FlexiTimePunchResolutionStrategy();
        var context = BuildContext(policy: BuildPolicy(status: TimeMgmtStatus.FlexiTime));
        context.TimeRegister.PlannedWorkingMins = 480;
        context.TimeRegister.TotalWorkingMins = 400;
        context.TimeRegister.ActualIn = Day.AddHours(8);
        context.TimeRegister.ActualOut = Day.AddHours(15);

        (await strategy.GetActualAbsenceMinutesAsync(context)).ShouldBe(80);
    }

    [Fact]
    public async Task FlexiTime_GetActualAbsenceMinutes_Should_Add_Unpaid_Leave_Minutes()
    {
        var absenceType = new AbsenceType
        {
            TenantId = 1,
            Code = "2",
            NameEn = "Unpaid",
            NameAr = "Unpaid",
            IsPaid = false,

            // Not persisted; the id is set manually so the MasterData lookup matches TimeRegister.LeaveTypeId.
            Id = 7
        };

        var strategy = new FlexiTimePunchResolutionStrategy();
        var context = BuildContext(
            policy: BuildPolicy(status: TimeMgmtStatus.FlexiTime),
            absenceTypes: new List<AbsenceType> { absenceType });

        context.TimeRegister.PlannedWorkingMins = 480;
        context.TimeRegister.TotalWorkingMins = 400;
        context.TimeRegister.ActualIn = Day.AddHours(8);
        context.TimeRegister.ActualOut = Day.AddHours(15);
        context.TimeRegister.LeaveDay = 1;
        context.TimeRegister.LeaveMinutes = 60;
        context.TimeRegister.LeaveTypeId = absenceType.Id;

        (await strategy.GetActualAbsenceMinutesAsync(context)).ShouldBe(140);
    }

    [Fact]
    public async Task FlexiTime_GetActualAbsenceMinutes_Should_Be_The_Full_Planned_Day_When_Not_Returned_From_Leave()
    {
        var strategy = new FlexiTimePunchResolutionStrategy();
        var context = BuildContext(policy: BuildPolicy(status: TimeMgmtStatus.FlexiTime));
        context.IsNotReturnFromLeave = true;
        context.TimeRegister.PlannedWorkingMins = 480;
        context.TimeRegister.TotalWorkingMins = 400;
        context.TimeRegister.ActualIn = Day.AddHours(8);
        context.TimeRegister.ActualOut = Day.AddHours(15);

        (await strategy.GetActualAbsenceMinutesAsync(context)).ShouldBe(480);
    }

    [Fact]
    public async Task FlexiTime_GetAbsentDay_Should_Be_One_Only_For_A_Planned_Day_With_No_Work_And_No_Leave()
    {
        var strategy = new FlexiTimePunchResolutionStrategy();
        var context = BuildContext(policy: BuildPolicy(status: TimeMgmtStatus.FlexiTime));
        context.TimeRegister.PlannedWorkingDay = 1;
        context.TimeRegister.ActualWorkingMins = 0;
        context.TimeRegister.LeaveDay = 0;

        (await strategy.GetAbsentDayAsync(context)).ShouldBe(1);

        context.TimeRegister.LeaveDay = 1;
        (await strategy.GetAbsentDayAsync(context)).ShouldBe(0);

        context.TimeRegister.LeaveDay = 0;
        context.TimeRegister.ActualWorkingMins = 60;
        (await strategy.GetAbsentDayAsync(context)).ShouldBe(0);
    }

    [Fact]
    public void FlexiTime_Should_Be_The_Only_Strategy_Implementing_IFlexiTimeCalculationOverride()
    {
        (new FlexiTimePunchResolutionStrategy() as IFlexiTimeCalculationOverride).ShouldNotBeNull();
        (new ActualTimePunchResolutionStrategy() as IFlexiTimeCalculationOverride).ShouldBeNull();
        (new PlannedTimePunchResolutionStrategy() as IFlexiTimeCalculationOverride).ShouldBeNull();
        (new TimeSheetPunchResolutionStrategy(FakeRepository.For<EmployeeTimesheet>(Context))
            as IFlexiTimeCalculationOverride).ShouldBeNull();
    }

    #endregion

    #region << TimeSheet strategy >>

    private Employee CreateEmployee()
    {
        var nationality = new HCMv3.GeneralMasterInfo.Nationality
        {
            TenantId = AbpSession.TenantId ?? 1,
            NameEn = "Local",
            NameAr = "Local"
        };
        Context.Nationalities.Add(nationality);
        Context.SaveChanges();

        var employee = new Employee
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = "E1",
            FirstNameEn = "John",
            LastNameEn = "Doe",
            FirstNameAr = "John",
            LastNameAr = "Doe",
            FullNameEn = "John Doe",
            FullNameAr = "John Doe",
            NationalityId = nationality.Id
        };
        Context.Employees.Add(employee);
        Context.SaveChanges();
        return employee;
    }

    private TimeSheetPunchResolutionStrategy CreateTimeSheetStrategyWithRow()
    {
        var employee = CreateEmployee();

        Context.EmployeeTimesheets.Add(new EmployeeTimesheet
        {
            TenantId = AbpSession.TenantId ?? 1,
            EmployeeId = employee.Id,
            TransDate = Day,
            ActualStartTime = new TimeSpan(8, 15, 0),
            BreakStartTime = new TimeSpan(12, 5, 0),
            BreakEndTime = new TimeSpan(12, 50, 0),
            ActualEndTime = new TimeSpan(16, 30, 0),
            TotalHours = 8
        });
        Context.SaveChanges();

        return new TimeSheetPunchResolutionStrategy(FakeRepository.For<EmployeeTimesheet>(Context));
    }

    [Fact]
    public async Task TimeSheet_GetActualIn_Should_Come_From_The_Timesheet_Row()
    {
        var strategy = CreateTimeSheetStrategyWithRow();
        var context = BuildContext(policy: BuildPolicy(status: TimeMgmtStatus.TimeSheet));

        (await strategy.GetActualInAsync(context)).ShouldBe(Day.AddHours(8).AddMinutes(15));
    }

    [Fact]
    public async Task TimeSheet_GetBreakOut_Should_Come_From_The_Timesheet_Row()
    {
        var strategy = CreateTimeSheetStrategyWithRow();
        var context = BuildContext(policy: BuildPolicy(status: TimeMgmtStatus.TimeSheet));

        (await strategy.GetBreakOutAsync(context)).ShouldBe(Day.AddHours(12).AddMinutes(5));
    }

    [Fact]
    public async Task TimeSheet_GetBreakIn_And_GetActualOut_Should_Reproduce_The_Legacy_GetActualIn_Quirk()
    {
        // Legacy Program.cs ~1285 and ~1293 both call TimesheetService.GetActualIn on the TimeSheet arm,
        // so BreakIn and ActualOut receive the timesheet's ActualStartTime (08:15), NOT BreakEndTime
        // (12:50) or ActualEndTime (16:30). Preserved deliberately -- see the strategy's XML remarks.
        var strategy = CreateTimeSheetStrategyWithRow();
        var context = BuildContext(policy: BuildPolicy(status: TimeMgmtStatus.TimeSheet));

        (await strategy.GetBreakInAsync(context)).ShouldBe(Day.AddHours(8).AddMinutes(15));
        (await strategy.GetActualOutAsync(context)).ShouldBe(Day.AddHours(8).AddMinutes(15));
    }

    [Fact]
    public async Task TimeSheet_Should_Return_No_Punch_When_There_Is_No_Timesheet_Row_For_The_Day()
    {
        var strategy = new TimeSheetPunchResolutionStrategy(FakeRepository.For<EmployeeTimesheet>(Context));
        var context = BuildContext(policy: BuildPolicy(status: TimeMgmtStatus.TimeSheet));

        (await strategy.GetActualInAsync(context)).ShouldBe(default(DateTime));
        (await strategy.GetBreakOutAsync(context)).ShouldBe(default(DateTime));
        (await strategy.GetBreakInAsync(context)).ShouldBe(default(DateTime));
        (await strategy.GetActualOutAsync(context)).ShouldBe(default(DateTime));
    }

    [Fact]
    public async Task TimeSheet_Actual_Times_Should_Ignore_The_ShouldSign_Flags()
    {
        // Unlike ActualTime/FlexiTime, the timesheet clamps run regardless of ShouldSignIn/ShouldSignOut.
        var strategy = new TimeSheetPunchResolutionStrategy(FakeRepository.For<EmployeeTimesheet>(Context));
        var context = BuildContext(
            policy: BuildPolicy(
                status: TimeMgmtStatus.TimeSheet,
                shouldSignIn: false,
                shouldSignOut: false,
                shouldBreakSignIn: false,
                shouldBreakSignOut: false));

        context.TimeRegister.ActualIn = Day.AddHours(7).AddMinutes(45);
        context.TimeRegister.ActualOut = Day.AddHours(16).AddMinutes(45);
        context.TimeRegister.BreakOut = Day.AddHours(11).AddMinutes(45);
        context.TimeRegister.BreakIn = Day.AddHours(13).AddMinutes(15);

        (await strategy.GetActualStartTimeAsync(context)).ShouldBe(Day.AddHours(8));
        (await strategy.GetActualEndTimeAsync(context)).ShouldBe(Day.AddHours(16));
        (await strategy.GetActualBreakStartTimeAsync(context)).ShouldBe(Day.AddHours(11).AddMinutes(45));
        (await strategy.GetActualBreakEndTimeAsync(context)).ShouldBe(Day.AddHours(13).AddMinutes(15));
    }

    [Fact]
    public async Task TimeSheet_Actual_Times_Should_Be_No_Punch_When_The_Underlying_Punch_Is_Missing()
    {
        var strategy = new TimeSheetPunchResolutionStrategy(FakeRepository.For<EmployeeTimesheet>(Context));
        var context = BuildContext(policy: BuildPolicy(status: TimeMgmtStatus.TimeSheet));

        (await strategy.GetActualStartTimeAsync(context)).ShouldBe(default(DateTime));
        (await strategy.GetActualEndTimeAsync(context)).ShouldBe(default(DateTime));
        (await strategy.GetActualBreakStartTimeAsync(context)).ShouldBe(default(DateTime));
        (await strategy.GetActualBreakEndTimeAsync(context)).ShouldBe(default(DateTime));
    }

    #endregion

    #region << Strategy identity / DI wiring >>

    /// <summary>
    /// Guards the resolver's <c>IEnumerable&lt;IPunchResolutionStrategy&gt;</c> injection: ABP's conventional
    /// registration must expose all four strategies under the SHARED <see cref="IPunchResolutionStrategy"/>
    /// service (not just under their own concrete types), otherwise
    /// <see cref="PunchResolutionStrategyResolver"/> silently receives an empty list at runtime and every day
    /// falls through to TimeSheet.
    /// </summary>
    [Fact]
    public void All_Four_Strategies_Should_Be_Resolvable_As_IPunchResolutionStrategy()
    {
        var strategies = LocalIocManager.ResolveAll<IPunchResolutionStrategy>();

        strategies.Select(s => s.TimeMgmtStatus).ShouldBe(
            new[]
            {
                TimeMgmtStatus.ActualTime,
                TimeMgmtStatus.PlannedTime,
                TimeMgmtStatus.FlexiTime,
                TimeMgmtStatus.TimeSheet
            },
            ignoreOrder: true);
    }

    [Fact]
    public void Resolver_Should_Be_Resolvable_From_The_Container()
    {
        var resolver = LocalIocManager.Resolve<IPunchResolutionStrategyResolver>();

        // Asserting on TimeMgmtStatus rather than the concrete type: the container hands back a Castle
        // [UnitOfWork] interception proxy, not the raw strategy instance.
        resolver.Resolve(TimeMgmtStatus.FlexiTime).TimeMgmtStatus.ShouldBe(TimeMgmtStatus.FlexiTime);
        resolver.Resolve(TimeMgmtStatus.NA).TimeMgmtStatus.ShouldBe(TimeMgmtStatus.TimeSheet);
        resolver.Resolve(TimeMgmtStatus.VariableTime).TimeMgmtStatus.ShouldBe(TimeMgmtStatus.TimeSheet);
    }

    [Fact]
    public void Each_Strategy_Should_Declare_Its_Own_TimeMgmtStatus()
    {
        new ActualTimePunchResolutionStrategy().TimeMgmtStatus.ShouldBe(TimeMgmtStatus.ActualTime);
        new PlannedTimePunchResolutionStrategy().TimeMgmtStatus.ShouldBe(TimeMgmtStatus.PlannedTime);
        new FlexiTimePunchResolutionStrategy().TimeMgmtStatus.ShouldBe(TimeMgmtStatus.FlexiTime);
        new TimeSheetPunchResolutionStrategy(FakeRepository.For<EmployeeTimesheet>(Context))
            .TimeMgmtStatus.ShouldBe(TimeMgmtStatus.TimeSheet);
    }

    #endregion
}
