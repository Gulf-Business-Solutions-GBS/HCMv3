using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;
using HCMv3.PersonnelAdministration;
using HCMv3.TimeManagement;
using NSubstitute;
using Shouldly;
using Xunit;
using static HCMv3.Tests.PayrollManagement.PayrollEngineFakes;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// Wave P9 -- <see cref="EmployeeAttendancePayCalculationService"/>, ported from legacy
/// <c>HCM10.PayrollEngine/EmployeeAttendancePayService.cs</c> (<c>Main</c> line 14,
/// <c>GetAttendancePay</c> line 103).
/// </summary>
public class EmployeeAttendancePayCalculationServiceTests
{
    private const int EmployeeId = 21;
    private const int RegisterId = 700;
    private const int WageTypeId = 5;
    private const int BasicWageTypeId = 1;
    private const int AttendanceTypeId = 3;

    private static readonly DateTime PayDate = new DateTime(2025, 4, 30);

    #region << Harness >>

    private sealed class Harness
    {
        public readonly FakePayrollRegisterStore Store = new FakePayrollRegisterStore();
        public readonly List<AttendanceType> AttendanceTypes = new List<AttendanceType>();
        public readonly List<EmployeeAttendance> EmployeeAttendances = new List<EmployeeAttendance>();
        public readonly List<TimeRegister> TimeRegisters = new List<TimeRegister>();
        public readonly List<WageType> WageTypes = new List<WageType>();
        public readonly IPayrollAdvancePaymentService AdvancePaymentService = Substitute.For<IPayrollAdvancePaymentService>();

        public Harness()
        {
            AdvancePaymentService
                .GetRecoveryPeriods(Arg.Any<DateTime>(), Arg.Any<DateTime>())
                .Returns(ci => EmployeeBasicPayCalculationService.GetRecoveryPeriods(
                    ci.ArgAt<DateTime>(0), ci.ArgAt<DateTime>(1)));
        }

        public EmployeeAttendancePayCalculationService Build()
        {
            return new EmployeeAttendancePayCalculationService(
                Repo(AttendanceTypes),
                Repo(EmployeeAttendances),
                Repo(TimeRegisters),
                Repo(WageTypes),
                Store,
                AdvancePaymentService);
        }
    }

    private static WageType AttendanceWageType(double fixedDays = 30)
    {
        return new WageType
        {
            Id = WageTypeId,
            Code = "1200",
            NameEn = "Attendance Pay",
            BillingType = BillingType.Earning,
            FixedDays = fixedDays
        };
    }

    private static PayrollCalculationContext Context(Harness harness, WageType wageType)
    {
        return new PayrollCalculationContext
        {
            EmployeeId = EmployeeId,
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
            TimeRegisterList = harness.TimeRegisters,
            TimeRegisterQuery = harness.TimeRegisters.AsQueryable()
        };
    }

    private static void SeedBasicPayRate(Harness harness, double rate)
    {
        harness.WageTypes.Add(new WageType { Id = BasicWageTypeId, Code = "1000", IncludeInBasicPay = true });
        harness.Store.Lines.Add(new PayrollRegisterLine
        {
            Id = 1,
            WageTypeId = BasicWageTypeId,
            WageCode = "1000",
            Rate = rate
        });
    }

    private static void SeedAttendanceType(Harness harness)
    {
        harness.AttendanceTypes.Add(new AttendanceType
        {
            Id = AttendanceTypeId,
            Code = "A1",
            NameEn = "Site Allowance",
            WageTypeId = WageTypeId
        });
    }

    private static void SeedAttendanceDays(Harness harness, params (DateTime day, double attendanceDay)[] days)
    {
        var id = 100;
        foreach (var (day, attendanceDay) in days)
        {
            harness.TimeRegisters.Add(new TimeRegister
            {
                Id = id++,
                EmployeeId = EmployeeId,
                DayDate = day,
                AttendanceTypeId = AttendanceTypeId,
                AttendanceDay = attendanceDay
            });
        }
    }

    #endregion

    /// <summary>
    /// Legacy lines 118-129: <c>amount = (totalBasicPay / FixedDays) * attendanceDays</c>, and
    /// <c>Rate</c> is the IncludeInBasicPay rate total.
    /// </summary>
    [Fact]
    public async Task Should_Pay_Basic_Pay_Rate_Divided_By_FixedDays_Times_Attendance_Days_On_A_Regular_Run()
    {
        var harness = new Harness();
        var wageType = AttendanceWageType(fixedDays: 30);
        harness.WageTypes.Add(wageType);
        SeedBasicPayRate(harness, 600);
        SeedAttendanceType(harness);
        SeedAttendanceDays(harness,
            (new DateTime(2025, 4, 10), 1),
            (new DateTime(2025, 4, 11), 1),
            (new DateTime(2025, 4, 12), 1));

        await harness.Build().CalculateAsync(Context(harness, wageType));

        var line = harness.Store.Lines.Single(l => l.WageTypeId == WageTypeId);
        line.Rate.ShouldBe(600);
        line.Number.ShouldBe(3);
        line.Amount.ShouldBe(60); // (600 / 30) * 3
    }

