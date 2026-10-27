using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.PersonnelAdministration;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Services;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TimeManagement;

/// <summary>
/// Money-affecting coverage for the two once-per-month wave-T4 ports:
/// <c>CalcMonthlyToleranceDeductionAsync</c> (legacy <c>Program.CalcMonthlyToleranceDeduction</c>, lines
/// 2089-2141) and <c>CalculateAnnualLeaveAdjustAsync</c> (legacy <c>Program.CalculateAnnualLeaveAdjust</c>,
/// lines 2398-2433). Both stamp a value onto exactly ONE <see cref="TimeRegister"/> row of the month that
/// payroll subsequently deducts or pays against.
/// </summary>
// ReSharper disable once InconsistentNaming
public class TimeEngineService_MonthlyAdjustments_Tests
{
    private const string CalcMonthlyTolerance = "CalcMonthlyToleranceDeductionAsync";
    private const string CalcAnnualLeaveAdjust = "CalculateAnnualLeaveAdjustAsync";

    private const int EmployeeId = 300;
    private const int TimePolicyId = 200;
    private const int AnnualLeaveTypeId = 55;

    private static readonly DateTime MonthStart = new DateTime(2024, 6, 1);
    private static readonly DateTime MonthEnd = new DateTime(2024, 6, 30);

    private readonly TimeEngineServiceTestHarness _harness = new TimeEngineServiceTestHarness();

    private readonly Employee _employee = new Employee
    {
        Id = EmployeeId,
        TenantId = 1,
        FirstNameEn = "Test",
        LastNameEn = "Employee",
        FirstNameAr = "Test",
        LastNameAr = "Employee",
        FullNameEn = "Test Employee",
        FullNameAr = "Test Employee",
        JoiningDate = new DateTime(2020, 1, 1)
    };

    private int _nextRegisterId = 1;

    #region << Builders >>

    private TimeRegister Register(
        int dayOfMonth,
        double deductionLateIn = 0,
        double deductionEarlyOut = 0,
        double annualLeaveDay = 0,
        double publicHoliday = 0,
        double dedAfterMonthlyTol = 0)
    {
        return _harness.Add(new TimeRegister
        {
            TenantId = 1,
            EmployeeId = EmployeeId,
            DayDate = new DateTime(MonthStart.Year, MonthStart.Month, dayOfMonth),
            DeductionLateIn = deductionLateIn,
            DeductionEarlyOut = deductionEarlyOut,
            AnnualLeaveDay = annualLeaveDay,
            PublicHoliday = publicHoliday,
            DedAfterMonthlyTol = dedAfterMonthlyTol
        }, _nextRegisterId++);
    }

    private TimePolicy Policy(
        MonthlyToleranceType toleranceType,
        double monthlyAfterTolerance,
        bool calcLateInAfterTolerance = false,
        double lateInTolerance = 0,
        bool calcEarlyOutAfterTolerance = false,
        double earlyOutTolerance = 0)
    {
        return _harness.Add(new TimePolicy
        {
            TenantId = 1,
            NameEn = "Policy",
            NameAr = "Policy",
            MonthlyToleranceType = toleranceType,
            MonthlyAfterTolerance = monthlyAfterTolerance,
            CalcLateInAfterTolerance = calcLateInAfterTolerance,
            LateInTolerance = lateInTolerance,
            CalcEarlyOutAfterTolerance = calcEarlyOutAfterTolerance,
            EarlyOutTolerance = earlyOutTolerance
        }, TimePolicyId);
    }

    private static EmployeeTimePolicy EmployeePolicy(DateTime start, DateTime end)
    {
        return new EmployeeTimePolicy
        {
            TenantId = 1,
            EmployeeId = EmployeeId,
            TimePolicyId = TimePolicyId,
            StartDate = start,
            EndDate = end
        };
    }

    private TimeEngineEmployeeContext EmployeeContext(
        IEnumerable<EmployeeTimePolicy> policies = null,
        IEnumerable<EmployeeAbsence> absences = null)
    {
        return new TimeEngineEmployeeContext
        {
            Employee = _employee,
            StartDate = MonthStart,
            EndDate = MonthEnd,
            TimePolicies = (policies ?? Enumerable.Empty<EmployeeTimePolicy>()).ToList(),
            Absences = (absences ?? Enumerable.Empty<EmployeeAbsence>()).ToList()
        };
    }

