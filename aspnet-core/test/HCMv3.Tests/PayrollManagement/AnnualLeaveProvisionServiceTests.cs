using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;
using static HCMv3.Tests.PayrollManagement.PayrollEngineFakes;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// Wave P5 -- <see cref="AnnualLeaveProvisionService"/>, ported from legacy
/// <c>HCM10.PayrollEngine/EmployeeCalculatedPayService.cs</c> <c>GetAnnualLeaveProvision</c> (2281),
/// <c>GetTotalAnnualLeaveProvision</c> (2317), <c>CalculateAnnualLeaveProvision</c> (2356),
/// <c>GetAnnualLeaveProvisionDayWiseEntry</c> (2422) and <c>GetProvisionChangeAnnualLeave</c> (2531).
///
/// <para>Pins the money maths, the two legacy modes (register-based vs. period-wise), and the single
/// <c>Math.Round(x / 26, 6)</c> in the whole scope.</para>
/// </summary>
public class AnnualLeaveProvisionServiceTests
{
    private const int RegisterId = 900;
    private const int EmployeeId = 7;
    private const int AnnualLeaveAbsenceTypeId = 1;

    /// <summary>Wage types that carry a WageTypeLeave row for annual leave (legacy's includeInPaidLeaveWageTypes).</summary>
    private const int BasicWageTypeId = 101;
    private const int HousingWageTypeId = 102;

    private static readonly DateTime PayDate = new DateTime(2025, 3, 31);

    #region << Builders >>

