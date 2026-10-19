using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.SystemSettings;
using HCMv3.SystemSettings.Services;
using HCMv3.TimeManagement.Services;
using NSubstitute;
using Shouldly;
using Xunit;
using static HCMv3.Tests.PayrollManagement.PayrollEngineFakes;

namespace HCMv3.Tests.PersonnelAdministration;

/// <summary>
/// Item 43 -- <see cref="ExpectedOvertimeAmountService"/>, ported from legacy
/// <c>HCM10.Module/BusinessObjects/PersonnelAdministration/EmployeeOvertime.cs</c>: the non-persistent
/// <c>ExpectedOvertimeAmount</c> getter (lines 87-97) and <c>GetExpectedOvertimeAmount</c> (lines 198-302).
/// Every expected value below is worked out by hand from those lines.
///
/// Same in-memory repository harness as <c>EmployeeOvertimePayCalculationServiceTests</c>.
/// </summary>
// ReSharper disable once InconsistentNaming
public class ExpectedOvertimeAmountService_Tests
{
    private const int EmployeeId = 41;
    private const int OvertimeTypeId = 7;
    private const int OtWageTypeId = 60;
    private const int BasicWageTypeId = 1;
    private const int HousingWageTypeId = 2;
    private const int TransportWageTypeId = 3;

    private static readonly DateTime April2025 = new DateTime(2025, 4, 15); // 30 days in month

    private sealed class Harness
    {
        public readonly List<Employee> Employees = new List<Employee> { new Employee { Id = EmployeeId, Code = "E41" } };
        public readonly List<OvertimeType> OvertimeTypes = new List<OvertimeType>();
        public readonly List<OvertimeTypeDetail> OvertimeTypeDetails = new List<OvertimeTypeDetail>();
        public readonly List<WageType> WageTypes = new List<WageType>();
        public readonly List<EmployeeBasicPay> BasicPays = new List<EmployeeBasicPay>();
        public readonly List<EmployeeBasicPayDetail> BasicPayDetails = new List<EmployeeBasicPayDetail>();
        public readonly ISysSettingsAccessor SettingsAccessor = Substitute.For<ISysSettingsAccessor>();
        public readonly ITimeDataService TimeDataService = Substitute.For<ITimeDataService>();
        public readonly IAccrualService AccrualService = Substitute.For<IAccrualService>();

        public ManagePayrollSettings Settings = new ManagePayrollSettings
        {
            OvertimeHoursBasis = OvertimeHoursBasis.FixedHours,
            DefaultWorkingHours = 8
        };

        public Harness()
        {
            WageTypes.Add(new WageType { Id = BasicWageTypeId, Code = "1000", IncludeInOvertimePay = true });
            WageTypes.Add(new WageType { Id = HousingWageTypeId, Code = "1010", IncludeInOvertimePay = true });
            WageTypes.Add(new WageType { Id = TransportWageTypeId, Code = "1020", IncludeInOvertimePay = false });
        }

        public ExpectedOvertimeAmountService Build()
        {
            SettingsAccessor.GetPayrollSettingsAsync().Returns(_ => Task.FromResult(Settings));

            return new ExpectedOvertimeAmountService(
                Repo(Employees),
                Repo(OvertimeTypes),
                Repo(OvertimeTypeDetails),
                Repo(WageTypes),
                Repo(BasicPays),
                Repo(BasicPayDetails),
                SettingsAccessor,
                TimeDataService,
                AccrualService);
        }

        public void OvertimeType(double overtimeRate = 1.5, BillingMethod billingMethod = BillingMethod.FixedDaysPerMonth,
            double fixedDays = 30, bool withWageType = true)
        {
            if (withWageType)
            {
                WageTypes.Add(new WageType
                {
                    Id = OtWageTypeId,
                    Code = "1300",
                    BillingMethod = billingMethod,
                    FixedDays = fixedDays
                });
            }

            OvertimeTypes.Add(new OvertimeType
            {
                Id = OvertimeTypeId,
                Code = "OT",
                NameEn = "Normal Day OT",
                OvertimeRate = overtimeRate,
                WageTypeId = withWageType ? OtWageTypeId : (int?)null
            });
        }

        public void FixedRate(double rate, DateTime start, DateTime end)
        {
            OvertimeTypeDetails.Add(new OvertimeTypeDetail
            {
                Id = OvertimeTypeDetails.Count + 1,
                OvertimeTypeId = OvertimeTypeId,
                StartDate = start,
                EndDate = end,
                OvertimeRateType = OvertimeRateType.Fixed,
                Rate = rate
            });
        }

        public void BasicPay(int id, DateTime start, DateTime end, params (int WageTypeId, double Amount)[] lines)
        {
            BasicPays.Add(new EmployeeBasicPay { Id = id, EmployeeId = EmployeeId, StartDate = start, EndDate = end });
            foreach (var line in lines)
            {
                BasicPayDetails.Add(new EmployeeBasicPayDetail
                {
                    Id = BasicPayDetails.Count + 1,
                    EmployeeBasicPayId = id,
                    WageTypeId = line.WageTypeId,
                    Amount = line.Amount
                });
            }
        }

