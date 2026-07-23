using System;
using HCMv3.PayrollManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class PayrollPeriodShiftCalculator_Tests
{
    [Fact]
    public void Should_Compute_Range_For_Jan_Dec()
    {
        var (start, end) = PayrollPeriodShiftCalculator.GetCalendarMonthRange("Jan-Dec", 1, 2015);

        start.ShouldBe(new DateTime(2015, 1, 1));
        end.ShouldBe(new DateTime(2015, 1, 31));
    }

    [Fact]
    public void Should_Compute_Range_For_Feb_Jan_With_Year_Wrap()
    {
        // 01/2015 is 01.02.2015 - 29.01.2016 (per legacy comment "31.01.2016")
        var (start, end) = PayrollPeriodShiftCalculator.GetCalendarMonthRange("Feb-Jan", 1, 2015);

        start.ShouldBe(new DateTime(2015, 2, 1));
        end.ShouldBe(new DateTime(2015, 2, 28));

        var (startDec, endDec) = PayrollPeriodShiftCalculator.GetCalendarMonthRange("Feb-Jan", 12, 2015);
        startDec.ShouldBe(new DateTime(2016, 1, 1));
        endDec.ShouldBe(new DateTime(2016, 1, 31));
    }

    [Fact]
    public void Should_Compute_Range_For_Dec_Nov_With_Year_Wrap()
    {
        // 01/2015 is 01.12.2015
        var (start, end) = PayrollPeriodShiftCalculator.GetCalendarMonthRange("Dec-Nov", 1, 2015);

        start.ShouldBe(new DateTime(2015, 12, 1));
        end.ShouldBe(new DateTime(2015, 12, 31));

        var (startFeb, endFeb) = PayrollPeriodShiftCalculator.GetCalendarMonthRange("Dec-Nov", 2, 2015);
        startFeb.ShouldBe(new DateTime(2016, 1, 1));
        endFeb.ShouldBe(new DateTime(2016, 1, 31));
    }

    [Fact]
    public void Should_Compute_Range_For_Jul_Jun_Mid_Year()
    {
        // Jul-Jun: PayrollMonth 6 (< wrap threshold 7) maps to December of the same PayrollYear, no wrap.
        var (start, end) = PayrollPeriodShiftCalculator.GetCalendarMonthRange("Jul-Jun", 6, 2020);
        start.ShouldBe(new DateTime(2020, 12, 1));
        end.ShouldBe(new DateTime(2020, 12, 31));

        // PayrollMonth 7 (>= wrap threshold 7) maps to January of the following PayrollYear.
        var (startWrap, endWrap) = PayrollPeriodShiftCalculator.GetCalendarMonthRange("Jul-Jun", 7, 2020);
        startWrap.ShouldBe(new DateTime(2021, 1, 1));
        endWrap.ShouldBe(new DateTime(2021, 1, 31));
    }

    [Fact]
    public void Should_Throw_For_Unrecognized_Code()
    {
        Should.Throw<ArgumentException>(() =>
            PayrollPeriodShiftCalculator.GetCalendarMonthRange("Unknown-Code", 1, 2020));
    }
}
