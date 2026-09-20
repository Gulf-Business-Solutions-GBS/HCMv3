using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using HCMv3.EngineRuns;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using HCMv3.SystemSettings;
using HCMv3.SystemSettings.Services;
using HCMv3.TimeManagement;
using HCMv3.Tests.EngineRuns;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// Wave P1b. Behaviour tests for <see cref="PayrollEngineService"/>, concentrated on the four behaviours most
/// likely to regress silently:
/// <list type="number">
/// <item><description>the two category-level on-hold gates the resolver does NOT apply (leave pay, payments)
/// and the INVERTED basic-pay one;</description></item>
/// <item><description><see cref="WageType.PayrollOrder"/> being honoured, and 9000 Net Pay therefore running
/// last;</description></item>
/// <item><description>per-employee error isolation;</description></item>
/// <item><description><see cref="PayrollRegisterTarget"/> being threaded through rather than
/// hardcoded.</description></item>
/// </list>
/// </summary>
public class PayrollEngineServiceTests
{
    private const string PayrollAreaName = "PA";
    private static readonly DateTime PayInDate = new DateTime(2024, 6, 30);

    /// <summary>
    /// Mid-June, so <c>PayrollControlRecordGuard.ResolveOffCyclePayInDate</c> resolves the run onto
    /// <see cref="PayInDate"/> (the month end of the payment date).
    /// </summary>
    private static readonly DateTime OffCyclePaymentDate = new DateTime(2024, 6, 15);

    #region << Ordering >>

    [Fact]
    public async Task RunAsync_Should_Iterate_WageTypes_In_PayrollOrder_With_NetPay_Last()
    {
        var harness = new Harness();

        // Deliberately inserted in a NON-PayrollOrder sequence, and 9000 is not last in the list.
        harness.WageTypes.Add(WageType("9000", "Net Pay", 900, BillingType.Deduction));
        harness.WageTypes.Add(WageType("2000", "Late Early Deduction", 200, BillingType.Deduction));
        harness.WageTypes.Add(WageType("1006", "Loan Pay", 100, BillingType.Earning));
        harness.WageTypes.Add(WageType("0023", "Fund Deduction", 300, BillingType.Deduction));

        harness.CalculatedWageCodes.UnionWith(new[] { "9000", "2000", "1006", "0023" });

        await harness.Build().RunAsync(harness.RegularInput());

        harness.DispatchedWageCodes.ShouldBe(new[] { "1006", "2000", "0023", "9000" });
        harness.DispatchedWageCodes.Last().ShouldBe("9000");
    }

    #endregion

    #region << One run per payroll area >>

    /// <summary>
    /// <b>ONE PAYROLL RUN PER PAYROLL AREA.</b> Two runs for the same area write the same
    /// <c>SalaryRegister</c> rows for the same employees, and the advance-recovery upsert ACCUMULATES — so a
    /// concurrent pair can double a recovery.
    ///
    /// <para><b>Not a port.</b> Legacy had no guard: its Run button truncated the whole <c>PayrollLog</c> table
    /// and queued another thread, so a second run destroyed the first one's progress log and the two runs'
    /// counters were added together. There was nothing to match, so this is new — and it is a GUARD, not a
    /// calculation, so it leaves every figure alone.</para>
    /// </summary>
    [Fact]
    public async Task RunAsync_Should_Refuse_A_Second_Run_For_The_Same_Payroll_Area()
    {
        var harness = new Harness();
        harness.ActiveRunForKey = PayrollEngineService.BuildRunKey(PayrollAreaName);

        var exception = await Should.ThrowAsync<Abp.UI.UserFriendlyException>(
            () => harness.Build().RunAsync(harness.RegularInput()));

        exception.Message.ShouldContain(PayrollAreaName);
        exception.Message.ShouldContain("already running");

        // Refused BEFORE any employee was touched -- no half-written register.
        harness.SalaryRegisters.ShouldBeEmpty();
    }

    /// <summary>
    /// A run in progress for a DIFFERENT payroll area must not block this one. Large tenants run their areas in
    /// parallel, and the two touch different employees and different control records.
    /// </summary>
    [Fact]
    public async Task RunAsync_Should_Allow_A_Run_While_Another_Payroll_Area_Is_Running()
    {
        var harness = new Harness();
        harness.ActiveRunForKey = PayrollEngineService.BuildRunKey("A DIFFERENT AREA");

        await harness.Build().RunAsync(harness.RegularInput());

        // Ran normally.
        harness.PayrollLogs.ShouldNotBeEmpty();
    }

    /// <summary>
    /// The key is normalised, so casing or stray spaces cannot smuggle a second run past the guard for the same
    /// area — "PA", "pa" and " PA " are one area.
    /// </summary>
    [Fact]
    public void BuildRunKey_Should_Normalise_Casing_And_Whitespace()
    {
        PayrollEngineService.BuildRunKey("PA").ShouldBe(PayrollEngineService.BuildRunKey(" pa "));
        PayrollEngineService.BuildRunKey("Payroll Area 1").ShouldBe(PayrollEngineService.BuildRunKey("PAYROLL AREA 1"));
        PayrollEngineService.BuildRunKey("A").ShouldNotBe(PayrollEngineService.BuildRunKey("B"));
        PayrollEngineService.BuildRunKey(" ").ShouldBeNull();
    }

    /// <summary>
    /// The guard <b>fails OPEN</b>: when the progress store cannot be read it returns null and the run proceeds.
    /// A reporting hiccup must never be able to stop payroll — that is strictly worse than the race it guards.
    /// </summary>
    [Fact]
    public async Task RunAsync_Should_Proceed_When_The_Concurrency_Check_Cannot_Be_Read()
    {
        var harness = new Harness();
        harness.ActiveRunForKey = null;          // the writer reports nothing running

        await harness.Build().RunAsync(harness.RegularInput());

        harness.PayrollLogs.ShouldNotBeEmpty();
    }

    /// <summary>
    /// The legacy <c>PayrollLog.StopFlag</c> abort is now SCOPED to this run. Legacy achieved that by truncating
    /// the table before every run; left unscoped, stopping one payroll area would abort a concurrent run of
    /// another. A stop flag from BEFORE this run started must be ignored.
    /// </summary>
    [Fact]
    public async Task RunAsync_Should_Ignore_A_StopFlag_From_An_Earlier_Run()
    {
        var harness = new Harness();

        // An old row left behind by a previous run that someone stopped.
        harness.PayrollLogs.Add(new PayrollLog
        {
            Id = 9001,
            StopFlag = true,
            RunTimeStamp = DateTime.Now.AddDays(-1),
            Message = "Stopped by user, yesterday"
        });

        var result = await harness.Build().RunAsync(harness.RegularInput());

        result.Aborted.ShouldBeFalse();
        result.EmployeeResults.ShouldNotBeEmpty();
    }

    #endregion

    #region << Time Engine autorun: no separate run needed >>

