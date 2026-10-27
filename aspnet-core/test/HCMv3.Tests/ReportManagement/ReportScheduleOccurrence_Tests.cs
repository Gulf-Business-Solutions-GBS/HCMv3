using System;
using HCMv3.ReportManagement;
using HCMv3.ReportManagement.Services;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.ReportManagement;

/// <summary>
/// Tests for "when does this schedule next fire".
///
/// Worth pinning carefully because both failure modes are invisible in production: a schedule that
/// stops firing looks exactly like one nobody triggered, and a schedule that drifts an hour later each
/// month looks like it is working until it lands in the wrong day.
/// </summary>
// ReSharper disable once InconsistentNaming
public class ReportScheduleOccurrence_Tests
{
    private static ReportSchedule Recurring(ReportRecurringPattern pattern, DateTime first, DateTime? endingOn = null)
    {
        return new ReportSchedule
        {
            IsActive = true,
            OccurrenceType = ReportScheduleOccurrenceType.Recurring,
            RecurringPattern = pattern,
            FirstOccurrence = first,
            EndingOn = endingOn,
        };
    }

    [Fact]
    public void A_Future_First_Occurrence_Is_The_Next_Run()
    {
        var schedule = Recurring(ReportRecurringPattern.Daily, new DateTime(2026, 6, 10, 7, 0, 0));

        var next = ReportScheduleOccurrenceCalculator.GetNextRunTime(schedule, new DateTime(2026, 6, 1));

        next.ShouldBe(new DateTime(2026, 6, 10, 7, 0, 0));
    }

    [Fact]
    public void Daily_Should_Keep_The_Anchor_Time_Of_Day()
    {
        // Drift is the thing to prevent: anchoring on the last run instead would let a job that ran
        // late once slide later every day until it crossed midnight.
        var schedule = Recurring(ReportRecurringPattern.Daily, new DateTime(2026, 6, 1, 7, 30, 0));

        var next = ReportScheduleOccurrenceCalculator.GetNextRunTime(schedule, new DateTime(2026, 6, 10, 9, 0, 0));

        next.ShouldBe(new DateTime(2026, 6, 11, 7, 30, 0));
    }

    [Fact]
    public void Weekly_Should_Advance_Seven_Days_From_The_Anchor()
    {
        var schedule = Recurring(ReportRecurringPattern.Weekly, new DateTime(2026, 6, 1, 8, 0, 0));

        var next = ReportScheduleOccurrenceCalculator.GetNextRunTime(schedule, new DateTime(2026, 6, 9));

        next.ShouldBe(new DateTime(2026, 6, 15, 8, 0, 0));
    }

    [Fact]
    public void Monthly_Anchored_On_The_31st_Should_Return_To_The_31st()
    {
        // The classic month-end bug. Stepping AddMonths(1) off the PREVIOUS value clamps 31 Jan to 28
        // Feb and then stays on the 28th forever; anchoring on the original date recovers the 31st.
        var schedule = Recurring(ReportRecurringPattern.Monthly, new DateTime(2026, 1, 31, 6, 0, 0));

        var afterFebruary = ReportScheduleOccurrenceCalculator.GetNextRunTime(
            schedule, new DateTime(2026, 2, 28, 23, 0, 0));

        afterFebruary.ShouldBe(new DateTime(2026, 3, 31, 6, 0, 0));
    }

    [Fact]
    public void Monthly_Should_Clamp_Into_A_Short_Month()
    {
        var schedule = Recurring(ReportRecurringPattern.Monthly, new DateTime(2026, 1, 31, 6, 0, 0));

        var next = ReportScheduleOccurrenceCalculator.GetNextRunTime(schedule, new DateTime(2026, 2, 1));

        // February has no 31st, so the occurrence lands on the last day it does have.
        next.ShouldBe(new DateTime(2026, 2, 28, 6, 0, 0));
    }

    [Fact]
    public void Quarterly_And_Yearly_Should_Step_From_The_Anchor()
    {
        var quarterly = Recurring(ReportRecurringPattern.Quarterly, new DateTime(2026, 1, 15));
        ReportScheduleOccurrenceCalculator
            .GetNextRunTime(quarterly, new DateTime(2026, 2, 1))
            .ShouldBe(new DateTime(2026, 4, 15));

        var yearly = Recurring(ReportRecurringPattern.Yearly, new DateTime(2026, 3, 1));
        ReportScheduleOccurrenceCalculator
            .GetNextRunTime(yearly, new DateTime(2026, 6, 1))
            .ShouldBe(new DateTime(2027, 3, 1));
    }

