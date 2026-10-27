using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSettings;
using HCMv3.TimeManagement.Services;
using NSubstitute;
using Shouldly;
using Xunit;
using static HCMv3.Tests.PayrollManagement.PayrollEngineFakes;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// Wave P9 -- <see cref="EmployeeOvertimePayCalculationService"/>, ported from legacy
/// <c>HCM10.PayrollEngine/EmployeeOvertimePayService.cs</c> (<c>Main</c> line 18,
/// <c>GetOvertimePerHourRate</c> line 157).
///
/// <para>This arm only became reachable in wave P8 (<c>OvertimeType.WageTypeId</c> restored), so these are
/// the first tests to exercise it at all.</para>
/// </summary>
public class EmployeeOvertimePayCalculationServiceTests
{
    private const int EmployeeId = 31;
    private const int RegisterId = 800;
    private const int WageTypeId = 6;
    private const int BasicWageTypeId = 1;
    private const int OvertimeTypeId = 2;

    private static readonly DateTime PayDate = new DateTime(2025, 4, 30); // 30 calendar days

    #region << Harness >>

    private sealed class Harness
    {
        public readonly FakePayrollRegisterStore Store = new FakePayrollRegisterStore();
        public readonly List<OvertimeType> OvertimeTypes = new List<OvertimeType>();
        public readonly List<OvertimeTypeDetail> OvertimeTypeDetails = new List<OvertimeTypeDetail>();
        public readonly List<EmployeeOvertime> EmployeeOvertimes = new List<EmployeeOvertime>();
        public readonly List<WageType> WageTypes = new List<WageType>();
        public readonly IPayrollAdvancePaymentService AdvancePaymentService = Substitute.For<IPayrollAdvancePaymentService>();
        public readonly IPayrollWeekOffCalculator WeekOffCalculator = Substitute.For<IPayrollWeekOffCalculator>();
        public readonly ITimeDataService TimeDataService = Substitute.For<ITimeDataService>();

        public readonly List<(DateTime PaymentDate, double Amount)> AdvancePostings = new List<(DateTime, double)>();

        public Harness()
        {
            AdvancePaymentService
                .GetRecoveryPeriods(Arg.Any<DateTime>(), Arg.Any<DateTime>())
                .Returns(ci => EmployeeBasicPayCalculationService.GetRecoveryPeriods(
                    ci.ArgAt<DateTime>(0), ci.ArgAt<DateTime>(1)));

            AdvancePaymentService
                .AdvancePaymentDedAsync(
                    Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<double>(),
                    Arg.Any<PayrollRunType>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<PayrollEngineSettings>())
                .Returns(ci =>
                {
                    AdvancePostings.Add((ci.ArgAt<DateTime>(1), ci.ArgAt<double>(3)));
                    return Task.CompletedTask;
                });

            WeekOffCalculator
                .GetActualWeekOffsAsync(Arg.Any<PayrollCalculationContext>(), Arg.Any<DateTime>())
                .Returns(Task.FromResult(0d));
        }

        public EmployeeOvertimePayCalculationService Build()
        {
            return new EmployeeOvertimePayCalculationService(
                Repo(OvertimeTypes),
                Repo(OvertimeTypeDetails),
                Repo(EmployeeOvertimes),
                Repo(WageTypes),
                Store,
                AdvancePaymentService,
                WeekOffCalculator,
                TimeDataService);
        }
    }

    private static WageType OvertimeWageType(BillingMethod billingMethod = BillingMethod.FixedDaysPerMonth, double fixedDays = 30)
    {
        return new WageType
        {
            Id = WageTypeId,
            Code = "1300",
            NameEn = "Overtime Pay",
            BillingType = BillingType.Earning,
            BillingMethod = billingMethod,
            FixedDays = fixedDays
        };
    }

    private static PayrollCalculationContext Context(Harness harness, WageType wageType, OvertimeHoursBasis basis = OvertimeHoursBasis.FixedHours)
    {
        return new PayrollCalculationContext
        {
            EmployeeId = EmployeeId,
            Employee = new Employee { Id = EmployeeId, Code = "E31" },
            PayDate = PayDate,
            PayStartDate = new DateTime(2025, 4, 1),
            PayInDate = PayDate,
            OffCycleEndDate = PayDate,
            PayrollRunType = PayrollRunType.Regular,
            WageType = wageType,
            Register = new PayrollRegisterRef
            {
                RegisterId = RegisterId,
                Target = PayrollRegisterTarget.Live,
                ForPeriod = PayDate,
                InPeriod = PayDate,
                PayrollRunType = PayrollRunType.Regular
            },
            Settings = new PayrollEngineSettings
            {
                OvertimeHoursBasis = basis,
                DefaultWorkingHours = 8
            }
        };
    }