    private static PayrollCalculationContext Context(WageType wageType, bool periodWise = false, Employee employee = null)
    {
        return new PayrollCalculationContext
        {
            EmployeeId = EmployeeId,
            Employee = employee,
            PayDate = PayDate,
            PayStartDate = new DateTime(2025, 3, 1),
            PayInDate = PayDate,
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
                IsPeriodwiseProvision = periodWise,
                AnnualLeaveAbsenceTypeId = AnnualLeaveAbsenceTypeId
            }
        };
    }

    private static WageType Wage(int id, string code, double fixedDays = 26)
    {
        return new WageType { Id = id, TenantId = 1, Code = code, NameEn = code, FixedDays = fixedDays, BillingType = BillingType.Provision, Status = true };
    }

    private static AnnualLeaveProvisionService Service(
        FakePayrollRegisterStore store,
        IEnumerable<SalaryRegister> registers = null,
        IEnumerable<EmployeeBasicPay> basicPays = null,
        IEnumerable<EmployeeBasicPayDetail> basicPayDetails = null,
        IEnumerable<EmployeeAbsAdjustment> absAdjustments = null,
        IEnumerable<HCMv3.TimeManagement.TimeRegister> timeRegisters = null,
        IEnumerable<Employee> employees = null)
    {
        var wageTypeLeaves = new[]
        {
            new WageTypeLeave { Id = 1, TenantId = 1, WageTypeId = BasicWageTypeId, AbsenceTypeId = AnnualLeaveAbsenceTypeId },
            new WageTypeLeave { Id = 2, TenantId = 1, WageTypeId = HousingWageTypeId, AbsenceTypeId = AnnualLeaveAbsenceTypeId }
        };

        return new AnnualLeaveProvisionService(
            store,
            Repo(registers ?? new List<SalaryRegister>()),
            Repo(wageTypeLeaves),
            Repo(new[] { new AbsenceType { Id = AnnualLeaveAbsenceTypeId, TenantId = 1, Code = "1" } }),
            Repo(employees ?? new List<Employee>()),
            Repo(basicPays ?? new List<EmployeeBasicPay>()),
            Repo(basicPayDetails ?? new List<EmployeeBasicPayDetail>()),
            Repo(absAdjustments ?? new List<EmployeeAbsAdjustment>()),
            Repo(timeRegisters ?? new List<HCMv3.TimeManagement.TimeRegister>()));
    }

    private static void SeedPaidLeaveRates(FakePayrollRegisterStore store)
    {
        store.Lines.Add(new PayrollRegisterLine { Id = 11, WageTypeId = BasicWageTypeId, WageCode = "1000", Rate = 1000, Amount = 1000 });
        store.Lines.Add(new PayrollRegisterLine { Id = 12, WageTypeId = HousingWageTypeId, WageCode = "1001", Rate = 300, Amount = 300 });
        // A line that is NOT an annual-leave-paid wage type -- must be excluded from the Rate sum.
        store.Lines.Add(new PayrollRegisterLine { Id = 13, WageTypeId = 999, WageCode = "1005", Rate = 500, Amount = 500 });
    }

    #endregion

    #region << GetAnnualLeaveProvision (legacy 2281) >>

    /// <summary>Legacy lines 2295-2298: <c>(sum of paid-leave Rates / FixedDays) * "4010".Number</c>.</summary>
    [Fact]
    public async Task GetAnnualLeaveProvision_Should_Prorate_The_Paid_Leave_Rate_Over_FixedDays()
    {
        var store = new FakePayrollRegisterStore();
        SeedPaidLeaveRates(store);
        store.Lines.Add(new PayrollRegisterLine { Id = 20, WageTypeId = 410, WageCode = "4010", Number = 2.5 });

        var result = await Service(store).GetAnnualLeaveProvisionAsync(Context(Wage(31, "3001")));

        result.Rate.ShouldBe(1300);
        result.Number.ShouldBe(2.5);
        result.Amount.ShouldBe(1300d / 26 * 2.5, 1e-9);
    }

    /// <summary>Legacy lines 2305-2310: no "4010" line => Rate/Number/Amount all zeroed.</summary>
    [Fact]
    public async Task GetAnnualLeaveProvision_Should_Zero_Everything_When_The_4010_Line_Is_Absent()
    {
        var store = new FakePayrollRegisterStore();
        SeedPaidLeaveRates(store);

        var result = await Service(store).GetAnnualLeaveProvisionAsync(Context(Wage(31, "3001")));

        result.Rate.ShouldBe(0);
        result.Number.ShouldBe(0);
        result.Amount.ShouldBe(0);
    }

    /// <summary>Legacy lines 2324/2335: the TOTAL variant reads "4022", not "4010".</summary>
    [Fact]
    public async Task GetTotalAnnualLeaveProvision_Should_Read_The_4022_Line()
    {
        var store = new FakePayrollRegisterStore();
        SeedPaidLeaveRates(store);
        store.Lines.Add(new PayrollRegisterLine { Id = 20, WageTypeId = 410, WageCode = "4010", Number = 2.5 });
        store.Lines.Add(new PayrollRegisterLine { Id = 22, WageTypeId = 422, WageCode = "4022", Number = 40 });

        var result = await Service(store).GetTotalAnnualLeaveProvisionAsync(Context(Wage(34, "3004")));

        result.Number.ShouldBe(40);
        result.Amount.ShouldBe(1300d / 26 * 40, 1e-9);
    }

    #endregion

    #region << CalculateAnnualLeaveProvision -- period-wise mode (legacy 2356) >>

    /// <summary>
    /// Legacy lines 2387-2420: per basic-pay sub-period,
    /// <c>(basicPay / FixedDays) * (accrual - leaveUsed - absenceAdjustments)</c>, accumulated. Rate is
    /// explicitly zeroed on this path (legacy line 2418).
    /// </summary>
    [Fact]
    public async Task PeriodWise_Should_Accrue_Per_BasicPay_SubPeriod_And_Zero_The_Rate()
    {
        var store = new FakePayrollRegisterStore();
        SeedPaidLeaveRates(store);
        store.Lines.Add(new PayrollRegisterLine { Id = 20, WageTypeId = 410, WageCode = "4010", Number = 2.5 });

        var basicPay = new EmployeeBasicPay
        {
            Id = 60,
            TenantId = 1,
            EmployeeId = EmployeeId,
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2030, 12, 31)
        };

        var basicPayDetails = new[]
        {
            new EmployeeBasicPayDetail { Id = 1, TenantId = 1, EmployeeBasicPayId = 60, WageTypeId = BasicWageTypeId, Amount = 1000 },
            new EmployeeBasicPayDetail { Id = 2, TenantId = 1, EmployeeBasicPayId = 60, WageTypeId = HousingWageTypeId, Amount = 300 },
            // Not a paid-leave wage type -- must be excluded.
            new EmployeeBasicPayDetail { Id = 3, TenantId = 1, EmployeeBasicPayId = 60, WageTypeId = 999, Amount = 500 }
        };

        var timeRegisters = new[]
        {
            new HCMv3.TimeManagement.TimeRegister { Id = 1, EmployeeId = EmployeeId, DayDate = new DateTime(2025, 3, 5), AnnualLeaveAccrualPerDay = 2 },
            new HCMv3.TimeManagement.TimeRegister { Id = 2, EmployeeId = EmployeeId, DayDate = new DateTime(2025, 3, 6), AnnualLeaveAccrualPerDay = 1, AnnualLeaveDay = 0.5, LeaveAdjDay = 0.25 },
            // Outside the period -- must be ignored.
            new HCMv3.TimeManagement.TimeRegister { Id = 3, EmployeeId = EmployeeId, DayDate = new DateTime(2025, 2, 20), AnnualLeaveAccrualPerDay = 99 }
        };

        var adjustments = new[]
        {
            new EmployeeAbsAdjustment { Id = 1, TenantId = 1, EmployeeId = EmployeeId, AbsenceTypeId = AnnualLeaveAbsenceTypeId, EffectiveDate = new DateTime(2025, 3, 10), AdjustmentType = AdjustmentType.Adjust, QuotaAdjusted = 0.25 },
            // NoImpactOnQuota rows are excluded (legacy line 2406).
            new EmployeeAbsAdjustment { Id = 2, TenantId = 1, EmployeeId = EmployeeId, AbsenceTypeId = AnnualLeaveAbsenceTypeId, EffectiveDate = new DateTime(2025, 3, 11), AdjustmentType = AdjustmentType.NoImpactOnQuota, QuotaAdjusted = 5 }
        };

        var service = Service(store, basicPays: new[] { basicPay }, basicPayDetails: basicPayDetails, absAdjustments: adjustments, timeRegisters: timeRegisters);

        var result = await service.GetAnnualLeaveProvisionAsync(Context(Wage(31, "3001"), periodWise: true));

        // accrual 3 - used 0.75 - adjustments 0.25 = 2
        result.Rate.ShouldBe(0);
        result.Number.ShouldBe(2, 1e-9);
        result.Amount.ShouldBe(1300d / 26 * 2, 1e-9);
    }

    /// <summary>Legacy line 2364: no overlapping basic-pay record => 0/0/0.</summary>
    [Fact]
    public async Task PeriodWise_Should_Return_Zero_When_There_Is_No_BasicPay_Record()
    {
        var store = new FakePayrollRegisterStore();
        SeedPaidLeaveRates(store);
        store.Lines.Add(new PayrollRegisterLine { Id = 20, WageTypeId = 410, WageCode = "4010", Number = 2.5 });

        var result = await Service(store).GetAnnualLeaveProvisionAsync(Context(Wage(31, "3001"), periodWise: true));

        result.Rate.ShouldBe(0);
        result.Number.ShouldBe(0);
        result.Amount.ShouldBe(0);
    }

    #endregion

    #region << GetProvisionChangeAnnualLeave (legacy 2531) >>

    /// <summary>Legacy lines 2589-2596, including the hardcoded divisor 26 and the 6-digit rounding.</summary>
    [Fact]
    public async Task GetProvisionChangeAnnualLeave_Should_Revalue_The_Previous_Closing_At_The_New_Per_Day_Rate()
    {
        var store = new FakePayrollRegisterStore();

        var prevRegister = new SalaryRegister
        {
            Id = 800,
            TenantId = 1,
            EmployeeId = EmployeeId,
            ForPeriod = new DateTime(2025, 2, 28),
            InPeriod = new DateTime(2025, 2, 28),
            PayrollRunType = PayrollRunType.Regular
        };

        // The fake store is register-agnostic; the "3004" line it returns stands in for the previous month's.
        store.Lines.Add(new PayrollRegisterLine { Id = 90, WageTypeId = 34, WageCode = "3004", Number = 40, Amount = 1846.153846 });

        var service = Service(store, registers: new[] { prevRegister });

        var difference = await service.GetProvisionChangeAnnualLeaveAsync(EmployeeId, PayDate, 1400, PayrollRegisterTarget.Live);

        var expected = 40 * Math.Round(1400d / 26, 6) - 1846.153846;
        difference.ShouldBe(expected, 1e-9);
    }

    /// <summary>Legacy line 2562: no previous REGULAR non-retro register => 0.</summary>
    [Fact]
    public async Task GetProvisionChangeAnnualLeave_Should_Return_Zero_When_There_Is_No_Previous_Regular_Register()
    {
        var store = new FakePayrollRegisterStore();
        store.Lines.Add(new PayrollRegisterLine { Id = 90, WageTypeId = 34, WageCode = "3004", Number = 40, Amount = 100 });

        // ForPeriod != InPeriod => a RETRO register, which legacy's filter excludes.
        var retro = new SalaryRegister
        {
            Id = 800,
            TenantId = 1,
            EmployeeId = EmployeeId,
            ForPeriod = new DateTime(2025, 2, 28),
            InPeriod = PayDate,
            PayrollRunType = PayrollRunType.Regular
        };

        var difference = await Service(store, registers: new[] { retro })
            .GetProvisionChangeAnnualLeaveAsync(EmployeeId, PayDate, 1400, PayrollRegisterTarget.Live);

        difference.ShouldBe(0);
    }

    /// <summary>Legacy line 2590: a zero new salary leaves the per-day rate at 0, so the difference is minus the old amount.</summary>
    [Fact]
    public async Task GetProvisionChangeAnnualLeave_Should_Treat_A_Zero_Salary_As_A_Zero_Per_Day_Rate()
    {
        var store = new FakePayrollRegisterStore();
        store.Lines.Add(new PayrollRegisterLine { Id = 90, WageTypeId = 34, WageCode = "3004", Number = 40, Amount = 500 });

        var prevRegister = new SalaryRegister
        {
            Id = 800, TenantId = 1, EmployeeId = EmployeeId,
            ForPeriod = new DateTime(2025, 2, 28), InPeriod = new DateTime(2025, 2, 28),
            PayrollRunType = PayrollRunType.Regular
        };

        var difference = await Service(store, registers: new[] { prevRegister })
            .GetProvisionChangeAnnualLeaveAsync(EmployeeId, PayDate, 0, PayrollRegisterTarget.Live);

        difference.ShouldBe(-500);
    }

    #endregion

    #region << GetAnnualLeaveProvisionDayWiseEntry (legacy 2422) >>

    /// <summary>Legacy lines 2429-2448: day-wise rows are deleted FIRST, then the no-increment early return.</summary>
    [Fact]
    public async Task DayWiseEntry_Should_Delete_Existing_Rows_Then_Return_Early_When_There_Is_No_Increment()
    {
        var store = new FakePayrollRegisterStore();
        store.Lines.Add(new PayrollRegisterLine { Id = 31, WageTypeId = 31, WageCode = "3001", Rate = 1300, Number = 2.5, Amount = 125 });

        var result = await Service(store).GetAnnualLeaveProvisionDayWiseEntryAsync(Context(Wage(31, "3001")));

        store.DayDetails[31].ShouldBeEmpty();
        result.CreatedDaysDetailCount.ShouldBe(0);
        result.ProvisionChangeAmount.ShouldBe(0);
        store.Lines.Single(l => l.WageCode == "3001").Amount.ShouldBe(125);
    }

    /// <summary>Legacy lines 2451-2452: an increment with no EARLIER basic pay is not a change.</summary>
    [Fact]
    public async Task DayWiseEntry_Should_Return_Early_When_There_Is_No_Earlier_BasicPay()
    {
        var store = new FakePayrollRegisterStore();
        store.Lines.Add(new PayrollRegisterLine { Id = 31, WageTypeId = 31, WageCode = "3001", Rate = 1300, Number = 2.5, Amount = 125 });

        var basicPays = new[]
        {
            new EmployeeBasicPay { Id = 60, TenantId = 1, EmployeeId = EmployeeId, StartDate = new DateTime(2025, 3, 15), EndDate = new DateTime(2030, 12, 31) }
        };

        var result = await Service(store, basicPays: basicPays)
            .GetAnnualLeaveProvisionDayWiseEntryAsync(Context(Wage(31, "3001")));

        result.CreatedDaysDetailCount.ShouldBe(0);
    }

    /// <summary>
    /// Legacy lines 2489-2522: with an increment and a non-zero difference, the difference is added to the
    /// "3001" line's Amount (Rate/Number unchanged) and two day-wise rows are written, ordered by period.
    /// </summary>
    [Fact]
    public async Task DayWiseEntry_Should_Add_The_Difference_To_The_3001_Line_And_Write_Two_DayWise_Rows()
    {
        var store = new FakePayrollRegisterStore();
        store.Lines.Add(new PayrollRegisterLine { Id = 31, WageTypeId = 31, WageCode = "3001", Rate = 1400, Number = 2.5, Amount = 134.615385 });
        store.Lines.Add(new PayrollRegisterLine { Id = 34, WageTypeId = 34, WageCode = "3004", Number = 40, Amount = 1846.153846 });

        var basicPays = new[]
        {
            new EmployeeBasicPay { Id = 59, TenantId = 1, EmployeeId = EmployeeId, StartDate = new DateTime(2024, 1, 1), EndDate = new DateTime(2025, 3, 14) },
            new EmployeeBasicPay { Id = 60, TenantId = 1, EmployeeId = EmployeeId, StartDate = new DateTime(2025, 3, 15), EndDate = new DateTime(2030, 12, 31) }
        };

        var prevRegister = new SalaryRegister
        {
            Id = 800, TenantId = 1, EmployeeId = EmployeeId,
            ForPeriod = new DateTime(2025, 2, 28), InPeriod = new DateTime(2025, 2, 28),
            PayrollRunType = PayrollRunType.Regular
        };

        var expectedDifference = 40 * Math.Round(1400d / 26, 6) - 1846.153846;
        expectedDifference.ShouldNotBe(0);

        var result = await Service(store, registers: new[] { prevRegister }, basicPays: basicPays)
            .GetAnnualLeaveProvisionDayWiseEntryAsync(Context(Wage(31, "3001")));

        result.ProvisionChangeAmount.ShouldBe(expectedDifference, 1e-9);
        result.CreatedDaysDetailCount.ShouldBe(2);

        var line = store.Lines.Single(l => l.WageCode == "3001");
        line.Rate.ShouldBe(1400);
        line.Number.ShouldBe(2.5);
        line.Amount.ShouldBe(134.615385 + expectedDifference, 1e-9);

        var days = store.DayDetails[31];
        days.Count.ShouldBe(2);
        days[0].Period.ShouldBe(new DateTime(2025, 2, 28));   // previous month first
        days[0].Amount.ShouldBe(expectedDifference, 1e-9);
        days[1].Period.ShouldBe(PayDate);
        days[1].Amount.ShouldBe(134.615385);
    }

    #endregion
}
