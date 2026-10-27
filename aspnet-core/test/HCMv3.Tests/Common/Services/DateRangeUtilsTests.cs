using System;
using HCMv3.Common;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Common.Services;

public class DateRangeUtilsTests
{
    [Fact]
    public void Should_Return_Month_End_Dates_Up_To_But_Not_Past_EndDate()
    {
        // Ported verbatim from legacy SysService.GetMonthYearList: the loop only adds a month-end date
        // when that month-end is <= endDate, so the month containing endDate itself is only included if
        // endDate is on-or-after that month's last day.
        var result = DateRangeUtils.GetMonthYearList(new DateTime(2023, 1, 10), new DateTime(2023, 3, 15));

        result.ShouldContain(new DateTime(2023, 1, 31));
        result.ShouldContain(new DateTime(2023, 2, 28));
        result.ShouldNotContain(new DateTime(2023, 3, 31));
    }

    [Fact]
    public void Should_Return_Empty_When_EndDate_Is_Before_Start_Months_End()
    {
        var result = DateRangeUtils.GetMonthYearList(new DateTime(2023, 5, 1), new DateTime(2023, 5, 20));

        result.ShouldBeEmpty();
    }

    [Fact]
    public void Should_Include_Month_End_When_EndDate_On_Or_After_It()
    {
        var result = DateRangeUtils.GetMonthYearList(new DateTime(2023, 5, 1), new DateTime(2023, 5, 31));

        result.ShouldBe(new[] { new DateTime(2023, 5, 31) });
    }
}
