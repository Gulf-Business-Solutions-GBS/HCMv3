using System;
using System.Collections.Generic;
using System.Linq;
using HCMv3.PersonnelAdministration;
using HCMv3.TimeManagement;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// All per-employee/per-period state one wage-type calculation needs. This is the DIRECT replacement for
/// the legacy 21-parameter static signature
/// <c>HCM10.PayrollEngine/EmployeeCalculatedPayService.Main(...)</c> (legacy line 24), which is the widest
/// of the six legacy <c>*PayService.Main</c> signatures and therefore the superset every wage rule of waves
/// P4-P6 codes against.
///
/// <para><b>Legacy parameter -&gt; context member map</b> (legacy
/// <c>HCM10.PayrollEngine/EmployeeCalculatedPayService.cs</c> lines 24-27):</para>
/// <list type="table">
/// <item><term><c>objectSpace</c></term><description>DROPPED -- dependency injection replaces it. A wage rule
/// injects the <c>IRepository&lt;T&gt;</c>s it needs and uses <see cref="Register"/>/
/// <see cref="IPayrollRegisterStore"/> for register I/O.</description></item>
/// <item><term><c>emp</c></term><description><see cref="Employee"/> / <see cref="EmployeeId"/></description></item>
/// <item><term><c>payDate</c></term><description><see cref="PayDate"/></description></item>
/// <item><term><c>salaryRegister</c></term><description><see cref="Register"/> (a
/// <see cref="PayrollRegisterRef"/>, NOT the entity -- see <see cref="PayrollRegisterTarget"/>)</description></item>
/// <item><term><c>payrollRunType</c></term><description><see cref="PayrollRunType"/> (also on
/// <see cref="Register"/>)</description></item>
/// <item><term><c>offCycleEndDate</c></term><description><see cref="OffCycleEndDate"/></description></item>
/// <item><term><c>offCyclePaymentDate</c></term><description><see cref="OffCyclePaymentDate"/></description></item>
/// <item><term><c>empTimeRegisterList</c></term><description><see cref="TimeRegisterList"/></description></item>
/// <item><term><c>empTimeRegister</c></term><description><see cref="TimeRegisterQuery"/></description></item>
/// <item><term><c>currWageType</c></term><description><see cref="WageType"/></description></item>
/// <item><term><c>onHold_BasicPay</c> .. <c>onHold_AllDeductions</c> (7 flags)</term>
/// <description><see cref="OnHold"/> (<see cref="PayrollOnHoldFlags"/>)</description></item>
/// <item><term><c>offCycleReasons</c></term><description><see cref="OffCycleReason"/></description></item>
/// <item><term><c>empPayrollArea</c></term><description><see cref="PayrollArea"/></description></item>
/// <item><term><c>empPaymentList</c></term><description><see cref="PaymentList"/></description></item>
/// <item><term><c>IsNotReturnFromLeave</c></term><description><see cref="IsNotReturnFromLeave"/></description></item>
/// <item><term><c>advancePayComment</c></term><description><see cref="AdvancePayComment"/></description></item>
/// </list>
///
/// <para><b>Plus the <c>Program</c> statics the rules read implicitly.</b> Legacy wage rules also reached
/// straight into <c>Program</c>'s ~40 <c>public static</c> fields. The ones the sampled rules (1006 Loan Pay,
/// 1010/3000/3003 Indemnity, 2000/2010 Late-Early, 9000 Net Pay) actually touch are carried explicitly here:
/// <see cref="Settings"/> (the whole config-flag snapshot), <see cref="PayInDate"/>,
/// <see cref="PayStartDate"/>, <see cref="PreviousSalaryRegisters"/>, <see cref="CurrentSalaryRegisters"/>.
/// A P4-P6 rule that needs a legacy static NOT present here should ADD it to this class (and document the
/// legacy field name) rather than re-query it per rule -- legacy loaded each of these exactly once per
/// employee, and re-querying changes both cost and, where the engine mutates rows mid-run, semantics.</para>
///
/// <para><b>Mutability.</b> The engine builds one instance per (employee, retro period) and reuses it for
/// every wage type in that period's <c>PayrollOrder</c> loop, assigning only <see cref="WageType"/> between
/// iterations -- exactly like legacy, which re-passed identical arguments in a loop and varied only
/// <c>currWageType</c> (legacy <c>Program.cs</c> lines 2525-2591). Wage rules MUST treat every other member
/// as read-only.</para>
/// </summary>
public class PayrollCalculationContext
{
    // ---------------------------------------------------------------------------------------------
    // Employee
    // ---------------------------------------------------------------------------------------------

    /// <summary>Legacy <c>emp.Oid</c>.</summary>
    public int EmployeeId { get; set; }

    /// <summary>
    /// Legacy <c>emp</c>. Loaded once per employee by the engine. Its child collections do NOT exist on this
    /// port's <c>Employee</c> entity (see that entity's remarks) -- a rule that needs e.g. <c>emp.Absence</c>
    /// queries <c>IRepository&lt;EmployeeAbsence&gt;</c> filtered by <see cref="EmployeeId"/>, exactly as
    /// <c>AccrualService</c> does.
    /// </summary>
    public Employee Employee { get; set; }