    private Task RunToleranceAsync(TimeEngineEmployeeContext context)
    {
        return _harness.InvokeAsync(CalcMonthlyTolerance, _employee, context, MonthStart, MonthEnd);
    }

    private Task RunAnnualLeaveAdjustAsync(TimeEngineEmployeeContext context)
    {
        var masterData = new TimeEngineMasterData { AnnualLeaveTypeId = AnnualLeaveTypeId };
        return _harness.InvokeAsync(
            CalcAnnualLeaveAdjust, _employee, context, masterData, MonthStart, MonthEnd);
    }

    #endregion

    #region << Monthly tolerance deduction >>

    [Fact]
    public async Task Tolerance_Should_Do_Nothing_When_Employee_Has_No_Time_Policy_In_The_Month()
    {
        Policy(MonthlyToleranceType.CalcMonthlyTolearnceOnDed, 40);
        var register = Register(3, deductionLateIn: 90);

        await RunToleranceAsync(EmployeeContext());

        register.DedAfterMonthlyTol.ShouldBe(0);
    }

    [Fact]
    public async Task Tolerance_Should_Do_Nothing_When_The_Policy_Has_No_Monthly_Tolerance_Type()
    {
        Policy(MonthlyToleranceType.NotSelected, 40);
        var register = Register(3, deductionLateIn: 90);

        await RunToleranceAsync(EmployeeContext(new[] { EmployeePolicy(MonthStart, MonthEnd) }));

        register.DedAfterMonthlyTol.ShouldBe(0);
    }

    [Fact]
    public async Task Tolerance_On_Deduction_Should_Stamp_The_Excess_Over_The_Monthly_Allowance()
    {
        Policy(MonthlyToleranceType.CalcMonthlyTolearnceOnDed, monthlyAfterTolerance: 40);

        // Post-tolerance deductions totalling 60 minutes: 30 + 20 late-in, 10 early-out.
        Register(3, deductionLateIn: 30);
        Register(4, deductionLateIn: 20);
        Register(5, deductionEarlyOut: 10);

        await RunToleranceAsync(EmployeeContext(new[] { EmployeePolicy(MonthStart, MonthEnd) }));

        // 60 - 40 = 20, written in MINUTES onto exactly one row.
        _harness.Store<TimeRegister>().Sum(r => r.DedAfterMonthlyTol).ShouldBe(20);
        _harness.Store<TimeRegister>().Count(r => r.DedAfterMonthlyTol > 0).ShouldBe(1);
    }

    [Fact]
    public async Task Tolerance_On_Deduction_Should_Stamp_Zero_When_Under_The_Monthly_Allowance()
    {
        Policy(MonthlyToleranceType.CalcMonthlyTolearnceOnDed, monthlyAfterTolerance: 100);

        Register(3, deductionLateIn: 30);
        Register(4, deductionEarlyOut: 10);

        await RunToleranceAsync(EmployeeContext(new[] { EmployeePolicy(MonthStart, MonthEnd) }));

        _harness.Store<TimeRegister>().ShouldAllBe(r => r.DedAfterMonthlyTol == 0);
    }

    [Fact]
    public async Task Tolerance_On_Actual_Should_Add_The_Per_Occurrence_Tolerance_Back_On()
    {
        // Legacy reconstructs the ACTUAL late/early minutes by adding the per-occurrence tolerance back on to
        // every deducted occurrence -- and it reads LateInTolerance/EarlyOutTolerance, NOT the Monthly*
        // variants (a legacy quirk the port preserves).
        Policy(
            MonthlyToleranceType.CalcMonthlyToleranceOnActual,
            monthlyAfterTolerance: 40,
            calcLateInAfterTolerance: true,
            lateInTolerance: 5,
            calcEarlyOutAfterTolerance: true,
            earlyOutTolerance: 3);

        Register(3, deductionLateIn: 30);   // -> 35
        Register(4, deductionLateIn: 20);   // -> 25
        Register(5, deductionEarlyOut: 10); // -> 13
        Register(6);                        // zero row: contributes nothing, no tolerance added back

        await RunToleranceAsync(EmployeeContext(new[] { EmployeePolicy(MonthStart, MonthEnd) }));

        // (35 + 25) + 13 = 73; 73 - 40 = 33.
        _harness.Store<TimeRegister>().Sum(r => r.DedAfterMonthlyTol).ShouldBe(33);
    }