    /// <summary>
    /// <b>THE POINT OF THE AUTORUN: the operator does not run the Time Engine separately.</b> With
    /// <c>EnableTimeEngineAutorun</c> on, the payroll run evaluates time for each employee itself (legacy
    /// <c>Program.cs</c> 554-753), passing the employee's code, the window, the payroll area, and
    /// <c>IsRanFromPayrollEngine</c> — legacy's six console arguments.
    /// </summary>
    [Fact]
    public async Task RunAsync_Should_Run_The_Time_Engine_Per_Employee_When_Autorun_Is_On()
    {
        var harness = new Harness();
        harness.EnableTimeEngineAutorun();
        harness.AddSecondEmployee();

        HCMv3.TimeManagement.Services.TimeEngineRunInput firstCall = null;
        harness.TimeEngineService
            .CreateTimeRegisterAsync(Arg.Do<HCMv3.TimeManagement.Services.TimeEngineRunInput>(i => firstCall ??= i))
            .Returns(_ => Task.FromResult(new HCMv3.TimeManagement.Services.TimeEngineRunResult()));

        await harness.Build().RunAsync(harness.RegularInput());

        // Once per employee, not once per run.
        await harness.TimeEngineService.Received(2)
            .CreateTimeRegisterAsync(Arg.Any<HCMv3.TimeManagement.Services.TimeEngineRunInput>());

        firstCall.ShouldNotBeNull();
        firstCall.EmployeeCodes.ShouldBe("E1");
        firstCall.PayrollAreaName.ShouldBe(PayrollAreaName);
        firstCall.IsRanFromPayrollEngine.ShouldBeTrue();

        // Legacy lines 566-567: the run month, first day to last.
        firstCall.StartDate.ShouldBe(new DateTime(2024, 6, 1));
        firstCall.EndDate.ShouldBe(new DateTime(2024, 6, 30));
    }

    /// <summary>
    /// With autorun OFF the payroll run must not touch the Time Engine — the operator runs it themselves, and
    /// the engine's existing "Time Engine not run for Employee" guard is what protects the figures.
    /// </summary>
    [Fact]
    public async Task RunAsync_Should_Not_Run_The_Time_Engine_When_Autorun_Is_Off()
    {
        var harness = new Harness();

        await harness.Build().RunAsync(harness.RegularInput());

        await harness.TimeEngineService.DidNotReceive()
            .CreateTimeRegisterAsync(Arg.Any<HCMv3.TimeManagement.Services.TimeEngineRunInput>());
    }

    /// <summary>
    /// Legacy lines 570-572: the window RESUMES from <c>PayrollStatus.NextTimeEvaluationFrom</c> rather than
    /// re-evaluating the whole month, so a payroll re-run does not redo work already done.
    /// </summary>
    [Fact]
    public async Task RunAsync_Should_Resume_The_Time_Window_From_NextTimeEvaluationFrom()
    {
        var harness = new Harness();
        harness.EnableTimeEngineAutorun();
        harness.PayrollStatuses.Single().NextTimeEvaluationFrom = new DateTime(2024, 6, 18);

        HCMv3.TimeManagement.Services.TimeEngineRunInput call = null;
        harness.TimeEngineService
            .CreateTimeRegisterAsync(Arg.Do<HCMv3.TimeManagement.Services.TimeEngineRunInput>(i => call = i))
            .Returns(_ => Task.FromResult(new HCMv3.TimeManagement.Services.TimeEngineRunResult()));

        await harness.Build().RunAsync(harness.RegularInput());

        call.StartDate.ShouldBe(new DateTime(2024, 6, 18));
        call.EndDate.ShouldBe(new DateTime(2024, 6, 30));
    }

    /// <summary>
    /// Legacy lines 575-585: on a REGULAR run a leave paid in full WITH this payroll extends the window past the
    /// month end to the leave's end date, so the spilled months are evaluated before payroll reads them.
    /// </summary>
    [Fact]
    public async Task RunAsync_Should_Extend_The_Time_Window_To_A_PayFullWithPayroll_Leave_End()
    {
        var harness = new Harness();
        harness.EnableTimeEngineAutorun();

        var absenceType = new AbsenceType { Id = 91, Code = "1", NameEn = "Annual Leave", IsPaid = true, IsActive = true };
        harness.Absences.Add(new EmployeeAbsence
        {
            Id = 91,
            EmployeeId = Harness.EmployeeId,
            AbsenceTypeId = absenceType.Id,
            AbsenceType = absenceType,
            StartDate = new DateTime(2024, 6, 20),
            EndDate = new DateTime(2024, 7, 25),
            LeavePaymentType = LeavePaymentType.PayFullWithPayroll,
            PaymentDate = PayInDate
        });

        HCMv3.TimeManagement.Services.TimeEngineRunInput call = null;
        harness.TimeEngineService
            .CreateTimeRegisterAsync(Arg.Do<HCMv3.TimeManagement.Services.TimeEngineRunInput>(i => call = i))
            .Returns(_ => Task.FromResult(new HCMv3.TimeManagement.Services.TimeEngineRunResult()));

        await harness.Build().RunAsync(harness.RegularInput());

        call.EndDate.ShouldBe(new DateTime(2024, 7, 25));       // past the month end, to the leave's end
    }

    /// <summary>
    /// Legacy lines 600-613: on an Advance Leave OFF-CYCLE the window extends to the advance leave's end date —
    /// exactly the window the off-cycle's own recovery periods will later span.
    /// </summary>
    [Fact]
    public async Task RunAsync_Should_Extend_The_Time_Window_To_The_Advance_Leave_End_On_OffCycle()
    {
        var harness = new Harness();
        harness.EnableTimeEngineAutorun();
        var input = harness.OffCycleAdvanceLeaveInput();

        HCMv3.TimeManagement.Services.TimeEngineRunInput call = null;
        harness.TimeEngineService
            .CreateTimeRegisterAsync(Arg.Do<HCMv3.TimeManagement.Services.TimeEngineRunInput>(i => call = i))
            .Returns(_ => Task.FromResult(new HCMv3.TimeManagement.Services.TimeEngineRunResult()));

        await harness.Build().RunAsync(input);

        call.ShouldNotBeNull();
        call.EndDate.ShouldBe(new DateTime(2024, 7, 10));       // the absence's EndDate
    }

    /// <summary>
    /// Legacy line 693: an employee already evaluated past the window end is skipped, not re-run — the guard that
    /// keeps a payroll re-run cheap.
    /// </summary>
    [Fact]
    public async Task RunAsync_Should_Skip_The_Time_Engine_When_The_Window_Is_Already_Covered()
    {
        var harness = new Harness();
        harness.EnableTimeEngineAutorun();
        harness.PayrollStatuses.Single().NextTimeEvaluationFrom = new DateTime(2024, 8, 1);   // past 30/06

        await harness.Build().RunAsync(harness.RegularInput());

        await harness.TimeEngineService.DidNotReceive()
            .CreateTimeRegisterAsync(Arg.Any<HCMv3.TimeManagement.Services.TimeEngineRunInput>());
    }