    // ---------------------------------------------------------------------------------------------
    // Period
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// Legacy <c>payDate</c>: the LAST day of the period currently being calculated. On a retro run this is
    /// the retro month's end, NOT <see cref="PayInDate"/> (legacy line 1057:
    /// <c>DateTime payDate = payForDate;</c> -- "Resetting PayDate to Retro Pay Date").
    /// </summary>
    public DateTime PayDate { get; set; }

    /// <summary>
    /// Legacy <c>payStartDate</c> = <c>new DateTime(payDate.Year, payDate.Month, 1)</c> (legacy line 1606).
    /// </summary>
    public DateTime PayStartDate { get; set; }

    /// <summary>
    /// Legacy <c>payInDate</c>: the month the results are PAID IN (always the last day of that month). Equals
    /// <see cref="PayDate"/> except on retro periods.
    /// </summary>
    public DateTime PayInDate { get; set; }

    /// <summary>
    /// Legacy <c>offCycleEndDate</c>. Initialised to <see cref="PayInDate"/> (legacy line 1607) and then, for
    /// off-cycle runs ONLY, narrowed per reason: End Of Service =&gt; max(FnF CompensationDate, separation
    /// LastDayInService) (legacy lines 1688-1691); Advance Leave =&gt; the advance leave's EndDate, else the
    /// leave-encashment adjustment's EffectiveDate (lines 1708/1746); Leave Encashment =&gt; the adjustment's
    /// EffectiveDate (line 1764).
    /// </summary>
    public DateTime OffCycleEndDate { get; set; }

    /// <summary>Legacy <c>offCyclePaymentDate</c>. <c>default(DateTime)</c> on a regular run.</summary>
    public DateTime OffCyclePaymentDate { get; set; }

    /// <summary>Legacy <c>payrollRunType</c>.</summary>
    public PayrollRunType PayrollRunType { get; set; }

    /// <summary>
    /// Legacy <c>offCycleReasons</c>. Compare against <see cref="PayrollOffCycleReasons"/> constants; see
    /// that class for why this stayed a string.
    /// </summary>
    public string OffCycleReason { get; set; }

    /// <summary>Convenience for legacy's ubiquitous <c>offCycleReasons == Program.endOfServiceOffcycle</c> test.</summary>
    public bool IsEndOfService => OffCycleReason == PayrollOffCycleReasons.EndOfService;

    /// <summary>Convenience for legacy's <c>offCycleReasons == Program.reasonAdvanceLeave</c> test.</summary>
    public bool IsAdvanceLeave => OffCycleReason == PayrollOffCycleReasons.AdvanceLeave;

    /// <summary>Convenience for legacy's <c>offCycleReasons == Program.reasonLeaveEncashment</c> test.</summary>
    public bool IsLeaveEncashment => OffCycleReason == PayrollOffCycleReasons.LeaveEncashment;

    // ---------------------------------------------------------------------------------------------
    // Register (target-agnostic -- see PayrollRegisterTarget)
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// Legacy <c>salaryRegister</c>, reduced to an id + period + target descriptor. Read and write its lines
    /// through <see cref="IPayrollRegisterStore"/>; never take a dependency on
    /// <see cref="SalaryRegisterDetail"/> directly, or wave P7 (simulation collapse) breaks.
    /// </summary>
    public PayrollRegisterRef Register { get; set; }

    // ---------------------------------------------------------------------------------------------
    // Current wage type (the ONLY member that varies inside the PayrollOrder loop)
    // ---------------------------------------------------------------------------------------------

    /// <summary>Legacy <c>currWageType</c>.</summary>
    public WageType WageType { get; set; }

    // ---------------------------------------------------------------------------------------------
    // Preloaded per-employee data (legacy Program statics)
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// Legacy <c>empTimeRegisterList</c>: the employee's <c>TimeRegister</c> rows for
    /// <see cref="PayStartDate"/>..<see cref="PayDate"/>. <b>Legacy sets this to <c>null</c> on off-cycle
    /// runs</b> (legacy <c>Program.cs</c> line 1771) -- that quirk is preserved, so rules must null-check it
    /// exactly where legacy did. Never <c>null</c> on a regular run (legacy lines 1614-1616).
    /// </summary>
    public List<TimeRegister> TimeRegisterList { get; set; }

    /// <summary>
    /// Legacy <c>empTimeRegister</c>. <b>Faithfully preserved legacy quirk:</b> legacy's
    /// <c>Program.GetEmployeeTimeRegister(objectSpace, emp, startDate, endDate)</c> IGNORES all four of its
    /// arguments and returns <c>objectSpace.GetObjectsQuery&lt;TimeRegister&gt;()</c> -- i.e. an UNFILTERED
    /// query over the whole table (legacy <c>Program.cs</c> lines 2713-2716). Every legacy consumer therefore
    /// re-applies its own <c>.Where(r =&gt; r.Employee.Oid == emp.Oid &amp;&amp; ...)</c>. This port keeps the
    /// same shape (<c>IRepository&lt;TimeRegister&gt;.GetAll()</c>, unfiltered) so ported rules can carry over
    /// their predicates verbatim. Do NOT pre-filter it "as an improvement": several legacy rules
    /// deliberately query days outside the pay period through it.
    /// </summary>
    public IQueryable<TimeRegister> TimeRegisterQuery { get; set; }

