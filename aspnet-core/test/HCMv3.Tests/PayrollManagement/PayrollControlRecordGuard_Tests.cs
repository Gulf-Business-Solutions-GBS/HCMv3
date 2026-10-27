using System;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// Wave P1 -- the <see cref="PayrollControlRecord"/> run guards, ported from legacy
/// <c>HCM10.PayrollEngine/Program.Main</c> (lines 266-276, 280-347 and 840-849).
///
/// <para>These are the payroll engine's financial-integrity controls: they are what stops a run writing into
/// a locked/closed period, and what stops an off-cycle run silently skipping a whole month of payroll. Every
/// assertion below is a legacy behaviour, not a design preference -- if one of them starts failing, the port
/// has diverged and real money is at stake. Do NOT "fix" a test here to match new code.</para>
///
/// <para><see cref="PayrollControlRecordGuard"/> is deliberately pure (no repositories, no DI), so these
/// tests need no database and no container.</para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class PayrollControlRecordGuard_Tests
{
    private static PayrollControlRecord Record(
        DateTime calendarMonthEnd,
        bool periodLocked = false,
        bool periodClosed = false)
    {
        return new PayrollControlRecord
        {
            TenantId = 1,
            PayrollAreaId = 1,
            PayrollArea = new PayrollArea { TenantId = 1, Code = "PA1", Name = "Main Area" },
            CalendarMonthStart = new DateTime(calendarMonthEnd.Year, calendarMonthEnd.Month, 1),
            CalendarMonthEnd = calendarMonthEnd,
            PayrollMonth = calendarMonthEnd.Month,
            PayrollYear = calendarMonthEnd.Year,
            PeriodLocked = periodLocked,
            PeriodClosed = periodClosed
        };
    }

    #region << MonthEnd / NextPeriodPayInDate >>

    [Theory]
    [InlineData(2025, 1, 15, 2025, 1, 31)]
    [InlineData(2025, 2, 1, 2025, 2, 28)]
    [InlineData(2024, 2, 10, 2024, 2, 29)] // leap year
    [InlineData(2025, 12, 31, 2025, 12, 31)]
    public void MonthEnd_Should_Return_The_Last_Day_Of_The_Month_At_Midnight(
        int y, int m, int d, int ey, int em, int ed)
    {
        PayrollControlRecordGuard.MonthEnd(new DateTime(y, m, d, 13, 45, 12))
            .ShouldBe(new DateTime(ey, em, ed, 0, 0, 0));
    }

    [Fact]
    public void NextPeriodPayInDate_Should_Roll_Into_The_Following_Month()
    {
        PayrollControlRecordGuard.NextPeriodPayInDate(Record(new DateTime(2025, 1, 31)))
            .ShouldBe(new DateTime(2025, 2, 28));
    }

    [Fact]
    public void NextPeriodPayInDate_Should_Roll_Across_A_Year_Boundary()
    {
        PayrollControlRecordGuard.NextPeriodPayInDate(Record(new DateTime(2025, 12, 31)))
            .ShouldBe(new DateTime(2026, 1, 31));
    }

    #endregion

    #region << Regular run: PeriodLocked is mandatory (legacy Program.cs line 266) >>

    [Fact]
    public void Regular_Run_Should_Be_Allowed_When_The_Period_Is_Locked_And_Current()
    {
        var record = Record(new DateTime(2025, 1, 31), periodLocked: true);

        Should.NotThrow(() => PayrollControlRecordGuard.EnsureRegularRunAllowed(record, new DateTime(2025, 1, 31)));
    }

    [Fact]
    public void Regular_Run_Should_Be_Blocked_When_The_Period_Is_Not_Locked()
    {
        // THE control that stops payroll running against an unlocked period. Legacy line 266.
        var record = Record(new DateTime(2025, 1, 31), periodLocked: false);

        var ex = Should.Throw<UserFriendlyException>(
            () => PayrollControlRecordGuard.EnsureRegularRunAllowed(record, new DateTime(2025, 1, 31)));

        ex.Message.ShouldBe("Payroll Control Record not locked or is not current.");
    }

    [Fact]
    public void Regular_Run_Should_Be_Blocked_When_The_Control_Record_Is_Not_On_The_Run_Period()
    {
        // Locked, but pointing at December while the run is for January -> still blocked.
        var record = Record(new DateTime(2024, 12, 31), periodLocked: true);

        Should.Throw<UserFriendlyException>(
            () => PayrollControlRecordGuard.EnsureRegularRunAllowed(record, new DateTime(2025, 1, 31)));
    }

    [Fact]
    public void Regular_Run_Should_Ignore_The_Time_Component_On_Both_Dates()
    {
        // Legacy compares .Date == .Date (line 266).
        var record = Record(new DateTime(2025, 1, 31, 23, 59, 59), periodLocked: true);

        Should.NotThrow(() => PayrollControlRecordGuard.EnsureRegularRunAllowed(
            record, new DateTime(2025, 1, 31, 6, 0, 0)));
    }

    #endregion

    #region << OffCycle run: PeriodClosed advances payInDate (legacy Program.cs lines 294-296) >>

    [Fact]
    public void OffCycle_With_TimeEngineAutorun_Should_Advance_PayInDate_To_The_Next_Period_When_Closed()
    {
        // Legacy lines 294-296: the off-cycle is booked into the month AFTER the closed one.
        var record = Record(new DateTime(2025, 1, 31), periodClosed: true);

        var resolved = PayrollControlRecordGuard.ResolveOffCyclePayInDate(
            record,
            payInDate: new DateTime(2025, 1, 31),
            offCyclePaymentDate: new DateTime(2025, 2, 5),
            employeeCodes: "E001",
            enableTimeEngineAutorun: true);

        resolved.ShouldBe(new DateTime(2025, 2, 28));
    }

    [Fact]
    public void OffCycle_With_TimeEngineAutorun_Should_Ignore_The_OffCyclePaymentDate()
    {
        // Legacy's autorun arm never reads offCyclePaymentDate -- a payment date three months out still
        // resolves to the period immediately after the closed one.
        var record = Record(new DateTime(2025, 1, 31), periodClosed: true);

        var resolved = PayrollControlRecordGuard.ResolveOffCyclePayInDate(
            record,
            payInDate: new DateTime(2025, 1, 31),
            offCyclePaymentDate: new DateTime(2025, 5, 9),
            employeeCodes: "E001",
            enableTimeEngineAutorun: true);

        resolved.ShouldBe(new DateTime(2025, 2, 28));
    }

    [Fact]
    public void OffCycle_With_TimeEngineAutorun_Should_Be_Blocked_When_The_Period_Is_Not_Closed()
    {
        var record = Record(new DateTime(2025, 1, 31), periodClosed: false);

        var ex = Should.Throw<UserFriendlyException>(() => PayrollControlRecordGuard.ResolveOffCyclePayInDate(
            record, new DateTime(2025, 1, 31), new DateTime(2025, 2, 5), "E001", enableTimeEngineAutorun: true));

        ex.Message.ShouldBe("Payroll Control Record not Closed or is not current.");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void OffCycle_Should_Always_Require_An_Employee_Code(bool enableTimeEngineAutorun)
    {
        // Legacy lines 285-291 (autorun) and 314-320 (manual) -- the same guard, duplicated.
        var record = Record(new DateTime(2025, 1, 31), periodClosed: true);

        var ex = Should.Throw<UserFriendlyException>(() => PayrollControlRecordGuard.ResolveOffCyclePayInDate(
            record, new DateTime(2025, 1, 31), new DateTime(2025, 2, 5), "", enableTimeEngineAutorun));

        ex.Message.ShouldBe("No Employee Code found, Cannot Run OffCycle Payroll Run.");
    }

    #endregion

    #region << OffCycle run without TimeEngineAutorun (legacy Program.cs lines 311-346) >>

    [Fact]
    public void OffCycle_Without_Autorun_Should_Use_The_Month_End_Of_The_Payment_Date()
    {
        // Legacy's own worked example on line 334: payment date 05.12.2017 -> pay-in date 31.12.2017.
        var record = Record(new DateTime(2017, 11, 30), periodClosed: true);

        var resolved = PayrollControlRecordGuard.ResolveOffCyclePayInDate(
            record,
            payInDate: new DateTime(2017, 11, 30),
            offCyclePaymentDate: new DateTime(2017, 12, 5),
            employeeCodes: "E001",
            enableTimeEngineAutorun: false);

        resolved.ShouldBe(new DateTime(2017, 12, 31));
    }

    [Fact]
    public void OffCycle_Without_Autorun_Should_Require_An_OffCycle_Payment_Date()
    {
        var record = Record(new DateTime(2025, 1, 31), periodClosed: true);

        var ex = Should.Throw<UserFriendlyException>(() => PayrollControlRecordGuard.ResolveOffCyclePayInDate(
            record, new DateTime(2025, 1, 31), default(DateTime), "E001", enableTimeEngineAutorun: false));

        ex.Message.ShouldBe("offCycle PaymentDate not entered.");
    }

    [Fact]
    public void OffCycle_Without_Autorun_Should_Be_Blocked_When_The_Period_Is_Not_Closed()
    {
        var record = Record(new DateTime(2025, 1, 31), periodClosed: false);

        Should.Throw<UserFriendlyException>(() => PayrollControlRecordGuard.ResolveOffCyclePayInDate(
            record, new DateTime(2025, 1, 31), new DateTime(2025, 2, 5), "E001", enableTimeEngineAutorun: false));
    }

    [Fact]
    public void OffCycle_Without_Autorun_Should_Be_Blocked_When_The_Payment_Date_Is_Inside_The_Control_Period()
    {
        // Legacy requires CalendarMonthEnd < offCyclePaymentDate (line 331): paying inside the period the
        // control record still sits on is refused even when that period is closed.
        var record = Record(new DateTime(2025, 1, 31), periodClosed: true);

        Should.Throw<UserFriendlyException>(() => PayrollControlRecordGuard.ResolveOffCyclePayInDate(
            record, new DateTime(2025, 1, 31), new DateTime(2025, 1, 20), "E001", enableTimeEngineAutorun: false));
    }

    #endregion

    #region << Per-employee re-check (legacy Program.cs lines 840-849) >>

    [Fact]
    public void Employee_Check_Should_Pass_For_A_Regular_Run_On_The_Control_Period()
    {
        var record = Record(new DateTime(2025, 1, 31), periodLocked: true);

        Should.NotThrow(() => PayrollControlRecordGuard.EnsureEmployeeControlRecordMatches(
            record, record.PayrollArea, PayrollRunType.Regular, new DateTime(2025, 1, 31)));
    }

    [Fact]
    public void Employee_Check_Should_Throw_When_The_Control_Record_Is_Ahead_Of_The_Run_Date()
    {
        // Legacy line 840-841.
        var record = Record(new DateTime(2025, 2, 28), periodLocked: true);

        var ex = Should.Throw<UserFriendlyException>(() => PayrollControlRecordGuard.EnsureEmployeeControlRecordMatches(
            record, record.PayrollArea, PayrollRunType.Regular, new DateTime(2025, 1, 31)));

        ex.Message.ShouldBe("Payroll Control Record set to run for period later than payroll run date.");
    }

    [Fact]
    public void Employee_Check_Should_Accept_An_OffCycle_Run_On_The_Period_Right_After_The_Control_Record()
    {
        var record = Record(new DateTime(2025, 1, 31), periodClosed: true);

        Should.NotThrow(() => PayrollControlRecordGuard.EnsureEmployeeControlRecordMatches(
            record, record.PayrollArea, PayrollRunType.OffCycle, new DateTime(2025, 2, 28)));
    }

    [Fact]
    public void Employee_Check_Should_Throw_Mismatch_When_An_OffCycle_Run_Would_Skip_A_Month()
    {
        // THE "do not skip a month of payroll" control -- legacy lines 844-849. Control record sits on
        // January; running off-cycle for March would leave February unpaid.
        var record = Record(new DateTime(2025, 1, 31), periodClosed: true);

        var ex = Should.Throw<UserFriendlyException>(() => PayrollControlRecordGuard.EnsureEmployeeControlRecordMatches(
            record, record.PayrollArea, PayrollRunType.OffCycle, new DateTime(2025, 3, 31)));

        ex.Message.ShouldBe("Mismatch between Payroll Control Record and Payroll run date. PayrollArea : Main Area");
    }

    [Fact]
    public void Employee_Check_Should_Throw_Mismatch_When_An_OffCycle_PayInDate_Is_Not_A_Month_End()
    {
        var record = Record(new DateTime(2025, 1, 31), periodClosed: true);

        Should.Throw<UserFriendlyException>(() => PayrollControlRecordGuard.EnsureEmployeeControlRecordMatches(
            record, record.PayrollArea, PayrollRunType.OffCycle, new DateTime(2025, 2, 15)));
    }

    [Fact]
    public void Employee_Check_Should_Not_Apply_The_Mismatch_Rule_To_A_Regular_Run()
    {
        // Legacy gates the second throw on payrollRunType == OffCycle only (line 844). A regular run on a
        // later period passes this particular check -- the Regular arm's own guard is what constrains it.
        var record = Record(new DateTime(2025, 1, 31), periodLocked: true);

        Should.NotThrow(() => PayrollControlRecordGuard.EnsureEmployeeControlRecordMatches(
            record, record.PayrollArea, PayrollRunType.Regular, new DateTime(2025, 3, 31)));
    }

    #endregion
}
