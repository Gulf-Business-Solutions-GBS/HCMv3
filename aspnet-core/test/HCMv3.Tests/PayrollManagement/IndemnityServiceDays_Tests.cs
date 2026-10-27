using System;
using HCMv3.PayrollManagement.Services;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// <see cref="IndemnityServiceDays"/>: service days for indemnity leave out every 29 February, so a leap year
/// never earns more than a normal one (deliberate deviation from legacy, user instruction 2026-09-21).
/// </summary>
// ReSharper disable once InconsistentNaming
public class IndemnityServiceDays_Tests
{
    [Fact]
    public void Should_Count_Two_Full_Years_Across_A_Leap_Day_As_730_Days()
    {
        IndemnityServiceDays.Count(new DateTime(2022, 7, 1), new DateTime(2024, 6, 30)).ShouldBe(730);
    }

    [Fact]
    public void Should_Count_Two_Full_Years_Without_A_Leap_Day_As_730_Days()
    {
        IndemnityServiceDays.Count(new DateTime(2021, 7, 1), new DateTime(2023, 6, 30)).ShouldBe(730);
    }

    [Fact]
    public void Should_Leave_Out_Every_Leap_Day_In_A_Long_Service()
    {
        // 2016, 2020 and 2024 fall inside.
        IndemnityServiceDays.Count(new DateTime(2012, 7, 1), new DateTime(2024, 6, 30)).ShouldBe(12 * 365);
    }

    [Fact]
    public void Should_Not_Leave_Out_A_Leap_Day_Outside_The_Period()
    {
        // 29/02/2024 is after the end; 29/02/2020 is before the start.
        IndemnityServiceDays.Count(new DateTime(2020, 3, 1), new DateTime(2024, 2, 28)).ShouldBe(1460);
    }

    [Theory]
    [InlineData("2024-02-29", "2024-02-29", 1)]
    [InlineData("2024-02-28", "2024-03-01", 1)]
    [InlineData("2023-01-01", "2023-12-31", 0)]
    [InlineData("2000-01-01", "2000-12-31", 1)] // 2000 is a leap year
    [InlineData("2100-01-01", "2100-12-31", 0)] // 2100 is not
    public void Should_Find_The_Leap_Days_Inclusive_Of_Both_Ends(string from, string to, int expected)
    {
        IndemnityServiceDays.LeapDaysBetween(DateTime.Parse(from), DateTime.Parse(to)).ShouldBe(expected);
    }

    [Fact]
    public void Should_Return_Non_Positive_Spans_Unchanged()
    {
        IndemnityServiceDays.Count(new DateTime(2024, 3, 5), new DateTime(2024, 3, 1)).ShouldBe(-3);
    }
}