    /// <summary>Legacy line 26: the <c>IsNotReturnFromLeave</c> arm is empty, so nothing is written.</summary>
    [Fact]
    public async Task Should_Do_Nothing_When_IsNotReturnFromLeave()
    {
        var harness = new Harness();
        var wageType = AttendanceWageType();
        harness.WageTypes.Add(wageType);
        SeedBasicPayRate(harness, 600);
        SeedAttendanceType(harness);
        SeedAttendanceDays(harness, (new DateTime(2025, 4, 10), 1));

        var context = Context(harness, wageType);
        context.IsNotReturnFromLeave = true;

        await harness.Build().CalculateAsync(context);

        harness.Store.Lines.ShouldNotContain(l => l.WageTypeId == WageTypeId);
    }

    /// <summary>
    /// Legacy line 26 again, contrasted with <see cref="PayrollArrearCalculationService"/>: the attendance
    /// service's copy of the idiom does NOT carry the <c>BillingType != Provision</c> escape, so even a
    /// Provision wage type is skipped here. Pinned because the two legacy files disagree.
    /// </summary>
    [Fact]
    public async Task Should_Skip_Even_A_Provision_Wage_Type_When_IsNotReturnFromLeave()
    {
        var harness = new Harness();
        var wageType = AttendanceWageType();
        wageType.BillingType = BillingType.Provision;
        harness.WageTypes.Add(wageType);
        SeedBasicPayRate(harness, 600);
        SeedAttendanceType(harness);
        SeedAttendanceDays(harness, (new DateTime(2025, 4, 10), 1));

        var context = Context(harness, wageType);
        context.IsNotReturnFromLeave = true;

        await harness.Build().CalculateAsync(context);

        harness.Store.Lines.ShouldNotContain(l => l.WageTypeId == WageTypeId);
    }

    /// <summary>Legacy line 68: zero attendance days writes nothing at all.</summary>
    [Fact]
    public async Task Should_Write_Nothing_When_There_Are_No_Attendance_Days()
    {
        var harness = new Harness();
        var wageType = AttendanceWageType();
        harness.WageTypes.Add(wageType);
        SeedBasicPayRate(harness, 600);
        SeedAttendanceType(harness);

        await harness.Build().CalculateAsync(Context(harness, wageType));

        harness.Store.Lines.ShouldNotContain(l => l.WageTypeId == WageTypeId);
    }

    /// <summary>
    /// Legacy lines 82-83: when the computed amount is 0 the detail is DELETED (not left at zero) -- the
    /// opposite of <see cref="PayrollArrearCalculationService"/>'s convention.
    /// </summary>
    [Fact]
    public async Task Should_Delete_The_Line_When_The_Computed_Amount_Is_Zero()
    {
        var harness = new Harness();
        var wageType = AttendanceWageType();
        harness.WageTypes.Add(wageType);

        // No IncludeInBasicPay rate on the register -> totalBasicPay 0 -> amount 0.
        harness.WageTypes.Add(new WageType { Id = BasicWageTypeId, Code = "1000", IncludeInBasicPay = true });
        SeedAttendanceType(harness);
        SeedAttendanceDays(harness, (new DateTime(2025, 4, 10), 1));

        harness.Store.Lines.Add(new PayrollRegisterLine
        {
            Id = 42,
            WageTypeId = WageTypeId,
            WageCode = "1200",
            Amount = 999
        });

        await harness.Build().CalculateAsync(Context(harness, wageType));

        harness.Store.Lines.ShouldNotContain(l => l.WageTypeId == WageTypeId);
        harness.Store.DeletedLineIds.ShouldContain(42);
    }

    /// <summary>
    /// Legacy line 49 vs line 62: the REGULAR arm filters time-register rows by attendance type. A day
    /// carrying a DIFFERENT attendance type must not be counted.
    /// </summary>
    [Fact]
    public async Task Should_Count_Only_Days_Of_The_Matching_Attendance_Type_On_A_Regular_Run()
    {
        var harness = new Harness();
        var wageType = AttendanceWageType(fixedDays: 30);
        harness.WageTypes.Add(wageType);
        SeedBasicPayRate(harness, 600);
        SeedAttendanceType(harness);
        SeedAttendanceDays(harness, (new DateTime(2025, 4, 10), 1));

        // A day of some other attendance type, inside the window.
        harness.TimeRegisters.Add(new TimeRegister
        {
            Id = 900,
            EmployeeId = EmployeeId,
            DayDate = new DateTime(2025, 4, 11),
            AttendanceTypeId = 999,
            AttendanceDay = 1
        });

        await harness.Build().CalculateAsync(Context(harness, wageType));

        harness.Store.Lines.Single(l => l.WageTypeId == WageTypeId).Number.ShouldBe(1);
    }

