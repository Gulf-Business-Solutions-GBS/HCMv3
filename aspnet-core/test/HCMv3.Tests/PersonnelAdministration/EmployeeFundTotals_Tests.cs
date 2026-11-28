using System.Collections.Generic;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

// ReSharper disable once InconsistentNaming
public class EmployeeFundTotals_Tests
{
    private static EmployeeFundDetail Row(FundPayType type, double amount) =>
        new() { FundPayType = type, Amount = amount };

    [Fact]
    public void Should_Sum_Opening_Deduction_And_ExternalDeduction_Into_Amount()
    {
        // Arrange
        var fund = new EmployeeFund();
        var rows = new List<EmployeeFundDetail>
        {
            Row(FundPayType.Opening, 100),
            Row(FundPayType.Deduction, 25.5),
            Row(FundPayType.ExternalDeduction, 10),
            Row(FundPayType.Payment, 40)
        };

        // Act
        EmployeeFundTotals.Apply(fund, rows);

        // Assert
        fund.Amount.ShouldBe(135.5);
    }

    [Fact]
    public void Should_Sum_Payment_Rows_Into_PaidAmount()
    {
        // Arrange
        var fund = new EmployeeFund();
        var rows = new List<EmployeeFundDetail>
        {
            Row(FundPayType.Deduction, 100),
            Row(FundPayType.Payment, 30),
            Row(FundPayType.Payment, 12.25)
        };

        // Act
        EmployeeFundTotals.Apply(fund, rows);

        // Assert
        fund.PaidAmount.ShouldBe(42.25);
    }

    [Fact]
    public void Should_Set_Balance_As_Amount_Minus_PaidAmount_Rounded_To_Two_Decimals()
    {
        // Arrange
        var fund = new EmployeeFund();
        var rows = new List<EmployeeFundDetail>
        {
            Row(FundPayType.Deduction, 10.001),
            Row(FundPayType.Deduction, 20.112),
            Row(FundPayType.Payment, 5.333)
        };

        // Act
        EmployeeFundTotals.Apply(fund, rows);

        // Assert
        fund.Amount.ShouldBe(30.11);
        fund.PaidAmount.ShouldBe(5.33);
        fund.BalanceAmount.ShouldBe(24.78);
    }

    [Fact]
    public void Should_Zero_All_Totals_When_There_Are_No_Rows()
    {
        // Arrange
        var fund = new EmployeeFund { Amount = 50, PaidAmount = 20, BalanceAmount = 30 };

        // Act
        EmployeeFundTotals.Apply(fund, new List<EmployeeFundDetail>());

        // Assert
        fund.Amount.ShouldBe(0);
        fund.PaidAmount.ShouldBe(0);
        fund.BalanceAmount.ShouldBe(0);
    }
}