    private static void SeedOvertimeType(Harness harness, double overtimeRate = 1.5)
    {
        harness.OvertimeTypes.Add(new OvertimeType
        {
            Id = OvertimeTypeId,
            Code = "OT1",
            NameEn = "Normal Overtime",
            OvertimeRate = overtimeRate,
            WageTypeId = WageTypeId
        });
    }

    private static void SeedBasicPayRate(Harness harness, double rate)
    {
        harness.WageTypes.Add(new WageType { Id = BasicWageTypeId, Code = "1000", IncludeInOvertimePay = true });
        harness.Store.Lines.Add(new PayrollRegisterLine
        {
            Id = 1,
            WageTypeId = BasicWageTypeId,
            WageCode = "1000",
            Rate = rate
        });
    }

    private static void SeedOvertime(Harness harness, DateTime effectiveDate, double approvedHours)
    {
        harness.EmployeeOvertimes.Add(new EmployeeOvertime
        {
            Id = harness.EmployeeOvertimes.Count + 1,
            EmployeeId = EmployeeId,
            EffectiveDate = effectiveDate,
            OvertimeTypeId = OvertimeTypeId,
            ApprovedOvertimeHours = approvedHours
        });
    }

    #endregion

    /// <summary>
    /// The FixedHours path end-to-end (legacy lines 63-69 + 200-202): rate =
    /// (basicPay / FixedDays) / defaultWorkingHours; hours = approved * OvertimeRate; amount = hours * rate.
    /// </summary>
    [Fact]
    public async Task Should_Pay_Overtime_On_The_FixedHours_Basis()
    {
        var harness = new Harness();
        var wageType = OvertimeWageType(BillingMethod.FixedDaysPerMonth, fixedDays: 30);
        harness.WageTypes.Add(wageType);
        SeedOvertimeType(harness, overtimeRate: 1.5);
        SeedBasicPayRate(harness, 720);
        SeedOvertime(harness, new DateTime(2025, 4, 10), approvedHours: 4);

        await harness.Build().CalculateAsync(Context(harness, wageType));

        // rate = (720 / 30) / 8 = 3 ; hours = 4 * 1.5 = 6 ; amount = 18
        var line = harness.Store.Lines.Single(l => l.WageTypeId == WageTypeId);
        line.Rate.ShouldBe(3);
        line.Number.ShouldBe(6);
        line.Amount.ShouldBe(18);
    }

    /// <summary>Legacy lines 204-207: CalendarDaysPerMonth divides by the month's real day count.</summary>
    [Fact]
    public async Task Should_Use_Calendar_Days_When_The_Billing_Method_Is_CalendarDaysPerMonth()
    {
        var harness = new Harness();
        var wageType = OvertimeWageType(BillingMethod.CalendarDaysPerMonth);
        harness.WageTypes.Add(wageType);
        SeedOvertimeType(harness, overtimeRate: 1);
        SeedBasicPayRate(harness, 720);
        SeedOvertime(harness, new DateTime(2025, 4, 10), approvedHours: 2);

        await harness.Build().CalculateAsync(Context(harness, wageType));

        // April has 30 days -> rate = (720 / 30) / 8 = 3 ; hours = 2 ; amount = 6
        harness.Store.Lines.Single(l => l.WageTypeId == WageTypeId).Amount.ShouldBe(6);
    }

    /// <summary>Legacy lines 209-215: WorkingDaysPerMonth subtracts the weekly-off days.</summary>
    [Fact]
    public async Task Should_Subtract_Weekly_Off_Days_When_The_Billing_Method_Is_WorkingDaysPerMonth()
    {
        var harness = new Harness();
        var wageType = OvertimeWageType(BillingMethod.WorkingDaysPerMonth);
        harness.WageTypes.Add(wageType);
        SeedOvertimeType(harness, overtimeRate: 1);
        SeedBasicPayRate(harness, 720);
        SeedOvertime(harness, new DateTime(2025, 4, 10), approvedHours: 1);

        harness.WeekOffCalculator
            .GetActualWeekOffsAsync(Arg.Any<PayrollCalculationContext>(), Arg.Any<DateTime>())
            .Returns(Task.FromResult(6d));

        await harness.Build().CalculateAsync(Context(harness, wageType));

        // salaryBasisDays = 30 - 6 = 24 -> rate = (720 / 24) / 8 = 3.75
        harness.Store.Lines.Single(l => l.WageTypeId == WageTypeId).Amount.ShouldBe(3.75);
    }

