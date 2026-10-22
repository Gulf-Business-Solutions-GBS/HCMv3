using HCMv3.TravelManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TravelManagement;

/// <summary>
/// Legacy <c>TravelDetail.PerDiemAmount</c> (<c>TravelDetail.cs</c> lines 135-143) and the paid share used by
/// payroll <c>GetTravelPerDiemPay</c> (<c>EmployeeCalculatedPayService.cs</c> line 4187).
/// </summary>
// ReSharper disable once InconsistentNaming
public class TravelPerDiemFormula_Tests
{
    [Fact]
    public void PerDiemAmount_Should_Be_Nights_Times_Rate()
    {
        TravelPerDiemFormula.PerDiemAmount(hasEmployee: true, totalNights: 4, perDiemRate: 25.5).ShouldBe(102);
    }

    [Fact]
    public void PerDiemAmount_Should_Round_To_3_Decimals()
    {
        TravelPerDiemFormula.PerDiemAmount(hasEmployee: true, totalNights: 1, perDiemRate: 1.23456).ShouldBe(1.235);
    }

    [Fact]
    public void PerDiemAmount_Should_Be_Zero_For_An_Applicant_Trip()
    {
        TravelPerDiemFormula.PerDiemAmount(hasEmployee: false, totalNights: 4, perDiemRate: 25.5).ShouldBe(0);
    }

    [Fact]
    public void PerDiemPayAmount_Should_Apply_The_Percentage_Without_Rounding_Again()
    {
        TravelPerDiemFormula.PerDiemPayAmount(1.235, 50).ShouldBe(0.6175, 0.0000001);
    }

    [Fact]
    public void PerDiemPayAmount_Should_Pay_In_Full_At_100_Percent()
    {
        TravelPerDiemFormula.PerDiemPayAmount(102, 100).ShouldBe(102);
    }
}