    /// <summary>
    /// Legacy lines 717-720 plus its <c>catch { log; continue; }</c> at 725-751: a failed <c>TimeLog</c> row fails
    /// THAT employee and leaves the rest of the run alone. The failure is reported as legacy words it.
    ///
    /// <para>Legacy scoped this check by truncating the whole <c>TimeLog</c> table first; this port filters on
    /// <c>RunTimeStamp</c> instead — the one deliberate deviation, in mechanism only.</para>
    /// </summary>
    [Fact]
    public async Task RunAsync_Should_Fail_Only_That_Employee_When_The_Time_Engine_Logs_A_Failure()
    {
        var harness = new Harness();
        harness.EnableTimeEngineAutorun();
        harness.AddSecondEmployee();

        // The first employee's evaluation writes a failed TimeLog row; the second's does not.
        harness.TimeEngineService
            .CreateTimeRegisterAsync(Arg.Any<HCMv3.TimeManagement.Services.TimeEngineRunInput>())
            .Returns(call =>
            {
                var runInput = call.Arg<HCMv3.TimeManagement.Services.TimeEngineRunInput>();
                if (runInput.EmployeeCodes == "E1")
                {
                    harness.TimeLogs.Add(new HCMv3.TimeManagement.TimeLog
                    {
                        Id = 1,
                        EmployeeId = Harness.EmployeeId,
                        Status = 1,
                        Message = "Duty not found",
                        DayDate = new DateTime(2024, 6, 5),
                        RunTimeStamp = DateTime.Now.AddMinutes(1)
                    });
                }

                return Task.FromResult(new HCMv3.TimeManagement.Services.TimeEngineRunResult());
            });

        var result = await harness.Build().RunAsync(harness.RegularInput());

        var first = result.EmployeeResults.Single(r => r.EmployeeCode == "E1");
        first.Succeeded.ShouldBeFalse();
        first.Message.ShouldContain("TimeEngine : Duty not found");

        // The run carries on, and the second employee is paid.
        result.EmployeeResults.Single(r => r.EmployeeCode == "E2").Succeeded.ShouldBeTrue();
    }

    #endregion

    #region << The 2005 / 2011 clearing blocks are wired in >>

    /// <summary>
    /// <b>Wiring, not arithmetic.</b> The four clearing operations are what make a payroll RE-RUN safe:
    /// <c>AdvancePaymentDed</c> ACCUMULATES onto its row, so an off-cycle that does not clear first recovers
    /// twice what it advanced. They are cheap to drop by accident in a refactor and produce no error when
    /// missing -- only a double deduction weeks later -- so the engine's calls to them are pinned here.
    ///
    /// <para>Legacy call sites: <c>Program.cs</c> line 1201 (delete, once per employee before the period loop)
    /// and lines 1796-1819 / 1832-1850 (zero, per register).</para>
    /// </summary>
    [Fact]
    public async Task RunAsync_Should_Clear_The_Previous_Advance_Recovery_Before_An_OffCycle_Run()
    {
        var harness = new Harness();
        var input = harness.OffCycleAdvanceLeaveInput();

        await harness.Build().RunAsync(input);

        // Block A -- the hard delete, keyed on the exact off-cycle payment date.
        await harness.AdvancePaymentService.Received(1)
            .DeleteAdvancePaymentDedAsync(Harness.EmployeeId, OffCyclePaymentDate);

        // Block C2 -- the window zero. payStartDate is the 1st of the run month; offCycleEndDate has been
        // narrowed to the leave's end date (10/07/2024) by ResolveOffCycleEndDate.
        await harness.AdvancePaymentService.Received(1).ZeroAdvanceRecoveryInWindowAsync(
            Harness.EmployeeId, new DateTime(2024, 6, 1), new DateTime(2024, 7, 10));

        // Block C4 -- the 2011 carry-forward NetPay may have written into the following month.
        await harness.AdvancePaymentService.Received(1)
            .ZeroLastMonthRecoveryAsync(Harness.EmployeeId, PayInDate);

        // Block B belongs to the Regular arm only.
        await harness.AdvancePaymentService.DidNotReceive()
            .ZeroFutureAdvanceRecoveryAsync(Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<bool>());
    }

    /// <summary>
    /// The Regular arm runs exactly one clear -- legacy's own asymmetry. Its counterpart at legacy line 1135 is
    /// COMMENTED OUT in the legacy source, so a regular run must not delete anything.
    /// </summary>
    [Fact]
    public async Task RunAsync_Should_Only_Zero_Future_Recovery_On_A_Regular_Run()
    {
        var harness = new Harness();

        await harness.Build().RunAsync(harness.RegularInput());

        await harness.AdvancePaymentService.Received(1)
            .ZeroFutureAdvanceRecoveryAsync(Harness.EmployeeId, PayInDate, Arg.Any<bool>());

        await harness.AdvancePaymentService.DidNotReceive()
            .DeleteAdvancePaymentDedAsync(Arg.Any<int>(), Arg.Any<DateTime>());
        await harness.AdvancePaymentService.DidNotReceive()
            .ZeroAdvanceRecoveryInWindowAsync(Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<DateTime>());
        await harness.AdvancePaymentService.DidNotReceive()
            .ZeroLastMonthRecoveryAsync(Arg.Any<int>(), Arg.Any<DateTime>());
    }

    /// <summary>
    /// Legacy's "is the advance-paid leave still on file?" test (lines 1623-1629) decides whether the FUTURE
    /// months' recovery is abandoned. The engine must pass the answer, not decide it inside the service: here
    /// the employee has no such leave on the pay date, so the flag is false and the clear goes ahead.
    /// </summary>
    [Fact]
    public async Task RunAsync_Should_Tell_The_Clear_Whether_The_AdvancePaid_Leave_Still_Exists()
    {
        var harness = new Harness();

        await harness.Build().RunAsync(harness.RegularInput());

        await harness.AdvancePaymentService.Received(1)
            .ZeroFutureAdvanceRecoveryAsync(Harness.EmployeeId, PayInDate, false);
    }

    #endregion

    #region << On-hold gates the resolver does NOT apply >>

    [Fact]
    public async Task RunAsync_Should_Skip_LeavePay_When_OnHold_LeavePay_Is_Set()
    {
        var harness = new Harness();
        var leaveWageType = WageType("1050", "Annual Leave Pay", 100, BillingType.Earning);
        harness.WageTypes.Add(leaveWageType);
        harness.LeavePayWageTypeIds.Add(leaveWageType.Id);

        harness.OnHolds.Add(OnHold(onHoldLeavePay: true));

        await harness.Build().RunAsync(harness.RegularInput());

        await harness.LeavePayService.DidNotReceive().CalculateAsync(Arg.Any<PayrollCalculationContext>());
    }

    [Fact]
    public async Task RunAsync_Should_Run_LeavePay_When_OnHold_LeavePay_Is_Not_Set()
    {
        var harness = new Harness();
        var leaveWageType = WageType("1050", "Annual Leave Pay", 100, BillingType.Earning);
        harness.WageTypes.Add(leaveWageType);
        harness.LeavePayWageTypeIds.Add(leaveWageType.Id);

        await harness.Build().RunAsync(harness.RegularInput());

        await harness.LeavePayService.Received(1).CalculateAsync(Arg.Any<PayrollCalculationContext>());
    }

