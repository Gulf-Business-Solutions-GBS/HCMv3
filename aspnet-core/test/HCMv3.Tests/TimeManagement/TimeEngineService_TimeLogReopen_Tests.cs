using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Services;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TimeManagement;

/// <summary>
/// The reopen cycle -- the half of the time log approval port that lives in the time engine, and the piece
/// most easily forgotten.
///
/// <para>
/// When recalculation moves an already-approved day, the approval it was granted no longer describes it. The
/// engine has to record BOTH triples in <see cref="TimeLogHistory"/>, set the day back to
/// <see cref="TimeRegisterStatus.Reopened"/>, CLEAR
/// <see cref="TimeRegister.TimeLogApprovalId"/> and reset the <c>Approved*</c> figures to the freshly
/// calculated raw ones. Clearing the link is the load-bearing step: while it is set, the day is still
/// "settled" and <c>TimeLogApprovalWorkflowTransactionHandler</c> refuses to put it in a new batch -- so
/// without this an approved day could never be corrected again.
/// </para>
///
/// <para>
/// Ported from legacy <c>HCM10.TimeEngine/Program.cs</c> lines 1475-1511. Exercised through the private
/// <c>ApplyApprovalFieldsAsync</c> for the reason set out in <see cref="TimeEngineServiceTestHarness"/>: the
/// public route is a whole <c>CreateTimeRegisterAsync</c> run over dozens of seeded entities.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class TimeEngineService_TimeLogReopen_Tests
{
    private const string Method = "ApplyApprovalFieldsAsync";

    private static TimeRegisterDayContext ContextFor(
        TimeRegister timeRegister,
        TimeRegisterPreviousValues previous,
        bool isNew = false,
        bool autoApprove = false)
    {
        return new TimeRegisterDayContext
        {
            TimeRegister = timeRegister,
            DayDate = timeRegister.DayDate,
            IsNewTimeRegisterRecord = isNew,
            PreviousValues = previous,
            Settings = new TimeEngineSettings
            {
                EnableTimeRegisterApproval = true,
                AutoApproveTimeLog = autoApprove
            }
        };
    }

    /// <summary>An approved day whose figures have just moved, with a real approval batch behind it.</summary>
    private static TimeRegister ApprovedDayThatJustChanged(int timeLogApprovalId = 77)
    {
        return new TimeRegister
        {
            Id = 500,
            TenantId = 1,
            EmployeeId = 9,
            DayDate = new DateTime(2026, 5, 4),
            Status = TimeRegisterStatus.Approved,
            TimeLogApprovalId = timeLogApprovalId,

            // Freshly recalculated.
            AbsentDay = 1,
            ActualWorkingDay = 0,
            DedLateInEarlyOutMins = 60,

            // What was approved, and is about to be discarded.
            ApprovedAbsentDay = 0,
            ApprovedActualWorkingDay = 1,
            ApprovedDedLateInEarlyOutMins = 15
        };
    }

    private static TimeRegisterPreviousValues PreviousFor(TimeRegister _)
    {
        // What the day read when it was approved.
        return new TimeRegisterPreviousValues
        {
            AbsentDay = 0,
            ActualWorkingDay = 1,
            DedLateInEarlyOutMins = 15
        };
    }

    [Fact]
    public async Task Recalculating_An_Approved_Day_Should_Write_A_TimeLogHistory_Row()
    {
        var harness = new TimeEngineServiceTestHarness();
        var timeRegister = ApprovedDayThatJustChanged();

        await harness.InvokeAsync(Method, ContextFor(timeRegister, PreviousFor(timeRegister)));

        var history = harness.Store<TimeLogHistory>().ShouldHaveSingleItem();

        history.TimeLogApprovalId.ShouldBe(77);
        history.TimeRegisterId.ShouldBe(500);
        history.TenantId.ShouldBe(1);

        // The raw triple as it stood when the approval was granted.
        history.AbsentDay.ShouldBe(0);
        history.ActualWorkingDay.ShouldBe(1);
        history.DedLateInEarlyOutMins.ShouldBe(15);

        // The approved triple being thrown away.
        history.ApprovedAbsentDay.ShouldBe(0);
        history.ApprovedActualWorkingDay.ShouldBe(1);
        history.ApprovedDedLateInEarlyOutMins.ShouldBe(15);
    }

    [Fact]
    public async Task Recalculating_An_Approved_Day_Should_Reopen_It_And_Clear_Its_Approval_Link()
    {
        // The clear is the whole point: an approved day that keeps its link can never enter a new batch,
        // because the handler refuses days that are still settled.
        var harness = new TimeEngineServiceTestHarness();
        var timeRegister = ApprovedDayThatJustChanged();

        await harness.InvokeAsync(Method, ContextFor(timeRegister, PreviousFor(timeRegister)));

        timeRegister.Status.ShouldBe(TimeRegisterStatus.Reopened);
        timeRegister.TimeLogApprovalId.ShouldBeNull();
    }

    [Fact]
    public async Task Recalculating_An_Approved_Day_Should_Reset_The_Approved_Values_To_The_Raw_Ones()
    {
        var harness = new TimeEngineServiceTestHarness();
        var timeRegister = ApprovedDayThatJustChanged();

        await harness.InvokeAsync(Method, ContextFor(timeRegister, PreviousFor(timeRegister)));

        timeRegister.ApprovedAbsentDay.ShouldBe(timeRegister.AbsentDay);
        timeRegister.ApprovedActualWorkingDay.ShouldBe(timeRegister.ActualWorkingDay);
        timeRegister.ApprovedDedLateInEarlyOutMins.ShouldBe(timeRegister.DedLateInEarlyOutMins);
    }

    [Fact]
    public async Task An_Auto_Approved_Day_Should_Go_Back_To_Opened_Without_History()
    {
        // Legacy Program.cs lines 1503-1507: a day that reached Approved through the AutoApproveTimeLog
        // shortcut has no human decision behind it, so there is nothing to record and nothing to reopen for.
        var harness = new TimeEngineServiceTestHarness();
        var timeRegister = ApprovedDayThatJustChanged(timeLogApprovalId: 0);
        timeRegister.TimeLogApprovalId = null;

        await harness.InvokeAsync(Method, ContextFor(timeRegister, PreviousFor(timeRegister)));

        timeRegister.Status.ShouldBe(TimeRegisterStatus.Opened);
        harness.Store<TimeLogHistory>().ShouldBeEmpty();
    }

    [Fact]
    public async Task An_Approved_Day_That_Did_Not_Change_Should_Be_Left_Completely_Alone()
    {
        // No history, no reopen, no released link -- recalculation that agrees with the approval must not
        // disturb it, or every engine run would invalidate every approval in the system.
        var harness = new TimeEngineServiceTestHarness();
        var timeRegister = ApprovedDayThatJustChanged();

        var unchanged = new TimeRegisterPreviousValues
        {
            AbsentDay = timeRegister.AbsentDay,
            ActualWorkingDay = timeRegister.ActualWorkingDay,
            DedLateInEarlyOutMins = timeRegister.DedLateInEarlyOutMins
        };

        await harness.InvokeAsync(Method, ContextFor(timeRegister, unchanged));

        timeRegister.Status.ShouldBe(TimeRegisterStatus.Approved);
        timeRegister.TimeLogApprovalId.ShouldBe(77);
        timeRegister.ApprovedDedLateInEarlyOutMins.ShouldBe(15);
        harness.Store<TimeLogHistory>().ShouldBeEmpty();
    }

    [Fact]
    public async Task A_New_Day_Should_Mirror_Its_Raw_Values_And_Write_No_History()
    {
        var harness = new TimeEngineServiceTestHarness();

        var timeRegister = new TimeRegister
        {
            TenantId = 1,
            EmployeeId = 9,
            DayDate = new DateTime(2026, 5, 5),
            AbsentDay = 1,
            ActualWorkingDay = 0,
            DedLateInEarlyOutMins = 30
        };

        await harness.InvokeAsync(
            Method,
            ContextFor(timeRegister, new TimeRegisterPreviousValues(), isNew: true));

        timeRegister.ApprovedAbsentDay.ShouldBe(1);
        timeRegister.ApprovedActualWorkingDay.ShouldBe(0);
        timeRegister.ApprovedDedLateInEarlyOutMins.ShouldBe(30);
        timeRegister.Status.ShouldBe(TimeRegisterStatus.Opened);
        harness.Store<TimeLogHistory>().ShouldBeEmpty();
    }

    [Fact]
    public async Task A_New_Day_With_Nothing_To_Approve_Should_Be_Auto_Approved_When_Configured()
    {
        var harness = new TimeEngineServiceTestHarness();

        var timeRegister = new TimeRegister
        {
            TenantId = 1,
            EmployeeId = 9,
            DayDate = new DateTime(2026, 5, 6),
            AbsentDay = 0,
            ActualWorkingDay = 1,
            DedLateInEarlyOutMins = 0
        };

        await harness.InvokeAsync(
            Method,
            ContextFor(timeRegister, new TimeRegisterPreviousValues(), isNew: true, autoApprove: true));

        timeRegister.Status.ShouldBe(TimeRegisterStatus.Approved);

        // Auto-approval is not an approval BATCH -- nothing to link back to.
        timeRegister.TimeLogApprovalId.ShouldBeNull();
        harness.Store<TimeLogHistory>().ShouldBeEmpty();
    }

    [Fact]
    public async Task A_Reopened_Day_Should_Become_Eligible_For_A_Fresh_Batch()
    {
        // End to end on the cycle: approved -> recalculated -> reopened -> unlinked. The last assertion is
        // the one that matters, because "settled and linked" is exactly what the handler refuses.
        var harness = new TimeEngineServiceTestHarness();
        var timeRegister = ApprovedDayThatJustChanged();

        await harness.InvokeAsync(Method, ContextFor(timeRegister, PreviousFor(timeRegister)));

        timeRegister.Status.ShouldNotBe(TimeRegisterStatus.Approved);
        timeRegister.TimeLogApprovalId.HasValue.ShouldBeFalse();
        harness.Store<TimeLogHistory>().Count(h => h.TimeRegisterId == 500).ShouldBe(1);
    }
}
