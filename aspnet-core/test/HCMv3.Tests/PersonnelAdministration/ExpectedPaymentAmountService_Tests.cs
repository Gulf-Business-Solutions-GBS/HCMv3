using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.SystemSettings;
using HCMv3.SystemSettings.Services;
using NSubstitute;
using Shouldly;
using Xunit;
using static HCMv3.Tests.PayrollManagement.PayrollEngineFakes;

namespace HCMv3.Tests.PersonnelAdministration;

/// <summary>
/// <see cref="ExpectedPaymentAmountService"/>, ported from legacy <c>EmployeePayment.ExpectedAmount</c> /
/// <c>GetExpectedAmount</c>. Partial-pay salary in every case is basic 300 + housing 100 = 400 (transport 50
/// is not partial pay). April 2025 has 30 days. Every expected value is worked out by hand.
/// </summary>
// ReSharper disable once InconsistentNaming
public class ExpectedPaymentAmountService_Tests
{
    private const int EmployeeId = 12;
    private const int PaymentWageTypeId = 50;
    private const int BasicWageTypeId = 1;
    private const int HousingWageTypeId = 2;
    private const int TransportWageTypeId = 3;

    private static readonly DateTime April2025 = new DateTime(2025, 4, 15);

    private sealed class Harness
    {
        public readonly List<WageType> WageTypes = new List<WageType>();
        public readonly List<EmployeeBasicPay> BasicPays = new List<EmployeeBasicPay>();
        public readonly List<EmployeeBasicPayDetail> BasicPayDetails = new List<EmployeeBasicPayDetail>();
        public readonly ISysSettingsAccessor SettingsAccessor = Substitute.For<ISysSettingsAccessor>();
        public readonly IAccrualService AccrualService = Substitute.For<IAccrualService>();
        public ManagePayrollSettings Settings = new ManagePayrollSettings { DefaultWorkingHours = 8 };

        public Harness(BillingMethod billingMethod = BillingMethod.CalendarDaysPerMonth, double fixedDays = 0)
        {
            WageTypes.Add(new WageType { Id = BasicWageTypeId, Code = "1000", IncludeInPartialPay = true });
            WageTypes.Add(new WageType { Id = HousingWageTypeId, Code = "1010", IncludeInPartialPay = true });
            WageTypes.Add(new WageType { Id = TransportWageTypeId, Code = "1020", IncludeInPartialPay = false });
            WageTypes.Add(new WageType { Id = PaymentWageTypeId, Code = "3000", BillingMethod = billingMethod, FixedDays = fixedDays });
        }

        public void BasicPay(int id, PayInterval interval = PayInterval.Monthly, double basic = 300,
            DateTime? start = null, DateTime? end = null)
        {
            BasicPays.Add(new EmployeeBasicPay
            {
                Id = id,
                EmployeeId = EmployeeId,
                StartDate = start ?? new DateTime(2025, 1, 1),
                EndDate = end ?? new DateTime(2025, 12, 31),
                PayInterval = interval
            });
            BasicPayDetails.Add(new EmployeeBasicPayDetail { Id = BasicPayDetails.Count + 1, EmployeeBasicPayId = id, WageTypeId = BasicWageTypeId, Amount = basic });
            BasicPayDetails.Add(new EmployeeBasicPayDetail { Id = BasicPayDetails.Count + 1, EmployeeBasicPayId = id, WageTypeId = HousingWageTypeId, Amount = 100 });
            BasicPayDetails.Add(new EmployeeBasicPayDetail { Id = BasicPayDetails.Count + 1, EmployeeBasicPayId = id, WageTypeId = TransportWageTypeId, Amount = 50 });
        }

        public Task<double> Calculate(UnitType unitType, double number, DateTime? date = null)
        {
            SettingsAccessor.GetPayrollSettingsAsync().Returns(_ => Task.FromResult(Settings));
            var service = new ExpectedPaymentAmountService(
                Repo(WageTypes), Repo(BasicPays), Repo(BasicPayDetails), SettingsAccessor, AccrualService);
            return service.GetExpectedAmountAsync(EmployeeId, PaymentWageTypeId, date ?? April2025, unitType, number);
        }
    }

    [Fact]
    public async Task Should_Return_Zero_For_Amount_Unit_Type()
    {
        var harness = new Harness();
        harness.BasicPay(1);

        (await harness.Calculate(UnitType.Amount, 3)).ShouldBe(0);
    }

    [Fact]
    public async Task Should_Return_Zero_When_Number_Is_Zero()
    {
        var harness = new Harness();
        harness.BasicPay(1);

        (await harness.Calculate(UnitType.Day, 0)).ShouldBe(0);
    }

    [Fact]
    public async Task Should_Divide_Partial_Pay_By_Calendar_Days_For_Days()
    {
        var harness = new Harness(BillingMethod.CalendarDaysPerMonth);
        harness.BasicPay(1);

        // 400 / 30 * 3 = 40
        (await harness.Calculate(UnitType.Day, 3)).ShouldBe(40);
    }