        /// <summary>Basic 400 + housing 200 count; transport 100 does not (line 270).</summary>
        public void StandardBasicPay() =>
            BasicPay(1, new DateTime(2025, 1, 1), new DateTime(2025, 12, 31),
                (BasicWageTypeId, 400), (HousingWageTypeId, 200), (TransportWageTypeId, 100));
    }

    private static Task<double> Calculate(Harness harness, double approvedHours = 4, DateTime? date = null) =>
        harness.Build().GetExpectedOvertimeAmountAsync(EmployeeId, OvertimeTypeId, date ?? April2025, approvedHours);

    [Fact]
    public async Task Should_Use_Basic_Pay_Over_Fixed_Days_And_Default_Hours_For_FixedDaysPerMonth()
    {
        // Lines 266-274: (600 / 30) / 8 = 2.5 per hour. Line 206: 4 h x 1.5 = 6 h. Line 294: 6 x 2.5 = 15.
        var harness = new Harness();
        harness.OvertimeType(billingMethod: BillingMethod.FixedDaysPerMonth, fixedDays: 30);
        harness.StandardBasicPay();

        (await Calculate(harness)).ShouldBe(15);
    }

    [Fact]
    public async Task Should_Use_Days_In_The_Month_For_CalendarDaysPerMonth()
    {
        // Lines 278-279: February 2024 has 29 days. (580 / 29) / 8 = 2.5; 6 h x 2.5 = 15.
        var harness = new Harness();
        harness.OvertimeType(billingMethod: BillingMethod.CalendarDaysPerMonth);
        harness.BasicPay(1, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), (BasicWageTypeId, 580));