    /// <summary>
    /// <c>onHold_BasicPay</c> works the OPPOSITE way to every other hold: legacy passes it INTO
    /// <c>EmployeeBasicPayService</c>, where it suppresses Number/Amount but STILL WRITES RATE (legacy
    /// <c>EmployeeBasicPayService.cs</c> lines 224-228). Gating basic pay out on it would drop the rate row.
    /// </summary>
    [Fact]
    public async Task RunAsync_Should_Still_Dispatch_BasicPay_When_OnHold_BasicPay_Is_Set_And_Pass_The_Flag_Through()
    {
        var harness = new Harness();
        var basicWageType = WageType("1000", "Basic Pay", 100, BillingType.Earning);
        harness.WageTypes.Add(basicWageType);
        harness.BasicPayWageTypeIds.Add(basicWageType.Id);

        harness.OnHolds.Add(OnHold(onHoldBasicPay: true));

        PayrollCalculationContext captured = null;
        harness.BasicPayService
            .CalculateAsync(Arg.Do<PayrollCalculationContext>(c => captured = c))
            .Returns(Task.CompletedTask);

        await harness.Build().RunAsync(harness.RegularInput());

        await harness.BasicPayService.Received(1).CalculateAsync(Arg.Any<PayrollCalculationContext>());
        captured.ShouldNotBeNull();
        captured.OnHold.BasicPay.ShouldBeTrue();
    }

    /// <summary>
    /// Legacy line 2547's earning/deduction PAIR test -- NOT the resolver's generic <c>IsOnHold</c>.
    /// A Deduction payment must still run while only earnings are on hold.
    /// </summary>
    [Fact]
    public async Task RunAsync_Should_Apply_The_Earning_Deduction_Pair_Gate_To_Payments()
    {
        var harness = new Harness();
        var earning = WageType("1100", "Bonus", 100, BillingType.Earning);
        var deduction = WageType("2100", "Fine", 200, BillingType.Deduction);
        harness.WageTypes.Add(earning);
        harness.WageTypes.Add(deduction);

        harness.Payments.Add(new EmployeePayment
        {
            Id = 1,
            EmployeeId = Harness.EmployeeId,
            WageTypeId = earning.Id,
            PaymentDate = new DateTime(2024, 6, 10)
        });
        harness.Payments.Add(new EmployeePayment
        {
            Id = 2,
            EmployeeId = Harness.EmployeeId,
            WageTypeId = deduction.Id,
            PaymentDate = new DateTime(2024, 6, 10)
        });

        harness.OnHolds.Add(OnHold(onHoldAllEarnings: true));

        var dispatched = new List<string>();
        harness.PaymentService
            .CalculateAsync(Arg.Do<PayrollCalculationContext>(c => dispatched.Add(c.WageType.Code)))
            .Returns(Task.CompletedTask);

        var engine = harness.Build();
        engine.PaymentService = harness.PaymentService;

        await engine.RunAsync(harness.RegularInput());

        dispatched.ShouldBe(new[] { "2100" });
    }

    #endregion

    #region << Per-employee error isolation >>

    [Fact]
    public async Task RunAsync_Should_Isolate_A_Failing_Employee_And_Continue_With_The_Next()
    {
        var harness = new Harness();
        harness.AddSecondEmployee();

        var netPay = WageType("9000", "Net Pay", 900, BillingType.Deduction);
        harness.WageTypes.Add(netPay);
        harness.CalculatedWageCodes.Add("9000");

        // Employee 1 blows up inside the wage-type loop; employee 2 must still be calculated.
        harness.ThrowForEmployeeId = Harness.EmployeeId;

        var result = await harness.Build().RunAsync(harness.RegularInput());

        result.EmployeeResults.Count.ShouldBe(2);

        var failed = result.EmployeeResults.Single(r => r.EmployeeId == Harness.EmployeeId);
        failed.Succeeded.ShouldBeFalse();

        var succeeded = result.EmployeeResults.Single(r => r.EmployeeId == Harness.SecondEmployeeId);
        succeeded.Succeeded.ShouldBeTrue();

        // One failed + one successful PayrollLog row.
        harness.PayrollLogs.Count(r => r.Status == 1).ShouldBe(1);
        harness.PayrollLogs.Count(r => r.Status == 0).ShouldBe(1);
    }

    /// <summary>Legacy lines 2594-2599: the wage code and name are appended to the logged message.</summary>
    [Fact]
    public async Task RunAsync_Should_Wrap_A_WageType_Failure_With_The_Legacy_Message()
    {
        var harness = new Harness();
        harness.WageTypes.Add(WageType("9000", "Net Pay", 900, BillingType.Deduction));
        harness.CalculatedWageCodes.Add("9000");
        harness.ThrowForEmployeeId = Harness.EmployeeId;

        var result = await harness.Build().RunAsync(harness.RegularInput());

        var failed = result.EmployeeResults.Single();
        failed.Succeeded.ShouldBeFalse();
        failed.Message.ShouldContain("Error in calculating WageType '9000 : Net Pay'");
        failed.Message.ShouldContain("boom");
    }

    [Fact]
    public async Task RunAsync_Should_Finalize_The_ExecutionLog_As_Partial_When_Some_Employees_Failed()
    {
        var harness = new Harness();
        harness.AddSecondEmployee();
        harness.WageTypes.Add(WageType("9000", "Net Pay", 900, BillingType.Deduction));
        harness.CalculatedWageCodes.Add("9000");
        harness.ThrowForEmployeeId = Harness.EmployeeId;

        await harness.Build().RunAsync(harness.RegularInput());

        var log = harness.ExecutionLogs.Single();
        log.Module.ShouldBe(ExecutionLogModule.PayrollEngine);
        log.TotalRecords.ShouldBe(2);
        log.Successfull.ShouldBe(1);
        log.Failed.ShouldBe(1);
        log.ExecutionStatus.ShouldBe(ExecutionLogStatus.PARTIAL);
    }

    /// <summary>
    /// Legacy lines 776-777: a <see cref="PayrollLog"/> row with StopFlag aborts the run.
    ///
    /// <para><b>Now scoped to THIS run.</b> Legacy compared against every row in the table and got away with it
    /// because its Run button truncated the table first. Unscoped, stopping one payroll area would abort a
    /// concurrent run of another, so the check now also requires <c>RunTimeStamp</c> to fall inside this run —
    /// hence the future stamp here. The companion
    /// <see cref="RunAsync_Should_Ignore_A_StopFlag_From_An_Earlier_Run"/> pins the other half.</para>
    /// </summary>
    [Fact]
    public async Task RunAsync_Should_Abort_When_A_StopFlag_PayrollLog_Row_Exists()
    {
        var harness = new Harness();
        harness.PayrollLogs.Add(new PayrollLog
        {
            Id = 99,
            StopFlag = true,
            RunTimeStamp = DateTime.Now.AddMinutes(1)
        });

        var result = await harness.Build().RunAsync(harness.RegularInput());

        result.Aborted.ShouldBeTrue();
        result.EmployeeResults.ShouldBeEmpty();
        harness.ExecutionLogs.Single().ExecutionStatus.ShouldBe(ExecutionLogStatus.ABORTED);
    }