    [Fact]
    public async Task Should_Convert_Hours_To_Days_With_Default_Working_Hours()
    {
        var harness = new Harness(BillingMethod.CalendarDaysPerMonth);
        harness.BasicPay(1);

        // 12 h / 8 = 1.5 days; 400 / 30 * 1.5 = 20
        (await harness.Calculate(UnitType.Hour, 12)).ShouldBe(20);
    }

    [Fact]
    public async Task Should_Return_Zero_For_Hours_When_Default_Working_Hours_Is_Zero()
    {
        var harness = new Harness();
        harness.BasicPay(1);
        harness.Settings.DefaultWorkingHours = 0;

        (await harness.Calculate(UnitType.Hour, 12)).ShouldBe(0);
    }

    [Fact]
    public async Task Should_Subtract_Actual_Week_Offs_For_Working_Days()
    {
        var harness = new Harness(BillingMethod.WorkingDaysPerMonth);
        harness.BasicPay(1);
        harness.AccrualService.GetActualWeekOffsAsync(EmployeeId, April2025).Returns(Task.FromResult(4d));

        // 400 / (30 - 4) * 2 = 30.76923 -> 30.769
        (await harness.Calculate(UnitType.Day, 2)).ShouldBe(30.769);
    }

    [Fact]
    public async Task Should_Return_Zero_For_Working_Days_When_No_Salary_Basis_Days_Remain()
    {
        var harness = new Harness(BillingMethod.WorkingDaysPerMonth);
        harness.BasicPay(1);
        harness.AccrualService.GetActualWeekOffsAsync(EmployeeId, April2025).Returns(Task.FromResult(30d));

        (await harness.Calculate(UnitType.Day, 2)).ShouldBe(0);
    }

    [Fact]
    public async Task Should_Divide_By_Wage_Type_Fixed_Days()
    {
        var harness = new Harness(BillingMethod.FixedDaysPerMonth, fixedDays: 25);
        harness.BasicPay(1);

        // 400 / 25 * 2 = 32
        (await harness.Calculate(UnitType.Day, 2)).ShouldBe(32);
    }

    [Fact]
    public async Task Should_Return_Zero_For_Fixed_Days_When_Fixed_Days_Is_Zero()
    {
        var harness = new Harness(BillingMethod.FixedDaysPerMonth, fixedDays: 0);
        harness.BasicPay(1);

        (await harness.Calculate(UnitType.Day, 2)).ShouldBe(0);
    }

    [Fact]
    public async Task Should_Return_Zero_For_Fixed_Amount_Billing()
    {
        var harness = new Harness(BillingMethod.FixedAmountPerMonth);
        harness.BasicPay(1);

        (await harness.Calculate(UnitType.Day, 2)).ShouldBe(0);
    }

    [Fact]
    public async Task Should_Use_Pay_Rate_As_Day_Rate_For_Daily_Pay_Interval()
    {
        var harness = new Harness(BillingMethod.FixedDaysPerMonth, fixedDays: 25);
        harness.BasicPay(1, PayInterval.Daily);

        // daily: 400 per day * 2 = 800, billing method ignored
        (await harness.Calculate(UnitType.Day, 2)).ShouldBe(800);
    }

    [Fact]
    public async Task Should_Return_Zero_For_Other_Pay_Intervals()
    {
        var harness = new Harness();
        harness.BasicPay(1, PayInterval.Hourly);

        (await harness.Calculate(UnitType.Day, 2)).ShouldBe(0);
    }

    [Fact]
    public async Task Should_Return_Zero_When_No_Basic_Pay_Covers_The_Date()
    {
        var harness = new Harness();
        harness.BasicPay(1, start: new DateTime(2025, 5, 1), end: new DateTime(2025, 12, 31));

        (await harness.Calculate(UnitType.Day, 2)).ShouldBe(0);
    }

    [Fact]
    public async Task Should_Use_First_Basic_Pay_Record_Covering_The_Date()
    {
        var harness = new Harness();
        harness.BasicPay(2, basic: 800); // 900 partial pay, later id
        harness.BasicPay(1);             // 400 partial pay, first id

        // 400 / 30 * 3 = 40
        (await harness.Calculate(UnitType.Day, 3)).ShouldBe(40);
    }

    [Fact]
    public async Task Should_Round_To_Three_Decimals()
    {
        var harness = new Harness();
        harness.BasicPay(1);

        // 400 / 30 = 13.3333 -> 13.333
        (await harness.Calculate(UnitType.Day, 1)).ShouldBe(13.333);
    }

    [Fact]
    public async Task Should_Return_Zero_For_A_Negative_Result()
    {
        var harness = new Harness();
        harness.BasicPay(1);

        (await harness.Calculate(UnitType.Day, -2)).ShouldBe(0);
    }
}