    /// <summary>Legacy lines 187-190: an OvertimeTypeDetail with a Fixed rate type wins outright.</summary>
    [Fact]
    public async Task Should_Use_The_Detail_Rate_When_The_Overtime_Rate_Type_Is_Fixed()
    {
        var harness = new Harness();
        var wageType = OvertimeWageType();
        harness.WageTypes.Add(wageType);
        SeedOvertimeType(harness, overtimeRate: 2);
        SeedBasicPayRate(harness, 720);
        SeedOvertime(harness, new DateTime(2025, 4, 10), approvedHours: 3);

        harness.OvertimeTypeDetails.Add(new OvertimeTypeDetail
        {
            Id = 1,
            OvertimeTypeId = OvertimeTypeId,
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 12, 31),
            OvertimeRateType = OvertimeRateType.Fixed,
            Rate = 10
        });

        await harness.Build().CalculateAsync(Context(harness, wageType));

        // hours = 3 * 2 = 6 ; amount = 6 * 10 = 60
        var line = harness.Store.Lines.Single(l => l.WageTypeId == WageTypeId);
        line.Rate.ShouldBe(10);
        line.Amount.ShouldBe(60);
    }

    /// <summary>Legacy lines 24-27: <c>IsNotReturnFromLeave</c> suppresses the whole rule.</summary>
    [Fact]
    public async Task Should_Do_Nothing_When_IsNotReturnFromLeave()
    {
        var harness = new Harness();
        var wageType = OvertimeWageType();
        harness.WageTypes.Add(wageType);
        SeedOvertimeType(harness);
        SeedBasicPayRate(harness, 720);
        SeedOvertime(harness, new DateTime(2025, 4, 10), approvedHours: 4);

        var context = Context(harness, wageType);
        context.IsNotReturnFromLeave = true;

        await harness.Build().CalculateAsync(context);

        harness.Store.Lines.ShouldNotContain(l => l.WageTypeId == WageTypeId);
    }

    /// <summary>
    /// <b>Legacy quirk, ported as-is</b> (legacy lines 123-146): the whole write sits inside
    /// <c>if (salaryRegisterDetail == null)</c>, so an EXISTING line silently blocks the overtime amount.
    /// </summary>
    [Fact]
    public async Task Should_Silently_Skip_The_Write_When_A_Line_Already_Exists_Legacy_Quirk()
    {
        var harness = new Harness();
        var wageType = OvertimeWageType();
        harness.WageTypes.Add(wageType);
        SeedOvertimeType(harness);
        SeedBasicPayRate(harness, 720);
        SeedOvertime(harness, new DateTime(2025, 4, 10), approvedHours: 4);

        harness.Store.Lines.Add(new PayrollRegisterLine
        {
            Id = 77,
            WageTypeId = WageTypeId,
            WageCode = "1300",
            Amount = 5
        });

        await harness.Build().CalculateAsync(Context(harness, wageType));

        // Untouched -- the computed 18 is dropped.
        harness.Store.Lines.Single(l => l.WageTypeId == WageTypeId).Amount.ShouldBe(5);
    }

    /// <summary>Legacy line 57: an employee with no approved overtime hours produces no line.</summary>
    [Fact]
    public async Task Should_Write_Nothing_When_There_Are_No_Approved_Overtime_Hours()
    {
        var harness = new Harness();
        var wageType = OvertimeWageType();
        harness.WageTypes.Add(wageType);
        SeedOvertimeType(harness);
        SeedBasicPayRate(harness, 720);

        await harness.Build().CalculateAsync(Context(harness, wageType));

        harness.Store.Lines.ShouldNotContain(l => l.WageTypeId == WageTypeId);
    }

    /// <summary>Legacy line 52-56: overtime outside the pay window is not picked up.</summary>
    [Fact]
    public async Task Should_Ignore_Overtime_Outside_The_Pay_Window()
    {
        var harness = new Harness();
        var wageType = OvertimeWageType();
        harness.WageTypes.Add(wageType);
        SeedOvertimeType(harness, overtimeRate: 1);
        SeedBasicPayRate(harness, 720);
        SeedOvertime(harness, new DateTime(2025, 3, 20), approvedHours: 10);
        SeedOvertime(harness, new DateTime(2025, 4, 10), approvedHours: 2);

        await harness.Build().CalculateAsync(Context(harness, wageType));

        harness.Store.Lines.Single(l => l.WageTypeId == WageTypeId).Number.ShouldBe(2);
    }

    /// <summary>Legacy line 29: an overtime type mapped to another wage type must not contribute.</summary>
    [Fact]
    public async Task Should_Ignore_Overtime_Types_Mapped_To_Another_Wage_Type()
    {
        var harness = new Harness();
        var wageType = OvertimeWageType();
        harness.WageTypes.Add(wageType);
        harness.OvertimeTypes.Add(new OvertimeType
        {
            Id = OvertimeTypeId,
            Code = "OT1",
            OvertimeRate = 1,
            WageTypeId = 4242
        });
        SeedBasicPayRate(harness, 720);
        SeedOvertime(harness, new DateTime(2025, 4, 10), approvedHours: 4);

        await harness.Build().CalculateAsync(Context(harness, wageType));

        harness.Store.Lines.ShouldNotContain(l => l.WageTypeId == WageTypeId);
    }

    /// <summary>
    /// The DutyHours path (legacy lines 74-91 + 169-178): the rate is computed PER overtime record from
    /// <c>GetTotalHoursInDay</c>, and -- <b>legacy quirk</b> -- the line's <c>Rate</c> stays 0 because
    /// <c>totalotHourRate</c> is only assigned on the FixedHours path.
    /// </summary>
    [Fact]
    public async Task Should_Compute_Per_Record_Rates_And_Leave_Rate_At_Zero_On_The_DutyHours_Basis()
    {
        var harness = new Harness();
        var wageType = OvertimeWageType(BillingMethod.FixedDaysPerMonth, fixedDays: 30);
        harness.WageTypes.Add(wageType);
        SeedOvertimeType(harness, overtimeRate: 1);
        SeedBasicPayRate(harness, 720);
        SeedOvertime(harness, new DateTime(2025, 4, 10), approvedHours: 2);

        harness.TimeDataService
            .GetTotalHoursInDayAsync(Arg.Any<Employee>(), Arg.Any<DateTime>())
            .Returns(Task.FromResult(6d));

        await harness.Build().CalculateAsync(Context(harness, wageType, OvertimeHoursBasis.DutyHours));

        // per-record rate = (720 / 30) / 6 = 4 ; hours = 2 ; amount = 8
        var line = harness.Store.Lines.Single(l => l.WageTypeId == WageTypeId);
        line.Amount.ShouldBe(8);
        line.Number.ShouldBe(2);
        line.Rate.ShouldBe(0); // legacy quirk
    }

    /// <summary>
    /// Legacy lines 171-177: a throwing <c>GetTotalHoursInDay</c> is swallowed and the basis falls back to
    /// <c>DefaultWorkingHours</c>.
    /// </summary>
    [Fact]
    public async Task Should_Fall_Back_To_Default_Working_Hours_When_GetTotalHoursInDay_Throws()
    {
        var harness = new Harness();
        var wageType = OvertimeWageType(BillingMethod.FixedDaysPerMonth, fixedDays: 30);
        harness.WageTypes.Add(wageType);
        SeedOvertimeType(harness, overtimeRate: 1);
        SeedBasicPayRate(harness, 720);
        SeedOvertime(harness, new DateTime(2025, 4, 10), approvedHours: 2);

        harness.TimeDataService
            .GetTotalHoursInDayAsync(Arg.Any<Employee>(), Arg.Any<DateTime>())
            .Returns<Task<double>>(_ => throw new InvalidOperationException("duty period missing"));

        await harness.Build().CalculateAsync(Context(harness, wageType, OvertimeHoursBasis.DutyHours));

        // falls back to DefaultWorkingHours = 8 -> rate = (720 / 30) / 8 = 3 ; amount = 6
        harness.Store.Lines.Single(l => l.WageTypeId == WageTypeId).Amount.ShouldBe(6);
    }
}