    #endregion

    #region << Live progress reporting >>

    /// <summary>
    /// The engine must publish a row for EVERY employee it processes -- succeeded and failed alike, with the
    /// failure message and a MEASURED duration -- which is exactly what the operator grid displays.
    /// </summary>
    [Fact]
    public async Task RunAsync_Should_Publish_Live_Progress_For_Every_Employee_Succeeded_And_Failed()
    {
        var harness = new Harness();
        harness.AddSecondEmployee();
        harness.WageTypes.Add(WageType("9000", "Net Pay", 900, BillingType.Deduction));
        harness.CalculatedWageCodes.Add("9000");
        harness.ThrowForEmployeeId = Harness.EmployeeId;

        var progress = new RecordingEngineRunProgressWriter();
        var engine = harness.Build();
        engine.ProgressWriter = progress;

        await engine.RunAsync(harness.RegularInput());

        var started = progress.StartedRuns.ShouldHaveSingleItem();
        started.EngineType.ShouldBe(EngineType.PayrollEngine);
        started.Parameters.ShouldContain("Payroll Area : " + PayrollAreaName,
            customMessage: "The progress row must carry the same parameter string as the ExecutionLog row.");

        progress.TotalsPublished.ShouldHaveSingleItem().ShouldBe(2);

        progress.Employees.Count.ShouldBe(2);

        var failed = progress.Employees.Single(e => e.EmployeeId == Harness.EmployeeId);
        failed.Succeeded.ShouldBeFalse();
        failed.Message.ShouldContain("Error in calculating WageType '9000 : Net Pay'");
        failed.SequenceNo.ShouldBe(1);

        var succeeded = progress.Employees.Single(e => e.EmployeeId == Harness.SecondEmployeeId);
        succeeded.Succeeded.ShouldBeTrue();
        succeeded.Message.ShouldBe("Successfull");
        succeeded.EmployeeCode.ShouldBe("E2");
        succeeded.SequenceNo.ShouldBe(2);

        // Measured, not assumed: the ETA is derived from these numbers, so they must be real.
        progress.Employees.ShouldAllBe(e => e.ElapsedMilliseconds >= 0);
        progress.Employees.ShouldAllBe(e => e.CompletedAt >= e.StartedAt);

        progress.Completed.ShouldBeTrue("A run that never closes its progress row sits on the screen forever.");
        progress.CompletedStatus.ShouldBe(EngineRunStatus.CompletedWithErrors);
        progress.CompletedExecutionLogId.ShouldBe(harness.ExecutionLogs.Single().Id);
        progress.CompletedFailureMessage.ShouldBeNull("A per-employee failure is not a run-level failure.");
    }

    [Fact]
    public async Task RunAsync_Should_Close_Progress_As_Completed_When_Every_Employee_Succeeds()
    {
        var harness = new Harness();
        harness.AddSecondEmployee();

        var progress = new RecordingEngineRunProgressWriter();
        var engine = harness.Build();
        engine.ProgressWriter = progress;

        await engine.RunAsync(harness.RegularInput());

        progress.CompletedStatus.ShouldBe(EngineRunStatus.Completed);
    }

    /// <summary>
    /// A Stop pressed after the run's transaction opened must actually stop it. The legacy
    /// <c>PayrollLog.StopFlag</c> check cannot see such a request, which is the whole reason the stop flag
    /// moved onto the independently-committed progress row.
    /// </summary>
    [Fact]
    public async Task RunAsync_Should_Abort_When_A_Stop_Is_Requested_Through_The_Progress_Row()
    {
        var harness = new Harness();
        harness.AddSecondEmployee();

        var progress = new RecordingEngineRunProgressWriter { StopRequested = true };
        var engine = harness.Build();
        engine.ProgressWriter = progress;

        var result = await engine.RunAsync(harness.RegularInput());

        result.Aborted.ShouldBeTrue();
        result.EmployeeResults.ShouldBeEmpty();
        progress.CompletedStatus.ShouldBe(EngineRunStatus.Aborted);
    }

    #endregion

    #region << Register creation / target threading >>

    [Fact]
    public async Task RunAsync_Should_Create_One_SalaryRegister_Header_Per_Period()
    {
        var harness = new Harness();

        var result = await harness.Build().RunAsync(harness.RegularInput());

        harness.SalaryRegisters.Count.ShouldBe(1);
        var register = harness.SalaryRegisters.Single();
        register.EmployeeId.ShouldBe(Harness.EmployeeId);
        register.ForPeriod.ShouldBe(PayInDate);
        register.InPeriod.ShouldBe(PayInDate);
        register.PayrollRunType.ShouldBe(PayrollRunType.Regular);
        register.PayrollResultType.ShouldBe(PayrollResultType.Current);

        result.EmployeeResults.Single().SalaryRegisterIds.ShouldContain(register.Id);
    }

    /// <summary>
    /// Wave P7 must stay a parameter flip: whatever <see cref="PayrollEngineRunInput.RegisterTarget"/> says
    /// has to reach <see cref="PayrollRegisterRef.Target"/> unchanged.
    /// </summary>
    [Fact]
    public async Task RunAsync_Should_Thread_The_RegisterTarget_Through_To_The_Calculation_Context()
    {
        var harness = new Harness();
        harness.WageTypes.Add(WageType("1000", "Basic Pay", 100, BillingType.Earning));
        harness.BasicPayWageTypeIds.Add(harness.WageTypes.Last().Id);

        PayrollCalculationContext captured = null;
        harness.BasicPayService
            .CalculateAsync(Arg.Do<PayrollCalculationContext>(c => captured = c))
            .Returns(Task.CompletedTask);

        var input = harness.RegularInput();
        input.RegisterTarget = PayrollRegisterTarget.Live;

        await harness.Build().RunAsync(input);

        captured.ShouldNotBeNull();
        captured.Register.Target.ShouldBe(PayrollRegisterTarget.Live);
        captured.Register.RegisterId.ShouldBe(harness.SalaryRegisters.Single().Id);
    }

    #endregion

    #region << Run-level guards (these DO throw) >>

    [Fact]
    public async Task RunAsync_Should_Throw_When_No_PayInDate_Was_Given()
    {
        var harness = new Harness();
        var input = harness.RegularInput();
        input.PayInDate = default(DateTime);

        var ex = await Assert.ThrowsAsync<Abp.UI.UserFriendlyException>(() => harness.Build().RunAsync(input));
        ex.Message.ShouldBe("No Pay Date defined for Payroll Calculation");
    }

    [Fact]
    public async Task RunAsync_Should_Throw_When_The_Control_Record_Is_Not_Locked()
    {
        var harness = new Harness();
        harness.ControlRecord.PeriodLocked = false;

        var ex = await Assert.ThrowsAsync<Abp.UI.UserFriendlyException>(
            () => harness.Build().RunAsync(harness.RegularInput()));
        ex.Message.ShouldBe("Payroll Control Record not locked or is not current.");
    }

