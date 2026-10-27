using System.Threading.Tasks;
using Abp.Domain.Services;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// The payroll run orchestrator: the port of legacy <c>HCM10.PayrollEngine/Program.Main</c> +
/// <c>Program.CreateSalaryRegister</c> (legacy <c>Program.cs</c> lines 380-1596) +
/// <c>Program.RunPayrollCalculation</c> (lines 1597-2601).
///
/// <para><b>What it owns.</b> Employee selection, pay-period (retro) resolution,
/// <see cref="SalaryRegister"/> header creation/refresh, the <c>PayrollOrder</c>-ordered wage-type loop with
/// its category dispatch, per-employee error isolation, and <c>ExecutionLog</c>/<c>PayrollLog</c>
/// progress reporting.</para>
///
/// <para><b>What it does NOT own.</b> Individual wage-type rules -- those are
/// <see cref="IWageTypeCalculator"/> implementations reached through
/// <see cref="IWageTypeCalculatorResolver"/>, or the category services
/// (<see cref="IEmployeeBasicPayCalculationService"/>, <see cref="IEmployeeLeavePayCalculationService"/>,
/// and the four declared at the bottom of this file, all ported in wave P9).</para>
///
/// <para><b>Category dispatch stays here, deliberately.</b> A single-string
/// <see cref="IWageTypeCalculator.WageCode"/> cannot express legacy's routing: legacy builds
/// <c>wtBasicPayWageList</c> PER EMPLOYEE from <c>emp.BasicPay.BasicPayDetail</c> (legacy line 1923) and
/// rebuilds the overtime/attendance/payment/recurring-payment lists per employee too (lines 1932-1942).
/// So the engine builds those id sets and calls the category services directly; the resolver owns only the
/// genuinely per-code rules (legacy's <c>wtCalculatedWageList</c>, built at lines 2684-2702).</para>
///
/// <para><b>Simulation (wave P7) is a parameter flip, not a rewrite.</b>
/// <see cref="PayrollEngineRunInput.RegisterTarget"/> is threaded through into every
/// <see cref="PayrollRegisterRef.Target"/> the engine builds, so P7 only has to teach
/// <see cref="IPayrollRegisterStore"/> (and the register-header creation branch here) about
/// <see cref="PayrollRegisterTarget.Simulation"/>. Wave P7 did exactly that -- see
/// <c>PayrollEngineService.Simulation.cs</c> and <see cref="PayrollSimulationScope"/>.</para>
/// </summary>
public interface IPayrollEngineService : IDomainService
{
    /// <summary>
    /// Runs one payroll calculation. Never throws for a single bad employee -- that employee is recorded as
    /// a failed <see cref="PayrollEmployeeRunResult"/> (and a <see cref="PayrollLog"/> row) and the run
    /// continues with the next one (legacy <c>Program.cs</c> lines 1438-1460). It DOES throw for run-level
    /// problems that legacy also refused to start on (missing pay date, missing control record, missing
    /// payroll calendar, an unlocked/unclosed control record).
    /// </summary>
    Task<PayrollEngineRunResult> RunAsync(PayrollEngineRunInput input);
}

// -------------------------------------------------------------------------------------------------------
// INJECTION POINTS for the four category services. WAVE P9: ALL FOUR ARE NOW PORTED.
//
// These four interfaces exist so that wiring the implementations was mechanical: each is implemented as an
// ITransientDependency domain service in this same namespace and ABP's conventional registration satisfies
// PayrollEngineService's OPTIONAL PROPERTY injection automatically -- no change to PayrollEngineService was
// required.
//
// PayrollEngineService still keeps a Null* default that THROWS NotImplementedException naming the exact
// legacy line. It is no longer a "not ported" marker; it is the DETECTOR for Castle conventional
// registration silently failing to match an implementation to its interface. A loud, per-employee, isolated
// failure beats silently under-paying an employee who has overtime/attendance/payment/arrear wage types.
// -------------------------------------------------------------------------------------------------------