    [Fact]
    public async Task Tolerance_On_Actual_Should_Not_Add_Tolerance_Back_When_The_Policy_Flag_Is_Off()
    {
        Policy(
            MonthlyToleranceType.CalcMonthlyToleranceOnActual,
            monthlyAfterTolerance: 40,
            calcLateInAfterTolerance: false,
            lateInTolerance: 5,
            calcEarlyOutAfterTolerance: false,
            earlyOutTolerance: 3);

        Register(3, deductionLateIn: 30);
        Register(4, deductionLateIn: 20);
        Register(5, deductionEarlyOut: 10);

        await RunToleranceAsync(EmployeeContext(new[] { EmployeePolicy(MonthStart, MonthEnd) }));

        // 60 - 40 = 20, i.e. identical to the OnDed result once the tolerances are switched off.
        _harness.Store<TimeRegister>().Sum(r => r.DedAfterMonthlyTol).ShouldBe(20);
    }

    [Fact]
    public async Task Tolerance_Should_Reuse_The_Row_That_Already_Carries_The_Largest_Deduction()
    {
        // Legacy picks OrderByDescending(DedAfterMonthlyTol).FirstOrDefault(), which on a re-run means the
        // row it stamped last time. Pinned so a "tidier" ordering change cannot silently move money to a
        // different day (and thus, potentially, a different payroll period).
        Policy(MonthlyToleranceType.CalcMonthlyTolearnceOnDed, monthlyAfterTolerance: 40);

        Register(3, deductionLateIn: 30);
        var previouslyStamped = Register(4, deductionLateIn: 20, dedAfterMonthlyTol: 99);
        Register(5, deductionEarlyOut: 10);

        await RunToleranceAsync(EmployeeContext(new[] { EmployeePolicy(MonthStart, MonthEnd) }));

        previouslyStamped.DedAfterMonthlyTol.ShouldBe(20);
        _harness.Store<TimeRegister>().Count(r => r.DedAfterMonthlyTol > 0).ShouldBe(1);
    }

    [Fact]
    public async Task Tolerance_Should_Ignore_Rows_Belonging_To_Another_Employee()
    {
        Policy(MonthlyToleranceType.CalcMonthlyTolearnceOnDed, monthlyAfterTolerance: 0);

        Register(3, deductionLateIn: 30);
        var otherEmployeeRow = _harness.Add(new TimeRegister
        {
            TenantId = 1,
            EmployeeId = EmployeeId + 1,
            DayDate = new DateTime(2024, 6, 4),
            DeductionLateIn = 500
        }, 90);

        await RunToleranceAsync(EmployeeContext(new[] { EmployeePolicy(MonthStart, MonthEnd) }));

        otherEmployeeRow.DedAfterMonthlyTol.ShouldBe(0);
        _harness.Store<TimeRegister>().Sum(r => r.DedAfterMonthlyTol).ShouldBe(30);
    }

    [Fact]
    public async Task Tolerance_Should_Match_A_Policy_Covering_Only_The_End_Of_The_Month()
    {
        // Legacy accepts a policy spanning EITHER month boundary.
        Policy(MonthlyToleranceType.CalcMonthlyTolearnceOnDed, monthlyAfterTolerance: 10);
        Register(20, deductionLateIn: 30);

        await RunToleranceAsync(
            EmployeeContext(new[] { EmployeePolicy(MonthEnd, MonthEnd.AddMonths(6)) }));

        _harness.Store<TimeRegister>().Sum(r => r.DedAfterMonthlyTol).ShouldBe(20);
    }

    #endregion

    #region << Full-month annual-leave adjustment >>

    private EmployeeAbsence AnnualLeave(DateTime start, DateTime end, int absenceTypeId = AnnualLeaveTypeId)
    {
        return new EmployeeAbsence
        {
            TenantId = 1,
            EmployeeId = EmployeeId,
            AbsenceTypeId = absenceTypeId,
            StartDate = start,
            EndDate = end
        };
    }

    [Fact]
    public async Task LeaveAdjust_Should_Do_Nothing_Without_A_Month_Spanning_Annual_Leave()
    {
        var register = Register(3, annualLeaveDay: 1);

        await RunAnnualLeaveAdjustAsync(EmployeeContext());

        register.LeaveAdjDay.ShouldBe(0);
    }