    [Fact]
    public async Task RunAsync_Should_Throw_When_The_Payroll_Calendar_Has_No_Row_For_The_Month()
    {
        var harness = new Harness();
        harness.PayrollCalendars.Clear();

        var ex = await Assert.ThrowsAsync<Abp.UI.UserFriendlyException>(
            () => harness.Build().RunAsync(harness.RegularInput()));
        ex.Message.ShouldBe("Payroll Calender does not have record for Payroll month you are running.");
    }

    #endregion

    #region << A throwing category service fails only that employee >>

    /// <summary>
    /// Wave P9 rewrote this test. It previously asserted the SCAFFOLDING behaviour of the unported era --
    /// that reaching a category service produced a "not ported" message -- which stopped being meaningful the
    /// moment all four were implemented.
    ///
    /// <para>The invariant underneath it is still worth protecting, and is arguably more important now: an
    /// exception raised inside a category service must fail ONLY the employee being processed, leaving the
    /// run to continue for everyone else. A regression here would turn one employee's bad data into a whole
    /// payroll run's failure. So the assertion is re-pointed at that isolation guarantee, driven by an
    /// arrear service that is configured to throw.</para>
    /// </summary>
    [Fact]
    public async Task RunAsync_Should_Fail_Only_That_Employee_When_A_Category_Service_Throws()
    {
        var harness = new Harness();
        var arrearWageType = WageType("5000", "Basic Pay Arrear", 100, BillingType.Earning);
        arrearWageType.IncludeInArrearPay = true;
        harness.WageTypes.Add(arrearWageType);

        harness.ArrearService
            .CalculateAsync(Arg.Any<PayrollCalculationContext>())
            .Returns(_ => Task.FromException(new InvalidOperationException("arrear boom")));

        var result = await harness.Build().RunAsync(harness.RegularInput());

        var employeeResult = result.EmployeeResults.Single();
        employeeResult.Succeeded.ShouldBeFalse();
        employeeResult.Message.ShouldContain("arrear boom");
    }

    #endregion

    #region << Fixtures >>

    private static int _nextWageTypeId = 1;

    private static WageType WageType(string code, string name, int payrollOrder, BillingType billingType)
    {
        return new WageType
        {
            Id = _nextWageTypeId++,
            Code = code,
            NameEn = name,
            NameAr = name,
            PayrollOrder = payrollOrder,
            BillingType = billingType,
            Status = true
        };
    }

    private static EmployeeOnHold OnHold(
        bool onHoldBasicPay = false,
        bool onHoldLeavePay = false,
        bool onHoldAllEarnings = false,
        bool onHoldAllDeductions = false)
    {
        return new EmployeeOnHold
        {
            Id = 1,
            EmployeeId = Harness.EmployeeId,
            StartDate = new DateTime(2000, 1, 1),
            EndDate = new DateTime(2099, 1, 1),
            OnHold = false,
            OnHoldBasicPay = onHoldBasicPay,
            OnHoldLeavePay = onHoldLeavePay,
            OnHoldAllEarnings = onHoldAllEarnings,
            OnHoldAllDeductions = onHoldAllDeductions
        };
    }

    /// <summary>
    /// Assembles a <see cref="PayrollEngineService"/> over in-memory repositories and substituted collaborators.
    /// Everything defaults to "one active employee, one unlocked-for-retro period, nothing on hold".
    /// </summary>
    private sealed class Harness
    {
        public const int EmployeeId = 1;
        public const int SecondEmployeeId = 2;
        public const int PayrollAreaId = 7;

        public readonly List<Employee> Employees = new List<Employee>();
        public readonly List<EmployeeOrgAssignment> OrgAssignments = new List<EmployeeOrgAssignment>();
        public readonly List<EmployeeContract> Contracts = new List<EmployeeContract>();
        public readonly List<EmployeePayrollStatus> PayrollStatuses = new List<EmployeePayrollStatus>();
        public readonly List<EmployeeOnHold> OnHolds = new List<EmployeeOnHold>();
        public readonly List<EmployeeAbsence> Absences = new List<EmployeeAbsence>();
        public readonly List<EmployeeAbsAdjustment> AbsAdjustments = new List<EmployeeAbsAdjustment>();
        public readonly List<EmployeeFnFPayment> FnFPayments = new List<EmployeeFnFPayment>();
        public readonly List<EmployeeSeparation> Separations = new List<EmployeeSeparation>();
        public readonly List<EmployeePayment> Payments = new List<EmployeePayment>();
        public readonly List<EmployeeRecurringPayment> RecurringPayments = new List<EmployeeRecurringPayment>();
        public readonly List<EmployeeOvertime> Overtimes = new List<EmployeeOvertime>();
        public readonly List<EmployeeAttendance> Attendances = new List<EmployeeAttendance>();
        public readonly List<EmployeeSubGroupPayment> SubGroupPayments = new List<EmployeeSubGroupPayment>();
        public readonly List<SalaryRegister> SalaryRegisters = new List<SalaryRegister>();
        public readonly List<PayrollCalendar> PayrollCalendars = new List<PayrollCalendar>();
        public readonly List<PayrollReasonType> ReasonTypes = new List<PayrollReasonType>();
        public readonly List<PayrollLog> PayrollLogs = new List<PayrollLog>();
        public readonly List<WageType> WageTypes = new List<WageType>();
        public readonly List<WageTypeException> WageTypeExceptions = new List<WageTypeException>();
        public readonly List<TimeRegister> TimeRegisters = new List<TimeRegister>();
        public readonly List<ExecutionLog> ExecutionLogs = new List<ExecutionLog>();

        public readonly List<string> DispatchedWageCodes = new List<string>();
        public readonly HashSet<string> CalculatedWageCodes = new HashSet<string>();
        public readonly List<int> BasicPayWageTypeIds = new List<int>();
        public readonly List<int> LeavePayWageTypeIds = new List<int>();

        public int? ThrowForEmployeeId;

        public PayrollArea PayrollArea;
        public PayrollControlRecord ControlRecord;

        public readonly IEmployeeBasicPayCalculationService BasicPayService = Substitute.For<IEmployeeBasicPayCalculationService>();
        public readonly IEmployeeLeavePayCalculationService LeavePayService = Substitute.For<IEmployeeLeavePayCalculationService>();

        // Wave P9: the four category services are REQUIRED constructor dependencies (previously optional
        // property injections defaulting to a throwing stub). Held as fields so individual tests can drive
        // them -- see RunAsync_Should_Fail_Only_That_Employee_When_A_Category_Service_Throws.
        // PaymentService is declared just above; it predates this wave.
        public readonly IPayrollArrearCalculationService ArrearService = Substitute.For<IPayrollArrearCalculationService>();
        public readonly IEmployeeOvertimePayCalculationService OvertimePayService = Substitute.For<IEmployeeOvertimePayCalculationService>();
        public readonly IEmployeeAttendancePayCalculationService AttendancePayService = Substitute.For<IEmployeeAttendancePayCalculationService>();
        public readonly IEmployeePaymentCalculationService PaymentService = Substitute.For<IEmployeePaymentCalculationService>();