        (await Calculate(harness, date: new DateTime(2024, 2, 10))).ShouldBe(15);
    }

    [Fact]
    public async Task Should_Take_Week_Offs_Off_The_Month_For_WorkingDaysPerMonth()
    {
        // Lines 283-286: April has 30 days, 4 week-offs (AccrualService.GetActualWeekOffs) -> 26.
        // (520 / 26) / 8 = 2.5; 6 h x 2.5 = 15.
        var harness = new Harness();
        harness.OvertimeType(billingMethod: BillingMethod.WorkingDaysPerMonth);
        harness.BasicPay(1, new DateTime(2025, 1, 1), new DateTime(2025, 12, 31), (BasicWageTypeId, 520));
        harness.AccrualService.GetActualWeekOffsAsync(EmployeeId, April2025).Returns(Task.FromResult(4d));

        (await Calculate(harness)).ShouldBe(15);
    }

    [Fact]
    public async Task Should_Use_The_Fixed_Rate_When_A_Fixed_Detail_Covers_The_Date()
    {
        // Lines 237-250: Rate 3 per hour; 6 h x 3 = 18. Basic pay is not read.
        var harness = new Harness();
        harness.OvertimeType();
        harness.StandardBasicPay();
        harness.FixedRate(3, new DateTime(2025, 1, 1), new DateTime(2025, 12, 31));

        (await Calculate(harness)).ShouldBe(18);
    }

    [Fact]
    public async Task Should_Fall_Back_To_Actual_When_No_Detail_Covers_The_Date()
    {
        // Line 245: no detail in range -> Actual. The 2024 fixed rate is ignored for an April 2025 date.
        var harness = new Harness();
        harness.OvertimeType();
        harness.StandardBasicPay();
        harness.FixedRate(3, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        (await Calculate(harness)).ShouldBe(15);
    }

    [Fact]
    public async Task Should_Give_Zero_With_A_Fixed_Rate_When_The_Hours_Basis_Is_Zero()
    {
        // Legacy parity, pinned: line 235 (`if (otHoursBasis != 0)`) wraps the Fixed branch too, although the
        // fixed rate never divides by the basis.
        var harness = new Harness();
        harness.Settings.DefaultWorkingHours = 0;
        harness.OvertimeType();
        harness.FixedRate(3, new DateTime(2025, 1, 1), new DateTime(2025, 12, 31));

        (await Calculate(harness)).ShouldBe(0);
    }

    [Fact]
    public async Task Should_Give_Zero_When_The_Overtime_Type_Has_No_Wage_Type()
    {
        // Legacy parity, pinned: line 208 skips the whole rate lookup, so the rate stays 0 (line 205) even
        // with a fixed rate configured.
        var harness = new Harness();
        harness.OvertimeType(withWageType: false);
        harness.StandardBasicPay();
        harness.FixedRate(3, new DateTime(2025, 1, 1), new DateTime(2025, 12, 31));

        (await Calculate(harness)).ShouldBe(0);
    }

    [Fact]
    public async Task Should_Use_Duty_Hours_When_The_Basis_Is_DutyHours()
    {
        // Lines 221-225: 10 duty hours. (600 / 30) / 10 = 2; 6 h x 2 = 12.
        var harness = new Harness();
        harness.Settings.OvertimeHoursBasis = OvertimeHoursBasis.DutyHours;
        harness.OvertimeType();
        harness.StandardBasicPay();
        harness.TimeDataService.GetTotalHoursInDayAsync(Arg.Any<Employee>(), April2025).Returns(Task.FromResult(10d));

        (await Calculate(harness)).ShouldBe(12);
    }

    [Fact]
    public async Task Should_Fall_Back_To_Default_Hours_When_The_Duty_Hours_Lookup_Fails()
    {
        // Lines 223-230: the failure is swallowed (empty catch), the basis stays 0 and becomes the default 8.
        var harness = new Harness();
        harness.Settings.OvertimeHoursBasis = OvertimeHoursBasis.DutyHours;
        harness.OvertimeType();
        harness.StandardBasicPay();
        harness.TimeDataService.GetTotalHoursInDayAsync(Arg.Any<Employee>(), Arg.Any<DateTime>())
            .Returns(Task.FromException<double>(new InvalidOperationException("no duty")));

        (await Calculate(harness)).ShouldBe(15);
    }

    [Fact]
    public async Task Should_Use_Only_The_First_Basic_Pay_Record_Covering_The_Date()
    {
        // Lines 267-268: `.FirstOrDefault()` -- a second overlapping record is ignored.
        var harness = new Harness();
        harness.OvertimeType();
        harness.StandardBasicPay();
        harness.BasicPay(2, new DateTime(2025, 4, 1), new DateTime(2025, 4, 30), (BasicWageTypeId, 9000));

        (await Calculate(harness)).ShouldBe(15);
    }

    [Fact]
    public async Task Should_Give_Zero_When_There_Is_No_Basic_Pay_On_The_Date()
    {
        // Lines 266-270: basic stays 0, so the rate is 0.
        var harness = new Harness();
        harness.OvertimeType();
        harness.BasicPay(1, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), (BasicWageTypeId, 600));

        (await Calculate(harness)).ShouldBe(0);
    }

    [Fact]
    public async Task Should_Round_To_Three_Decimals()
    {
        // Line 301: (1000 / 30) / 8 = 4.1666...; 1 h x 1.0 = 1 h -> 4.167.
        var harness = new Harness();
        harness.OvertimeType(overtimeRate: 1.0);
        harness.BasicPay(1, new DateTime(2025, 1, 1), new DateTime(2025, 12, 31), (BasicWageTypeId, 1000));

        (await Calculate(harness, approvedHours: 1)).ShouldBe(4.167);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public async Task Should_Give_Zero_When_Approved_Hours_Are_Not_Positive(double approvedHours)
    {
        // Line 92 (`ApprovedOvertimeHours != 0`) and line 203 (`approvedOvertimeHours > 0`).
        var harness = new Harness();
        harness.OvertimeType();
        harness.StandardBasicPay();

        (await Calculate(harness, approvedHours)).ShouldBe(0);
    }

    [Fact]
    public async Task Should_Give_Zero_When_The_Overtime_Type_Does_Not_Exist()
    {
        // Line 92: `this.OvertimeType != null`.
        var harness = new Harness();
        harness.StandardBasicPay();

        (await Calculate(harness)).ShouldBe(0);
    }

    [Fact]
    public async Task Should_Divide_By_Zero_Fixed_Days_Unguarded()
    {
        // Legacy parity, pinned: line 274 divides by FixedDays with no guard. In double arithmetic 600 / 0 is
        // +Infinity, and Math.Round leaves it (line 301). Legacy showed the same.
        var harness = new Harness();
        harness.OvertimeType(fixedDays: 0);
        harness.StandardBasicPay();

        (await Calculate(harness)).ShouldBe(double.PositiveInfinity);
    }

    [Fact]
    public async Task Should_Return_One_Amount_Per_Row_For_A_Batch()
    {
        var harness = new Harness();
        harness.OvertimeType();
        harness.StandardBasicPay();
        var service = harness.Build();

        var amounts = await service.GetExpectedOvertimeAmountsAsync(new[]
        {
            new EmployeeOvertime { Id = 1, EmployeeId = EmployeeId, OvertimeTypeId = OvertimeTypeId, EffectiveDate = April2025, ApprovedOvertimeHours = 4 },
            new EmployeeOvertime { Id = 2, EmployeeId = EmployeeId, OvertimeTypeId = OvertimeTypeId, EffectiveDate = April2025, ApprovedOvertimeHours = 2 },
            new EmployeeOvertime { Id = 3, EmployeeId = EmployeeId, OvertimeTypeId = OvertimeTypeId, EffectiveDate = April2025, ApprovedOvertimeHours = 0 }
        });

        amounts[1].ShouldBe(15);
        amounts[2].ShouldBe(7.5);
        amounts[3].ShouldBe(0);
    }
}
