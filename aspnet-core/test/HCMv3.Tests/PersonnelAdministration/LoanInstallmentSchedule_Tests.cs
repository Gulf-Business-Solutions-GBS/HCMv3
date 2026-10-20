using System;
using System.Collections.Generic;
using System.Linq;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

/// <summary>
/// The instalment schedule's arithmetic, against legacy <c>EmployeeLoan.GetInstallmentAmount</c>,
/// <c>CalculateRecoveryDate</c>, <c>EmployeeLoanInstallment.SetEndDate</c> and the rejoin carry-forward dates. Every
/// expected figure is worked out in the test's own comment rather than recomputed.
/// </summary>
// ReSharper disable once InconsistentNaming
public class LoanInstallmentSchedule_Tests
{
    private static readonly DateTime Open = new DateTime(9999, 12, 31);

    private static EmployeeLoanInstallment Row(int id, DateTime start, DateTime end, double amount) =>
        new EmployeeLoanInstallment { Id = id, StartDate = start, EndDate = end, Amount = amount };

    private static List<EmployeeLoanInstallment> TwoRows() => new()
    {
        Row(1, new DateTime(2026, 1, 1), new DateTime(2026, 3, 31), 100),
        Row(2, new DateTime(2026, 4, 1), Open, 150)
    };

    [Fact]
    public void Should_Return_The_Row_In_Force_For_The_Payroll_Month()
    {
        LoanInstallmentSchedule.GetInstallmentAmount(10, TwoRows(), new DateTime(2026, 2, 28)).ShouldBe(100);
        LoanInstallmentSchedule.GetInstallmentAmount(10, TwoRows(), new DateTime(2026, 4, 30)).ShouldBe(150);
    }

    [Fact]
    public void Should_Fall_Back_To_The_Fixed_Amount_Before_The_First_Row()
    {
        LoanInstallmentSchedule.GetInstallmentAmount(10, TwoRows(), new DateTime(2025, 12, 31)).ShouldBe(10);
    }

    [Fact]
    public void Should_Fall_Back_To_The_Fixed_Amount_When_There_Is_No_Schedule()
    {
        LoanInstallmentSchedule.GetInstallmentAmount(10, new EmployeeLoanInstallment[0], new DateTime(2026, 4, 30)).ShouldBe(10);
    }

    [Fact]
    public void Should_Skip_The_Excluded_Row()
    {
        // Row 2 is being deleted: April has no row in force at all, so the fixed amount comes back.
        LoanInstallmentSchedule.GetInstallmentAmount(10, TwoRows(), new DateTime(2026, 4, 30), excludeInstallmentId: 2)
            .ShouldBe(10);
    }

    /// <summary>
    /// Legacy pins the two lookups to different dates: GetInstallmentAmount looks at the 1st of the month, the payroll
    /// engine at the pay date itself. Only a row starting mid-month -- which validation refuses, but data written
    /// before it may hold -- tells them apart. Kept apart on purpose.
    /// </summary>
    [Fact]
    public void Payroll_Lookup_Should_Use_The_Pay_Date_While_GetInstallmentAmount_Uses_The_First_Of_The_Month()
    {
        var rows = new List<EmployeeLoanInstallment>
        {
            Row(1, new DateTime(2026, 1, 1), new DateTime(2026, 6, 14), 100),
            Row(2, new DateTime(2026, 6, 15), Open, 150)
        };

        LoanInstallmentSchedule.GetPayrollInstallmentAmount(10, rows, new DateTime(2026, 6, 30)).ShouldBe(150);
        LoanInstallmentSchedule.GetInstallmentAmount(10, rows, new DateTime(2026, 6, 30)).ShouldBe(100);
    }

    [Fact]
    public void Chaining_Should_Close_Each_Row_The_Day_Before_The_Next_And_Leave_The_Last_Open()
    {
        var rows = new List<EmployeeLoanInstallment>
        {
            Row(1, new DateTime(2026, 6, 1), Open, 150),
            Row(2, new DateTime(2026, 1, 1), Open, 100),
            Row(3, new DateTime(2026, 9, 1), new DateTime(2026, 1, 1), 50)
        };

        var changed = LoanInstallmentSchedule.ChainEndDates(rows);

        rows.Single(r => r.Id == 2).EndDate.ShouldBe(new DateTime(2026, 5, 31));
        rows.Single(r => r.Id == 1).EndDate.ShouldBe(new DateTime(2026, 8, 31));
        rows.Single(r => r.Id == 3).EndDate.ShouldBe(Open);
        changed.Select(r => r.Id).OrderBy(i => i).ShouldBe(new[] { 1, 2, 3 });
    }