        /// <summary>
        /// The engine calls this for the four 2005/2011 clearing blocks that make a payroll re-run safe (legacy
        /// <c>Program.cs</c> lines 1201, 1619-1653 and 1787-1851). Substituted so the tests below can assert
        /// WHICH clears fire on which run type -- the arithmetic lives in
        /// <see cref="OffCycleAdvanceLeaveScenario_Tests"/>.
        /// </summary>
        public readonly IPayrollAdvancePaymentService AdvancePaymentService = Substitute.For<IPayrollAdvancePaymentService>();

        /// <summary>
        /// The Time Engine, which the payroll run now calls per employee when
        /// <c>ManageTimeSettings.EnableTimeEngineAutorun</c> is on (legacy <c>Program.cs</c> 554-753). Substituted
        /// so these tests can assert the WINDOW it is handed -- the arithmetic of time evaluation is
        /// <c>TimeEngineService</c>'s own concern.
        /// </summary>
        public readonly HCMv3.TimeManagement.Services.ITimeEngineService TimeEngineService =
            Substitute.For<HCMv3.TimeManagement.Services.ITimeEngineService>();

        public readonly List<HCMv3.TimeManagement.TimeLog> TimeLogs = new List<HCMv3.TimeManagement.TimeLog>();

        /// <summary>Turns autorun on and makes the Time Engine return a successful, empty result.</summary>
        public void EnableTimeEngineAutorun()
        {
            AutorunEnabled = true;
            TimeEngineService.CreateTimeRegisterAsync(Arg.Any<HCMv3.TimeManagement.Services.TimeEngineRunInput>())
                .Returns(_ => Task.FromResult(new HCMv3.TimeManagement.Services.TimeEngineRunResult()));
        }

        public bool AutorunEnabled { get; private set; }

        /// <summary>
        /// Set to a run key to make the concurrency guard see a run already in progress for it. Null (the
        /// default) means nothing is running. Tests that install their own progress writer after
        /// <see cref="Build"/> override this, which is deliberate.
        /// </summary>
        public string ActiveRunForKey { get; set; }

        public Harness()
        {
            PayrollArea = new PayrollArea { Id = PayrollAreaId, Code = "PA", Name = PayrollAreaName, CutOffDay = 0 };

            ControlRecord = new PayrollControlRecord
            {
                Id = 1,
                PayrollAreaId = PayrollAreaId,
                PayrollArea = PayrollArea,
                CalendarMonthEnd = PayInDate,
                CalendarMonthStart = new DateTime(2024, 6, 1),
                PeriodLocked = true,
                PeriodClosed = false
            };

            PayrollCalendars.Add(new PayrollCalendar
            {
                Id = 1,
                PayrollAreaId = PayrollAreaId,
                PayrollMonth = PayInDate.Month,
                PayrollYear = PayInDate.Year
            });

            AddEmployee(EmployeeId, "E1");
        }

        public void AddSecondEmployee() => AddEmployee(SecondEmployeeId, "E2");

        private void AddEmployee(int id, string code)
        {
            Employees.Add(new Employee
            {
                Id = id,
                Code = code,
                Status = EmployeeStatus.Active,
                JoiningDate = new DateTime(2020, 1, 1)
            });

            OrgAssignments.Add(new EmployeeOrgAssignment
            {
                Id = id,
                EmployeeId = id,
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2099, 1, 1)
            });

            Contracts.Add(new EmployeeContract
            {
                Id = id,
                EmployeeId = id,
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2099, 1, 1)
            });

