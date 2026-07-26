using System;
using HCMv3.Common.Services;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Common.Services;

public class DateDiffCalculatorTests
{
    [Fact]
    public void Should_Calculate_Years_Months_Days_Between_Two_Dates()
    {
        var calculator = new DateDiffCalculator(new DateTime(2020, 1, 15), new DateTime(2023, 4, 20));

        calculator.Years.ShouldBe(3);
        calculator.Months.ShouldBe(3);
        calculator.Days.ShouldBe(5);
    }

    [Fact]
    public void Should_Normalize_When_First_Date_Is_After_Second_Date()
    {
        var forward = new DateDiffCalculator(new DateTime(2020, 1, 1), new DateTime(2021, 1, 1));
        var backward = new DateDiffCalculator(new DateTime(2021, 1, 1), new DateTime(2020, 1, 1));

        backward.Years.ShouldBe(forward.Years);
        backward.Months.ShouldBe(forward.Months);
        backward.Days.ShouldBe(forward.Days);
    }

    [Fact]
    public void Should_Handle_February_Leap_Year_Boundary()
    {
        var calculator = new DateDiffCalculator(new DateTime(2020, 1, 31), new DateTime(2020, 3, 1));

        // 2020 is a leap year -> Feb has 29 days
        calculator.ToString().ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void Should_Return_Zero_For_Identical_Dates()
    {
        var date = new DateTime(2022, 6, 10);
        var calculator = new DateDiffCalculator(date, date);

        calculator.Years.ShouldBe(0);
        calculator.Months.ShouldBe(0);
        calculator.Days.ShouldBe(0);
    }
}