    /// <summary>
    /// Legacy <c>empPaymentList</c>: <c>EmployeePayment</c> rows with
    /// <c>PaymentDate</c> between <see cref="PayStartDate"/> and <see cref="OffCycleEndDate"/> and a non-null
    /// WageType (legacy <c>Program.cs</c> lines 1875-1878).
    /// </summary>
    public List<EmployeePayment> PaymentList { get; set; } = new List<EmployeePayment>();

    /// <summary>
    /// Legacy <c>Program.empOrgAssignmentList</c>: ALL of the employee's <c>EmployeeOrgAssignment</c> rows,
    /// loaded once per employee (legacy <c>Program.cs</c> line 783) and read 7 times from
    /// <c>EmployeeCalculatedPayService</c> alone. Carried here rather than re-queried per rule for the reason
    /// given in the class remarks: legacy loaded it exactly once, and the engine mutates nothing on it
    /// mid-run.
    ///
    /// <para><see cref="PayrollArea"/> is the pre-resolved answer to the single most common question asked of
    /// this list ("which payroll area covers <see cref="PayInDate"/>?"); prefer it where it suffices.</para>
    /// </summary>
    public List<EmployeeOrgAssignment> OrgAssignments { get; set; } = new List<EmployeeOrgAssignment>();

    /// <summary>
    /// Legacy <c>prevSalRegList</c>: registers of the SAME run type whose <c>ForPeriod</c> is the previous
    /// month end (legacy <c>Program.cs</c> lines 1900-1905).
    /// </summary>
    public List<SalaryRegister> PreviousSalaryRegisters { get; set; } = new List<SalaryRegister>();

    /// <summary>
    /// Legacy <c>currentSalRegisters</c>: this run's OTHER retro registers -- <c>InPeriod == payInDate</c>,
    /// <c>ForPeriod != payInDate</c>, <c>PayrollResultType.Current</c>, same run type, ordered by
    /// <c>ForPeriod</c> (legacy <c>Program.cs</c> lines 1907-1912).
    /// </summary>
    public List<SalaryRegister> CurrentSalaryRegisters { get; set; } = new List<SalaryRegister>();

    // ---------------------------------------------------------------------------------------------
    // Gates and run configuration
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// Legacy's seven <c>onHold_*</c> parameters. <b>Rules should NOT re-test these:</b> the dispatcher
    /// (<see cref="IWageTypeCalculatorResolver"/>) already applies legacy's <c>IsOnHold</c> earnings/
    /// deductions gate before invoking a calculator. The object is still exposed because a handful of legacy
    /// branches gate on a SPECIFIC hold rather than the generic one -- e.g. Air Ticket Pay tests
    /// <c>onHold_AirTicketPay</c> (legacy <c>EmployeeCalculatedPayService.cs</c> line 62), Loan Recovery tests
    /// <c>onHold_LoanDeduction</c> (line 76), Indemnity Pay tests <c>onHold_IndemnityPay</c> (line 177).
    /// </summary>
    public PayrollOnHoldFlags OnHold { get; set; } = new PayrollOnHoldFlags();

    /// <summary>
    /// Legacy <c>IsNotReturnFromLeave</c>. When true, legacy's <c>EmployeeCalculatedPayService.Main</c>
    /// SKIPS its entire body for every wage type except <c>BillingType.Provision</c> ones and 9000 Net Pay
    /// (legacy line 35). That gate is applied by the dispatcher, not by each rule.
    /// </summary>
    public bool IsNotReturnFromLeave { get; set; }

    /// <summary>
    /// Legacy <c>empPayrollArea</c>: the payroll area from the employee's org assignment covering
    /// <see cref="PayInDate"/> (legacy <c>Program.cs</c> line 832). See
    /// <see cref="IEmployeePayrollAreaResolver"/> for how this port resolves it, since
    /// <c>EmployeeOrgAssignment</c> carries no <c>PayrollAreaId</c> FK.
    /// </summary>
    public PayrollArea PayrollArea { get; set; }

    /// <summary>
    /// Legacy <c>advancePayComment</c>: a human-readable multi-line summary of the advance-leave records
    /// driving an off-cycle advance payment, stamped onto generated <c>EmployeePayment</c> rows. Built once
    /// per employee at legacy <c>Program.cs</c> lines 1724-1733; empty string on every other run type.
    /// </summary>
    public string AdvancePayComment { get; set; } = string.Empty;

    /// <summary>
    /// The run-wide configuration snapshot (legacy's <c>Program</c> static config flags). Read flags from
    /// here; do NOT inject <see cref="SystemSettings.Services.ISysSettingsAccessor"/> into a wage rule --
    /// see <see cref="PayrollEngineSettings"/>.
    /// </summary>
    public PayrollEngineSettings Settings { get; set; } = new PayrollEngineSettings();
}