            PayrollStatuses.Add(new EmployeePayrollStatus
            {
                Id = id,
                EmployeeId = id
            });
        }

        public PayrollEngineRunInput RegularInput()
        {
            return new PayrollEngineRunInput
            {
                PayrollRunType = PayrollRunType.Regular,
                PayInDate = PayInDate,
                PayrollAreaName = PayrollAreaName,
                RegisterTarget = PayrollRegisterTarget.Live
            };
        }

        /// <summary>
        /// An Advance Leave off-cycle for the period AFTER the one the control record sits on, set up the way
        /// <see cref="PayrollControlRecordGuard"/> insists: the control record CLOSED and one period behind, an
        /// employee code (mandatory off-cycle), and a paid <c>PayInAdvance</c> absence on the payment date so
        /// <c>ResolveOffCycleEndDate</c> can narrow the window to the leave's end.
        /// </summary>
        public PayrollEngineRunInput OffCycleAdvanceLeaveInput()
        {
            // Legacy requires CalendarMonthEnd < offCyclePaymentDate AND NextPeriodPayInDate == payInDate, so
            // the record has to sit on the month BEFORE PayInDate.
            ControlRecord.CalendarMonthEnd = new DateTime(2024, 5, 31);
            ControlRecord.CalendarMonthStart = new DateTime(2024, 5, 1);
            ControlRecord.PeriodLocked = false;
            ControlRecord.PeriodClosed = true;

            var absenceType = new AbsenceType
            {
                Id = 90,
                Code = "1",
                NameEn = "Annual Leave",
                IsPaid = true,
                IsActive = true
            };

            Absences.Add(new EmployeeAbsence
            {
                Id = 90,
                EmployeeId = EmployeeId,
                AbsenceTypeId = absenceType.Id,
                AbsenceType = absenceType,
                // Both navigations must be set: the autorun employee-selection branch filters on
                // r.Employee.Status and r.AbsenceType.IsPaid. Against a real DbContext those translate to
                // joins; against the in-memory fake they are dereferenced, so an unset one is a null ref.
                Employee = Employees.Single(e => e.Id == EmployeeId),
                StartDate = new DateTime(2024, 6, 20),
                EndDate = new DateTime(2024, 7, 10),
                LeavePaymentType = LeavePaymentType.PayInAdvance,
                PaymentDate = OffCyclePaymentDate
            });

            // Legacy refuses an off-cycle the Time Engine has not caught up to (Program.cs lines 852-858):
            // NextTimeEvaluationFrom must be >= the off-cycle payment date (15/06).
            //
            // But it must ALSO stay inside the autorun's window, because that window starts here (legacy lines
            // 570-572) and legacy skips the time-engine call entirely when start > end (line 693). Pushing this
            // far into the future would satisfy the first rule and silently defeat the second -- which is
            // exactly what RunAsync_Should_Skip_The_Time_Engine_When_The_Window_Is_Already_Covered asserts on
            // purpose. 20/06 satisfies both.
            foreach (var status in PayrollStatuses)
            {
                status.NextTimeEvaluationFrom = new DateTime(2024, 6, 20);
            }

            return new PayrollEngineRunInput
            {
                PayrollRunType = PayrollRunType.OffCycle,
                PayInDate = PayInDate,
                PayrollAreaName = PayrollAreaName,
                EmployeeCodes = "E1",
                OffCyclePaymentDate = OffCyclePaymentDate,
                OffCycleReason = PayrollOffCycleReasons.AdvanceLeave,
                RegisterTarget = PayrollRegisterTarget.Live
            };
        }

        public PayrollEngineService Build()
        {
            var settingsAccessor = Substitute.For<ISysSettingsAccessor>();
            settingsAccessor.GetPayrollSettingsAsync().Returns(new ManagePayrollSettings { DecimalInSalaryRegister = 6 });
            settingsAccessor.GetTimeSettingsAsync().Returns(
                new ManageTimeSettings { EnableTimeEngineAutorun = AutorunEnabled });
            settingsAccessor.GetSecuritySettingsAsync().Returns(new ManageSecuritySettings());

            var areaResolver = Substitute.For<IEmployeePayrollAreaResolver>();
            areaResolver.GetEmployeeIdsInPayrollAreaAsync(Arg.Any<string>(), Arg.Any<DateTime>())
                .Returns(_ => Task.FromResult(Employees.Select(e => e.Id).ToList()));
            areaResolver.GetEmployeePayrollAreaAsync(Arg.Any<int>(), Arg.Any<DateTime>())
                .Returns(_ => Task.FromResult(PayrollArea));

            BasicPayService.GetBasicPayWageTypeIdsAsync(Arg.Any<int>(), Arg.Any<DateTime>())
                .Returns(_ => Task.FromResult(BasicPayWageTypeIds.ToList()));
            LeavePayService.GetLeavePayWageTypeIdsAsync()
                .Returns(_ => Task.FromResult(LeavePayWageTypeIds.ToList()));

            var calculatorResolver = Substitute.For<IWageTypeCalculatorResolver>();
            calculatorResolver.Resolve(Arg.Any<string>()).Returns(call =>
                CalculatedWageCodes.Contains(call.Arg<string>())
                    ? Substitute.For<IWageTypeCalculator>()
                    : null);

            // WAVE P9: the engine's calculated-pay membership test moved from Resolve(code) != null to
            // HasCalculatorFor(wageType), because legacy's wtCalculatedWageList also contains every wage type
            // referenced by a DisciplinaryType/TicketType/LoanType/TravelType (legacy Program.cs lines
            // 2698-2701) -- codes NO calculator claims by name. Stubbing only Resolve leaves HasCalculatorFor
            // returning NSubstitute's default false, which silently dispatches nothing at all.
            calculatorResolver.HasCalculatorFor(Arg.Any<WageType>()).Returns(call =>
                CalculatedWageCodes.Contains(call.Arg<WageType>().Code));

            calculatorResolver.TryCalculateAsync(Arg.Any<PayrollCalculationContext>()).Returns(call =>
            {
                var context = call.Arg<PayrollCalculationContext>();

                if (ThrowForEmployeeId != null && context.EmployeeId == ThrowForEmployeeId.Value)
                {
                    throw new InvalidOperationException("boom");
                }

                DispatchedWageCodes.Add(context.WageType.Code);
                return Task.FromResult(true);
            });

            var engine = new PayrollEngineService(
                Writable(Employees),
                Writable(OrgAssignments),
                Writable(Contracts),
                Writable(PayrollStatuses),
                Writable(OnHolds),
                Writable(Absences),
                Writable(AbsAdjustments),
                Writable(FnFPayments),
                Writable(Separations),
                Writable(Payments),
                Writable(RecurringPayments),
                Writable(Overtimes),
                Writable(Attendances),
                Writable(SubGroupPayments),
                Writable(SalaryRegisters),
                Writable(new List<PayrollControlRecord> { ControlRecord }),
                Writable(PayrollCalendars),
                Writable(new List<PayrollArea> { PayrollArea }),
                Writable(ReasonTypes),
                Writable(PayrollLogs),
                Writable(WageTypes),
                Writable(WageTypeExceptions),
                Writable(TimeRegisters),
                Writable(ExecutionLogs),
                settingsAccessor,
                areaResolver,
                calculatorResolver,
                BasicPayService,
                LeavePayService,
                new FakePayrollRegisterStore(),
                AdvancePaymentService,
                TimeEngineService,
                Writable(TimeLogs),
                // Wave: the time-type cumulation pre-pass is now a real collaborator. These engine tests
                // assert orchestration (employee selection, register creation, wage-type dispatch), not day
                // counts, so it is stubbed out. TimeTypeCumulationServiceTests covers the pre-pass itself.
                Substitute.For<ITimeTypeCumulationService>(),
                // Wave P9: the four category services became REQUIRED constructor dependencies (they were
                // optional property injections defaulting to a throwing stub). These tests assert
                // orchestration, not category-pay arithmetic, so they are substituted; each service has its
                // own dedicated test class.
                ArrearService,
                PaymentService,
                OvertimePayService,
                AttendancePayService);

            // Only needed by the concurrency-guard tests; harmless otherwise, since a null ActiveRunForKey
            // reports nothing running. Tests that assign their own writer after Build() replace this.
            engine.ProgressWriter = new HCMv3.Tests.EngineRuns.RecordingEngineRunProgressWriter
            {
                ActiveRunKey = ActiveRunForKey
            };

            return engine;
        }
    }

    /// <summary>
    /// A WRITABLE in-memory <see cref="IRepository{TEntity}"/>. <c>PayrollEngineFakes.Repo</c> is read-only
    /// (by design -- the wage-rule waves never insert), but the engine creates
    /// <see cref="SalaryRegister"/>/<see cref="PayrollLog"/>/<see cref="ExecutionLog"/> rows and must read
    /// identities back, so <c>InsertAndGetIdAsync</c> has to work here.
    /// </summary>
    private static IRepository<TEntity> Writable<TEntity>(List<TEntity> list)
        where TEntity : class, IEntity<int>
    {
        var repository = Substitute.For<IRepository<TEntity>>();

        repository.GetAll().Returns(_ => list.AsQueryable());
        repository.GetAllIncluding(Arg.Any<Expression<Func<TEntity, object>>[]>()).Returns(_ => list.AsQueryable());

        repository.InsertAsync(Arg.Any<TEntity>()).Returns(call =>
        {
            var entity = call.Arg<TEntity>();
            if (entity.Id == 0)
            {
                entity.Id = NextId(list);
            }

            list.Add(entity);
            return Task.FromResult(entity);
        });

        repository.InsertAndGetIdAsync(Arg.Any<TEntity>()).Returns(call =>
        {
            var entity = call.Arg<TEntity>();
            if (entity.Id == 0)
            {
                entity.Id = NextId(list);
            }

            list.Add(entity);
            return Task.FromResult(entity.Id);
        });

        repository.UpdateAsync(Arg.Any<TEntity>()).Returns(call => Task.FromResult(call.Arg<TEntity>()));

        repository.DeleteAsync(Arg.Any<TEntity>()).Returns(call =>
        {
            list.Remove(call.Arg<TEntity>());
            return Task.CompletedTask;
        });

        repository.FirstOrDefaultAsync(Arg.Any<int>()).Returns(call =>
        {
            var id = call.Arg<int>();
            return Task.FromResult(list.FirstOrDefault(e => e.Id == id));
        });

        return repository;
    }

    private static int NextId<TEntity>(List<TEntity> list) where TEntity : IEntity<int>
    {
        return list.Count == 0 ? 1 : list.Max(e => e.Id) + 1;
    }

    #endregion
}