    [Fact]
    public async Task LeaveAdjust_Should_Do_Nothing_For_A_Mid_Month_Annual_Leave()
    {
        // Starts after the 1st: legacy requires StartDate <= monthStart AND EndDate >= monthEnd.
        var absence = AnnualLeave(MonthStart.AddDays(5), MonthEnd);
        var register = Register(6, annualLeaveDay: 1);

        await RunAnnualLeaveAdjustAsync(EmployeeContext(absences: new[] { absence }));

        register.LeaveAdjDay.ShouldBe(0);
    }

    [Fact]
    public async Task LeaveAdjust_Should_Ignore_An_Absence_Of_Another_Type()
    {
        var absence = AnnualLeave(MonthStart.AddDays(-5), MonthEnd.AddDays(5),
            absenceTypeId: AnnualLeaveTypeId + 1);
        var register = Register(3, annualLeaveDay: 1);

        await RunAnnualLeaveAdjustAsync(EmployeeContext(absences: new[] { absence }));

        register.LeaveAdjDay.ShouldBe(0);
    }

    [Fact]
    public async Task LeaveAdjust_Should_Stamp_The_Shortfall_To_Twenty_Six_Days_On_The_First_Leave_Day()
    {
        var absence = AnnualLeave(MonthStart.AddDays(-5), MonthEnd.AddDays(5));

        // 20 annual-leave days and 4 public holidays posted for the month.
        var first = Register(2, annualLeaveDay: 1);
        for (var day = 3; day <= 21; day++)
        {
            Register(day, annualLeaveDay: 1);
        }

        for (var day = 22; day <= 25; day++)
        {
            Register(day, publicHoliday: 1);
        }

        await RunAnnualLeaveAdjustAsync(EmployeeContext(absences: new[] { absence }));

        // 26 - 20 - 4 = 2, on the FIRST annual-leave row only.
        first.LeaveAdjDay.ShouldBe(2);
        _harness.Store<TimeRegister>().Count(r => r.LeaveAdjDay != 0).ShouldBe(1);
    }

    [Fact]
    public async Task LeaveAdjust_Should_Stamp_A_Negative_Value_When_The_Month_Exceeds_Twenty_Six_Days()
    {
        // Legacy applies no floor: a 30-day leave month yields -4. Pinned deliberately -- clamping this at
        // zero would change pay.
        var absence = AnnualLeave(MonthStart, MonthEnd);

        var first = Register(1, annualLeaveDay: 1);
        for (var day = 2; day <= 30; day++)
        {
            Register(day, annualLeaveDay: 1);
        }

        await RunAnnualLeaveAdjustAsync(EmployeeContext(absences: new[] { absence }));

        first.LeaveAdjDay.ShouldBe(-4);
    }

    [Fact]
    public async Task LeaveAdjust_Should_Do_Nothing_When_No_Day_Was_Posted_As_Annual_Leave()
    {
        var absence = AnnualLeave(MonthStart, MonthEnd);
        var register = Register(3, publicHoliday: 1);

        await RunAnnualLeaveAdjustAsync(EmployeeContext(absences: new[] { absence }));

        register.LeaveAdjDay.ShouldBe(0);
    }

    [Fact]
    public async Task LeaveAdjust_Should_Ignore_Rows_Outside_The_Month_And_Other_Employees()
    {
        var absence = AnnualLeave(MonthStart, MonthEnd);

        var first = Register(1, annualLeaveDay: 1);
        Register(2, annualLeaveDay: 1);

        // Next month, same employee.
        _harness.Add(new TimeRegister
        {
            TenantId = 1,
            EmployeeId = EmployeeId,
            DayDate = MonthEnd.AddDays(1),
            AnnualLeaveDay = 1
        }, 80);

        // Same month, different employee.
        _harness.Add(new TimeRegister
        {
            TenantId = 1,
            EmployeeId = EmployeeId + 1,
            DayDate = new DateTime(2024, 6, 3),
            AnnualLeaveDay = 1
        }, 81);

        await RunAnnualLeaveAdjustAsync(EmployeeContext(absences: new[] { absence }));

        // Only the two in-month rows for THIS employee count: 26 - 2 - 0 = 24.
        first.LeaveAdjDay.ShouldBe(24);
    }

    #endregion
}