    [Fact]
    public void Should_Stop_After_The_End_Date()
    {
        var schedule = Recurring(
            ReportRecurringPattern.Daily,
            new DateTime(2026, 6, 1),
            endingOn: new DateTime(2026, 6, 3));

        ReportScheduleOccurrenceCalculator
            .GetNextRunTime(schedule, new DateTime(2026, 6, 2, 12, 0, 0))
            .ShouldBe(new DateTime(2026, 6, 3));

        // Past the end date there is no next run at all.
        ReportScheduleOccurrenceCalculator
            .GetNextRunTime(schedule, new DateTime(2026, 6, 3, 12, 0, 0))
            .ShouldBeNull();
    }

    [Fact]
    public void End_Date_Should_Include_The_Whole_Day()
    {
        // EndingOn is a date, so an occurrence at 23:00 on that date is still within it. Comparing
        // against midnight would silently drop the final run.
        var schedule = Recurring(
            ReportRecurringPattern.Daily,
            new DateTime(2026, 6, 1, 23, 0, 0),
            endingOn: new DateTime(2026, 6, 3));

        ReportScheduleOccurrenceCalculator
            .GetNextRunTime(schedule, new DateTime(2026, 6, 2, 23, 30, 0))
            .ShouldBe(new DateTime(2026, 6, 3, 23, 0, 0));
    }

    [Fact]
    public void A_One_Off_Runs_Once_And_Never_Again()
    {
        var schedule = new ReportSchedule
        {
            IsActive = true,
            OccurrenceType = ReportScheduleOccurrenceType.Once,
            FirstOccurrence = new DateTime(2026, 6, 1),
        };

        ReportScheduleOccurrenceCalculator
            .GetNextRunTime(schedule, new DateTime(2026, 5, 1))
            .ShouldBe(new DateTime(2026, 6, 1));

        schedule.LastRunTime = new DateTime(2026, 6, 1);

        ReportScheduleOccurrenceCalculator
            .GetNextRunTime(schedule, new DateTime(2026, 6, 2))
            .ShouldBeNull();
    }

    [Fact]
    public void An_Inactive_Schedule_Never_Fires()
    {
        var schedule = Recurring(ReportRecurringPattern.Daily, new DateTime(2026, 6, 1));
        schedule.IsActive = false;

        ReportScheduleOccurrenceCalculator
            .GetNextRunTime(schedule, new DateTime(2026, 6, 10))
            .ShouldBeNull();

        ReportScheduleOccurrenceCalculator.IsDue(schedule, new DateTime(2026, 6, 10)).ShouldBeFalse();
    }

    [Fact]
    public void IsDue_Should_Be_True_Only_Once_The_Next_Run_Has_Arrived()
    {
        var schedule = Recurring(ReportRecurringPattern.Daily, new DateTime(2026, 6, 1, 7, 0, 0));
        schedule.NextRunTime = new DateTime(2026, 6, 10, 7, 0, 0);

        ReportScheduleOccurrenceCalculator.IsDue(schedule, new DateTime(2026, 6, 10, 6, 59, 0)).ShouldBeFalse();
        ReportScheduleOccurrenceCalculator.IsDue(schedule, new DateTime(2026, 6, 10, 7, 0, 0)).ShouldBeTrue();
    }

    [Fact]
    public void Should_Not_Spin_On_A_Schedule_Anchored_Far_In_The_Past()
    {
        // A daily schedule anchored decades ago must not hang the dispatcher walking every day since.
        var schedule = Recurring(ReportRecurringPattern.Daily, new DateTime(1990, 1, 1));

        var next = ReportScheduleOccurrenceCalculator.GetNextRunTime(schedule, new DateTime(2026, 6, 1));

        // Either it resolves or it gives up, but it returns promptly either way.
        (next == null || next > new DateTime(2026, 6, 1)).ShouldBeTrue();
    }
}
