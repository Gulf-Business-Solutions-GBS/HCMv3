using System;
using System.Collections.Generic;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Services;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TimeManagement;

/// <summary>
/// The geo punch rules, ported from legacy <c>GeoLocationController</c>: which button is open when, how far
/// the employee is from the work location, and whether that distance is allowed.
///
/// <para>
/// Each button case is a row in legacy's state table, asserted as the four buttons the employee would see.
/// The split-shift cases lean on legacy's tolerances (an hour before duty start, two hours after the break
/// begins, an hour before it ends), so the times in them are chosen either side of those edges.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class GeoPunchRules_Tests
{
    private static GeoPunchEvent Punch(string code, int hour, int minute = 0) =>
        new(code, new TimeSpan(hour, minute, 0));

    private static TimeSpan At(int hour, int minute = 0) => new(hour, minute, 0);

    private static void ShouldBe(GeoPunchButtons buttons, bool clockIn, bool clockOut, bool otIn, bool otOut)
    {
        buttons.CanClockIn.ShouldBe(clockIn, "Clock In");
        buttons.CanClockOut.ShouldBe(clockOut, "Clock Out");
        buttons.CanOvertimeIn.ShouldBe(otIn, "Overtime In");
        buttons.CanOvertimeOut.ShouldBe(otOut, "Overtime Out");
    }

    private static readonly GeoPunchDutyShape SplitShift = new()
    {
        DutyStart = At(8),
        DutyEnd = At(21),
        BreakStart = At(12),
        BreakEnd = At(16)
    };

    // ---------------------------------------------------------------- ordinary shift

    [Fact]
    public void Should_Offer_Only_Clock_In_When_Nothing_Is_Punched_Yet()
    {
        var buttons = GeoPunchRules.ResolveButtons(new List<GeoPunchEvent>(), null, At(7));

        ShouldBe(buttons, clockIn: true, clockOut: false, otIn: false, otOut: false);
    }

    [Fact]
    public void Should_Offer_Only_Clock_Out_After_Clocking_In()
    {
        var buttons = GeoPunchRules.ResolveButtons(
            new List<GeoPunchEvent> { Punch(TransTypeCodes.ClockIn, 8) }, null, At(10));

        ShouldBe(buttons, clockIn: false, clockOut: true, otIn: false, otOut: false);
    }

    [Fact]
    public void Should_Offer_Only_Overtime_In_After_Both_In_And_Out()
    {
        var buttons = GeoPunchRules.ResolveButtons(
            new List<GeoPunchEvent> { Punch(TransTypeCodes.ClockIn, 8), Punch(TransTypeCodes.ClockOut, 17) },
            null,
            At(18));

        ShouldBe(buttons, clockIn: false, clockOut: false, otIn: true, otOut: false);
    }

    [Fact]
    public void Should_Offer_Only_Overtime_Out_After_Overtime_In()
    {
        var buttons = GeoPunchRules.ResolveButtons(
            new List<GeoPunchEvent>
            {
                Punch(TransTypeCodes.ClockIn, 8),
                Punch(TransTypeCodes.ClockOut, 17),
                Punch(TransTypeCodes.OvertimeIn, 18)
            },
            null,
            At(19));

        ShouldBe(buttons, clockIn: false, clockOut: false, otIn: false, otOut: true);
    }

    [Fact]
    public void Should_Reopen_Overtime_In_After_Overtime_Out()
    {
        var buttons = GeoPunchRules.ResolveButtons(
            new List<GeoPunchEvent>
            {
                Punch(TransTypeCodes.ClockIn, 8),
                Punch(TransTypeCodes.ClockOut, 17),
                Punch(TransTypeCodes.OvertimeIn, 18),
                Punch(TransTypeCodes.OvertimeOut, 20)
            },
            null,
            At(21));

        ShouldBe(buttons, clockIn: false, clockOut: false, otIn: true, otOut: false);
    }

    [Fact]
    public void Should_Judge_The_Last_Overtime_Punch_By_Time_Not_By_List_Order()
    {
        // Legacy ordered by TransDate, identical for every punch of the day. Listed out of order here: the
        // OT Out at 20:00 is the last one, so OT In must be open again.
        var buttons = GeoPunchRules.ResolveButtons(
            new List<GeoPunchEvent>
            {
                Punch(TransTypeCodes.OvertimeOut, 20),
                Punch(TransTypeCodes.ClockIn, 8),
                Punch(TransTypeCodes.OvertimeIn, 18),
                Punch(TransTypeCodes.ClockOut, 17)
            },
            null,
            At(21));

        buttons.CanOvertimeIn.ShouldBeTrue();
        buttons.CanOvertimeOut.ShouldBeFalse();
    }

    [Fact]
    public void Should_Close_Overtime_While_A_Shift_Is_Open()
    {
        var buttons = GeoPunchRules.ResolveButtons(
            new List<GeoPunchEvent> { Punch(TransTypeCodes.ClockIn, 8), Punch(TransTypeCodes.OvertimeIn, 9) },
            null,
            At(10));

        buttons.CanOvertimeIn.ShouldBeFalse();
        buttons.CanOvertimeOut.ShouldBeFalse();
    }

    [Fact]
    public void Should_Treat_A_Duty_Without_A_Break_As_An_Ordinary_Shift()
    {
        var duty = new GeoPunchDutyShape { DutyStart = At(8), DutyEnd = At(17) };

        var buttons = GeoPunchRules.ResolveButtons(new List<GeoPunchEvent>(), duty, At(7));

        ShouldBe(buttons, clockIn: true, clockOut: false, otIn: false, otOut: false);
    }

    // ---------------------------------------------------------------- split shift

    [Fact]
    public void Split_Shift_Should_Offer_Clock_In_Before_The_First_Half()
    {
        var buttons = GeoPunchRules.ResolveButtons(new List<GeoPunchEvent>(), SplitShift, At(7, 30));

        ShouldBe(buttons, clockIn: true, clockOut: false, otIn: false, otOut: false);
    }

    [Fact]
    public void Split_Shift_Should_Offer_Clock_Out_During_The_First_Half()
    {
        var buttons = GeoPunchRules.ResolveButtons(
            new List<GeoPunchEvent> { Punch(TransTypeCodes.ClockIn, 8) }, SplitShift, At(11));

        ShouldBe(buttons, clockIn: false, clockOut: true, otIn: false, otOut: false);
    }

    [Fact]
    public void Split_Shift_Should_Close_Everything_During_The_Break()
    {
        // Break starts 12:00 (+2h tolerance = 14:00) and ends 16:00 (-1h tolerance = 15:00).
        var buttons = GeoPunchRules.ResolveButtons(
            new List<GeoPunchEvent> { Punch(TransTypeCodes.ClockIn, 8), Punch(TransTypeCodes.ClockOut, 12) },
            SplitShift,
            At(14, 30));

        ShouldBe(buttons, clockIn: false, clockOut: false, otIn: false, otOut: false);
    }

    [Fact]
    public void Split_Shift_Should_Offer_Clock_In_For_The_Second_Half()
    {
        var buttons = GeoPunchRules.ResolveButtons(
            new List<GeoPunchEvent> { Punch(TransTypeCodes.ClockIn, 8), Punch(TransTypeCodes.ClockOut, 12) },
            SplitShift,
            At(15, 30));

        ShouldBe(buttons, clockIn: true, clockOut: false, otIn: false, otOut: false);
    }

    [Fact]
    public void Split_Shift_Should_Offer_Clock_Out_After_The_Second_Clock_In()
    {
        var buttons = GeoPunchRules.ResolveButtons(
            new List<GeoPunchEvent>
            {
                Punch(TransTypeCodes.ClockIn, 8),
                Punch(TransTypeCodes.ClockOut, 12),
                Punch(TransTypeCodes.ClockIn, 16)
            },
            SplitShift,
            At(18));

        ShouldBe(buttons, clockIn: false, clockOut: true, otIn: false, otOut: false);
    }

    [Fact]
    public void Split_Shift_Should_Offer_Overtime_In_Once_Both_Halves_Are_Done_And_The_Duty_Has_Ended()
    {
        var buttons = GeoPunchRules.ResolveButtons(
            new List<GeoPunchEvent>
            {
                Punch(TransTypeCodes.ClockIn, 8),
                Punch(TransTypeCodes.ClockOut, 12),
                Punch(TransTypeCodes.ClockIn, 16),
                Punch(TransTypeCodes.ClockOut, 21)
            },
            SplitShift,
            At(21, 30));

        ShouldBe(buttons, clockIn: false, clockOut: false, otIn: true, otOut: false);
    }

    [Fact]
    public void Split_Shift_Should_Keep_Overtime_Closed_Before_The_Duty_Ends()
    {
        var buttons = GeoPunchRules.ResolveButtons(
            new List<GeoPunchEvent>
            {
                Punch(TransTypeCodes.ClockIn, 8),
                Punch(TransTypeCodes.ClockOut, 12),
                Punch(TransTypeCodes.ClockIn, 16),
                Punch(TransTypeCodes.ClockOut, 20)
            },
            SplitShift,
            At(20, 30));

        buttons.CanOvertimeIn.ShouldBeFalse();
        buttons.CanOvertimeOut.ShouldBeFalse();
    }

    [Fact]
    public void Split_Shift_Should_Close_Clock_In_When_The_Second_Half_Was_Missed_And_The_Duty_Is_Over()
    {
        var buttons = GeoPunchRules.ResolveButtons(
            new List<GeoPunchEvent> { Punch(TransTypeCodes.ClockIn, 8), Punch(TransTypeCodes.ClockOut, 12) },
            SplitShift,
            At(22));

        buttons.CanClockIn.ShouldBeFalse();
        buttons.CanClockOut.ShouldBeFalse();
    }

    // ---------------------------------------------------------------- button lookup

    [Theory]
    [InlineData(TransTypeCodes.ClockIn, true)]
    [InlineData(TransTypeCodes.ClockOut, false)]
    [InlineData(TransTypeCodes.OvertimeIn, true)]
    [InlineData(TransTypeCodes.OvertimeOut, false)]
    [InlineData("9", false)]
    [InlineData(null, false)]
    public void Allows_Should_Map_Each_Code_To_Its_Button(string code, bool expected)
    {
        var buttons = new GeoPunchButtons { CanClockIn = true, CanOvertimeIn = true };

        buttons.Allows(code).ShouldBe(expected);
    }

    // ---------------------------------------------------------------- distance and range

    [Fact]
    public void Distance_Should_Be_Zero_For_The_Same_Point()
    {
        GeoPunchRules.DistanceMeters(29.3759, 47.9774, 29.3759, 47.9774).ShouldBe(0, 0.001);
    }

    [Fact]
    public void Distance_Should_Match_The_Haversine_Formula()
    {
        // One thousandth of a degree of latitude is ~111.19 m on a 6371 km sphere.
        GeoPunchRules.DistanceMeters(29.3759, 47.9774, 29.3769, 47.9774).ShouldBe(111.19, 0.05);
    }

    [Fact]
    public void Distance_Should_Be_Symmetric()
    {
        var there = GeoPunchRules.DistanceMeters(29.3759, 47.9774, 29.3117, 47.4818);
        var back = GeoPunchRules.DistanceMeters(29.3117, 47.4818, 29.3759, 47.9774);

        there.ShouldBe(back, 0.001);
    }

    [Theory]
    [InlineData(50, 100, false, GeoPunchRangeVerdict.InRange)]
    [InlineData(100, 100, false, GeoPunchRangeVerdict.InRange)]
    [InlineData(100.5, 100, false, GeoPunchRangeVerdict.OutOfRangeRefused)]
    [InlineData(100.5, 100, true, GeoPunchRangeVerdict.OutOfRangeAllowed)]
    [InlineData(1, 0, false, GeoPunchRangeVerdict.OutOfRangeRefused)]
    public void JudgeRange_Should_Allow_Up_To_And_Including_The_Radius(
        double distance, double range, bool allowOutside, GeoPunchRangeVerdict expected)
    {
        GeoPunchRules.JudgeRange(distance, range, allowOutside).ShouldBe(expected);
    }
}