    [Fact]
    public void Chaining_Should_Report_Nothing_When_The_Rows_Already_Chain()
    {
        LoanInstallmentSchedule.ChainEndDates(TwoRows()).ShouldBeEmpty();
    }

    /// <summary>
    /// 1200 due at 100 a month from 10 Jan 2026: Jan..Dec recover 100 each and December clears it, so the end is
    /// 31 Dec 2026. (The older ceil(due / fixed) formula gave 31 Jan 2027 for the same loan.)
    /// </summary>
    [Fact]
    public void Recovery_End_Date_Should_Be_The_Month_That_Clears_The_Due_Amount()
    {
        LoanInstallmentSchedule.CalculateRecoveryEndDate(
                1200, new DateTime(2026, 1, 10), 100, new DateTime(2030, 1, 1),
                new EmployeeLoanInstallment[0], new DateTime[0])
            .ShouldBe(new DateTime(2026, 12, 31));
    }

    /// <summary>One exempted month (May) recovers nothing, so December's 100 is still owed and January clears it.</summary>
    [Fact]
    public void Recovery_End_Date_Should_Move_Out_A_Month_For_Each_Exempted_Month()
    {
        LoanInstallmentSchedule.CalculateRecoveryEndDate(
                1200, new DateTime(2026, 1, 10), 100, new DateTime(2030, 1, 1),
                new EmployeeLoanInstallment[0], new[] { new DateTime(2026, 5, 20) })
            .ShouldBe(new DateTime(2027, 1, 31));
    }

    /// <summary>
    /// 1000 due: Jan-Mar at 100 (300), then 150 from April: Apr..Sep recover 900 more, passing 1000 in August
    /// (300 + 5 x 150 = 1050). End 31 Aug 2026.
    /// </summary>
    [Fact]
    public void Recovery_End_Date_Should_Follow_The_Installment_Schedule()
    {
        LoanInstallmentSchedule.CalculateRecoveryEndDate(
                1000, new DateTime(2026, 1, 1), 10, new DateTime(2030, 1, 1), TwoRows(), new DateTime[0])
            .ShouldBe(new DateTime(2026, 8, 31));
    }

    [Fact]
    public void Recovery_End_Date_Should_Stay_When_A_Month_Has_No_Installment()
    {
        var current = new DateTime(2030, 1, 1);

        LoanInstallmentSchedule.CalculateRecoveryEndDate(
                1000, new DateTime(2026, 1, 1), 0, current, new EmployeeLoanInstallment[0], new DateTime[0])
            .ShouldBe(current);
    }

    [Fact]
    public void Recovery_End_Date_Should_Stay_Without_A_Due_Amount_Or_A_Recovery_Start()
    {
        var current = new DateTime(2030, 1, 1);

        LoanInstallmentSchedule.CalculateRecoveryEndDate(
                0, new DateTime(2026, 1, 1), 100, current, new EmployeeLoanInstallment[0], new DateTime[0])
            .ShouldBe(current);
        LoanInstallmentSchedule.CalculateRecoveryEndDate(
                1000, default, 100, current, new EmployeeLoanInstallment[0], new DateTime[0])
            .ShouldBe(current);
    }

    /// <summary>
    /// Legacy rejoin: the old rows starting AFTER the new recovery start month, plus that month itself, distinct and
    /// in order. A row starting before it (Jan) and one in it (Mar) collapse into the month itself.
    /// </summary>
    [Fact]
    public void Carry_Forward_Dates_Should_Be_The_Recovery_Start_Month_And_Later_Changes()
    {
        var oldRows = new List<EmployeeLoanInstallment>
        {
            Row(1, new DateTime(2026, 1, 1), new DateTime(2026, 2, 28), 100),
            Row(2, new DateTime(2026, 3, 1), new DateTime(2026, 5, 31), 120),
            Row(3, new DateTime(2026, 6, 1), Open, 150),
            Row(4, new DateTime(2026, 6, 1), Open, 50)
        };

        LoanInstallmentSchedule.CarryForwardEffectiveDates(new DateTime(2026, 3, 18), oldRows)
            .ShouldBe(new[] { new DateTime(2026, 3, 1), new DateTime(2026, 6, 1) });
    }
}