    /// <summary>Legacy lines 45-50: days outside the pay window are not counted.</summary>
    [Fact]
    public async Task Should_Ignore_Attendance_Days_Outside_The_Pay_Window()
    {
        var harness = new Harness();
        var wageType = AttendanceWageType(fixedDays: 30);
        harness.WageTypes.Add(wageType);
        SeedBasicPayRate(harness, 600);
        SeedAttendanceType(harness);
        SeedAttendanceDays(harness,
            (new DateTime(2025, 3, 31), 1),
            (new DateTime(2025, 4, 10), 1),
            (new DateTime(2025, 5, 1), 1));

        await harness.Build().CalculateAsync(Context(harness, wageType));

        harness.Store.Lines.Single(l => l.WageTypeId == WageTypeId).Number.ShouldBe(1);
    }

    /// <summary>
    /// <b>Legacy defect, ported as-is</b> (legacy lines 62-64): on an OFF-CYCLE run the attendance-day sum
    /// has NO attendance-type predicate, so days belonging to an unrelated attendance type that fall inside
    /// the matched <c>EmployeeAttendance</c> span ARE counted. Pinned so it is never silently "fixed".
    /// </summary>
    [Fact]
    public async Task Should_Count_Foreign_Attendance_Types_On_An_OffCycle_Run_Legacy_Defect()
    {
        var harness = new Harness();
        var wageType = AttendanceWageType(fixedDays: 30);
        wageType.IncludeInAdvancePay = true;
        harness.WageTypes.Add(wageType);
        SeedBasicPayRate(harness, 600);
        SeedAttendanceType(harness);

        harness.EmployeeAttendances.Add(new EmployeeAttendance
        {
            Id = 1,
            EmployeeId = EmployeeId,
            AttendanceTypeId = AttendanceTypeId,
            AttendanceType = harness.AttendanceTypes[0],
            StartDate = new DateTime(2025, 4, 10),
            EndDate = new DateTime(2025, 4, 12)
        });

        // One matching day and one FOREIGN attendance-type day, both inside the span.
        SeedAttendanceDays(harness, (new DateTime(2025, 4, 10), 1));
        harness.TimeRegisters.Add(new TimeRegister
        {
            Id = 901,
            EmployeeId = EmployeeId,
            DayDate = new DateTime(2025, 4, 11),
            AttendanceTypeId = 999,
            AttendanceDay = 1
        });

        var context = Context(harness, wageType);
        context.PayrollRunType = PayrollRunType.OffCycle;
        context.OffCycleReason = PayrollOffCycleReasons.AdvanceLeave;
        context.Register.PayrollRunType = PayrollRunType.OffCycle;

        await harness.Build().CalculateAsync(context);

        // 2, not 1 -- the foreign day is included. That is legacy's behaviour.
        harness.Store.Lines.Single(l => l.WageTypeId == WageTypeId).Number.ShouldBe(2);
    }

    /// <summary>
    /// Legacy lines 136-138: an off-cycle run whose reason is neither Advance-Leave-with-IncludeInAdvancePay
    /// nor End-Of-Service computes an amount of 0, and the caller then deletes the row.
    /// </summary>
    [Fact]
    public async Task Should_Pay_Nothing_On_An_OffCycle_Run_That_Is_Neither_AdvanceLeave_Nor_EndOfService()
    {
        var harness = new Harness();
        var wageType = AttendanceWageType(fixedDays: 30);
        wageType.IncludeInAdvancePay = false;
        harness.WageTypes.Add(wageType);
        SeedBasicPayRate(harness, 600);
        SeedAttendanceType(harness);

        harness.EmployeeAttendances.Add(new EmployeeAttendance
        {
            Id = 1,
            EmployeeId = EmployeeId,
            AttendanceTypeId = AttendanceTypeId,
            AttendanceType = harness.AttendanceTypes[0],
            StartDate = new DateTime(2025, 4, 10),
            EndDate = new DateTime(2025, 4, 12)
        });
        SeedAttendanceDays(harness, (new DateTime(2025, 4, 10), 1));

        var context = Context(harness, wageType);
        context.PayrollRunType = PayrollRunType.OffCycle;
        context.OffCycleReason = PayrollOffCycleReasons.AdvanceLeave;
        context.Register.PayrollRunType = PayrollRunType.OffCycle;

        await harness.Build().CalculateAsync(context);

        harness.Store.Lines.ShouldNotContain(l => l.WageTypeId == WageTypeId);
    }

    /// <summary>
    /// Legacy line 35: an attendance type mapped to a DIFFERENT wage type must not contribute to this wage
    /// type's line.
    /// </summary>
    [Fact]
    public async Task Should_Ignore_Attendance_Types_Mapped_To_Another_Wage_Type()
    {
        var harness = new Harness();
        var wageType = AttendanceWageType(fixedDays: 30);
        harness.WageTypes.Add(wageType);
        SeedBasicPayRate(harness, 600);

        harness.AttendanceTypes.Add(new AttendanceType
        {
            Id = AttendanceTypeId,
            Code = "A1",
            WageTypeId = 4242 // some other wage type
        });
        SeedAttendanceDays(harness, (new DateTime(2025, 4, 10), 1));

        await harness.Build().CalculateAsync(Context(harness, wageType));

        harness.Store.Lines.ShouldNotContain(l => l.WageTypeId == WageTypeId);
    }
}