/// <summary>
/// Legacy <c>HCM10.PayrollEngine/EmployeeOvertimePayService.Main</c>, dispatched at legacy
/// <c>Program.cs</c> line 2569 for wage types in <c>wtOverTimeWageList</c>
/// (<c>EmployeeOvertime.OvertimeType.WageType</c>, rebuilt per employee at legacy line 1932).
///
/// <para><b>PORTED in wave P9</b> -- see <see cref="EmployeeOvertimePayCalculationService"/> for the rule
/// and its preserved legacy quirks.</para>
///
/// <para><b>Category gate applied by the engine, not here:</b> legacy wraps the call in
/// <c>if (!onHold_AllEarnings)</c> (legacy line 2567).</para>
///
/// <para><b>WAVE P8 made this arm reachable.</b> Until then the overtime category set was always empty
/// (<c>OvertimeType</c> had no <c>WageType</c> FK). The FK is restored and the legacy line-1932 query is
/// active, so employees with overtime now genuinely exercise this rule.</para>
/// </summary>
public interface IEmployeeOvertimePayCalculationService : IDomainService
{
    Task CalculateAsync(PayrollCalculationContext context);
}

/// <summary>
/// Legacy <c>HCM10.PayrollEngine/EmployeeAttendancePayService.Main</c>, dispatched at legacy
/// <c>Program.cs</c> line 2585 for wage types in <c>wtAttendanceWageList</c>
/// (<c>EmployeeAttendance.AttendanceType.WageType</c>, rebuilt per employee at legacy line 1936).
///
/// <para><b>PORTED in wave P9</b> -- see <see cref="EmployeeAttendancePayCalculationService"/>.</para>
///
/// <para>Legacy applies NO extra on-hold gate on this arm.</para>
/// </summary>
public interface IEmployeeAttendancePayCalculationService : IDomainService
{
    Task CalculateAsync(PayrollCalculationContext context);
}

/// <summary>
/// Legacy <c>HCM10.PayrollEngine/EmployeePaymentService.Main</c>, dispatched at legacy
/// <c>Program.cs</c> line 2549 for wage types in <c>wtPaymentWageList</c> OR <c>wtRecPaymentWageList</c>
/// (rebuilt per employee at legacy lines 1939-1942).
///
/// <para><b>PORTED in wave P9</b> -- see <see cref="EmployeePaymentCalculationService"/>.</para>
///
/// <para><b>Category gate applied by the engine, not here.</b> Legacy line 2547 is an earning/deduction
/// PAIR test, and it is NOT the same as the resolver's generic <c>IsOnHold</c>:
/// <c>(!onHold_AllEarnings &amp;&amp; BillingType == Earning) || (!onHold_AllDeductions &amp;&amp;
/// BillingType == Deduction)</c>. Note the consequence -- a Provision or TimeType wage type reaching this
/// arm fails BOTH clauses and is skipped entirely, which the resolver's gate would NOT do.</para>
/// </summary>
public interface IEmployeePaymentCalculationService : IDomainService
{
    Task CalculateAsync(PayrollCalculationContext context);
}

/// <summary>
/// Legacy <c>HCM10.PayrollEngine/PayrollArrearService.Main</c>, dispatched at legacy
/// <c>Program.cs</c> line 2534. This is the FIRST arm of legacy's else-if chain and it keys off
/// <c>WageType.IncludeInArrearPay</c> (legacy line 2532) -- i.e. an arrear wage type never reaches any other
/// category, whatever else it looks like. Legacy applies no on-hold gate on this arm.
///
/// <para><b>PORTED in wave P9</b> -- see <see cref="PayrollArrearCalculationService"/>. Because this arm is
/// evaluated for EVERY active non-TimeType wage type of EVERY employee (the flag is a wage-type property, not
/// employee data), leaving it unported aborted every employee at the first arrear wage code in
/// <c>PayrollOrder</c> sequence -- typically <c>7000 Basic Salary Arrear</c>, PayrollOrder 20020.</para>
/// </summary>
public interface IPayrollArrearCalculationService : IDomainService
{
    Task CalculateAsync(PayrollCalculationContext context);
}
