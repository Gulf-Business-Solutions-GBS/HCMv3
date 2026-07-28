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
/// and the four not-yet-ported ones in this file).</para>
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
/// <see cref="PayrollRegisterTarget.Simulation"/>. The store deliberately throws for Simulation today; that
/// is correct and pinned by tests.</para>
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
// INJECTION POINTS for the four category services that are NOT ported yet (tracked separately).
//
// These four interfaces exist so that wiring the real implementations later is mechanical: implement the
// interface as an ITransientDependency domain service in this same namespace and ABP's conventional
// registration will satisfy PayrollEngineService's OPTIONAL PROPERTY injection automatically -- no change
// to PayrollEngineService is required.
//
// Until then PayrollEngineService keeps a Null* default that THROWS NotImplementedException naming the exact
// legacy line. That is deliberate: a loud, per-employee, isolated failure beats silently under-paying an
// employee who has overtime/attendance/payment/arrear wage types configured. Employees with none of those
// wage types are unaffected, because the dispatch arm is never reached for them.
// -------------------------------------------------------------------------------------------------------

/// <summary>
/// NOT PORTED. Legacy <c>HCM10.PayrollEngine/EmployeeOvertimePayService.Main</c>, dispatched at legacy
/// <c>Program.cs</c> line 2569 for wage types in <c>wtOverTimeWageList</c>
/// (<c>EmployeeOvertime.OvertimeType.WageType</c>, rebuilt per employee at legacy line 1932).
///
/// <para><b>Category gate applied by the engine, not here:</b> legacy wraps the call in
/// <c>if (!onHold_AllEarnings)</c> (legacy line 2567).</para>
/// </summary>
public interface IEmployeeOvertimePayCalculationService : IDomainService
{
    Task CalculateAsync(PayrollCalculationContext context);
}

/// <summary>
/// NOT PORTED. Legacy <c>HCM10.PayrollEngine/EmployeeAttendancePayService.Main</c>, dispatched at legacy
/// <c>Program.cs</c> line 2585 for wage types in <c>wtAttendanceWageList</c>
/// (<c>EmployeeAttendance.AttendanceType.WageType</c>, rebuilt per employee at legacy line 1936).
///
/// <para>Legacy applies NO extra on-hold gate on this arm.</para>
/// </summary>
public interface IEmployeeAttendancePayCalculationService : IDomainService
{
    Task CalculateAsync(PayrollCalculationContext context);
}

/// <summary>
/// NOT PORTED. Legacy <c>HCM10.PayrollEngine/EmployeePaymentService.Main</c>, dispatched at legacy
/// <c>Program.cs</c> line 2549 for wage types in <c>wtPaymentWageList</c> OR <c>wtRecPaymentWageList</c>
/// (rebuilt per employee at legacy lines 1939-1942).
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
/// NOT PORTED. Legacy <c>HCM10.PayrollEngine/PayrollArrearService.Main</c>, dispatched at legacy
/// <c>Program.cs</c> line 2534. This is the FIRST arm of legacy's else-if chain and it keys off
/// <c>WageType.IncludeInArrearPay</c> (legacy line 2532) -- i.e. an arrear wage type never reaches any other
/// category, whatever else it looks like. Legacy applies no on-hold gate on this arm.
/// </summary>
public interface IPayrollArrearCalculationService : IDomainService
{
    Task CalculateAsync(PayrollCalculationContext context);
}
