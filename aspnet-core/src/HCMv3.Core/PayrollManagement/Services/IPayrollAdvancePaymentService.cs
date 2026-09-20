using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Domain.Services;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// The "Advance Payment Ded (WageCode : 2005)" sub-block that legacy copy-pastes into ~12 wage-type branches
/// of <c>HCM10.PayrollEngine/EmployeeCalculatedPayService.cs</c> (lines 105-111, 163-167, 559-563, 602-606,
/// 645-649, 687-693, 731-737, 775-781, 819-825, 865-871, 911-917, 957-963, 1000-1006, 1044-1050, 3166-3172,
/// 4174-4179), plus legacy <c>Program.GetRecoveryPeriods</c> (legacy <c>Program.cs</c> lines 2922-2939).
///
/// <para>Factored out here (wave P6 instruction 5) instead of being duplicated per rule. It is deliberately
/// NOT an <see cref="IWageTypeCalculator"/>: nothing dispatches on wage code 2005 -- the engine clears 2005
/// lines before an off-cycle run (see <see cref="PayrollEngineWageCodes.AdvancePaymentDeduction"/>) and the
/// individual earning/deduction rules accumulate into it as a side effect.</para>
///
/// <para><b>The four clearing operations below are what makes a payroll RE-RUN safe.</b>
/// <see cref="AdvancePaymentDedAsync"/> ACCUMULATES onto its row, so without them a second off-cycle run for
/// the same payment date recovers twice what it advanced. Each mirrors exactly one legacy block; see each
/// method for which. They were the last unported part of the off-cycle path.</para>
/// </summary>
public interface IPayrollAdvancePaymentService : IDomainService
{
    /// <summary>
    /// Legacy <c>Program.AdvancePaymentDed</c> (legacy <c>Program.cs</c> lines 2940-2988). Accumulates
    /// <paramref name="amount"/> onto the employee's wage-code-2005 <c>EmployeePayment</c> row for
    /// <paramref name="paymentDate"/>, creating it when absent.
    ///
    /// <para>Legacy no-ops when <paramref name="amount"/> is 0, and returns immediately on an End-Of-Service
    /// off-cycle run (legacy lines 2942-2947). Both preserved.</para>
    ///
    /// <para>The running total is rounded with
    /// <c>Math.Round(existing + amount, PayrollEngineSettings.DecimalInSalaryRegister)</c> -- legacy line
    /// 2982 uses <c>Program.decimalInSalaryRegister</c>.</para>
    ///
    /// <para>The existing row is matched on legacy's FULL key
    /// <c>Employee + PaymentDate + OffCyclePaymentDate + WageType.Code == "2005"</c> (wave P8 restored
    /// <c>EmployeePayment.OffCyclePaymentDate</c>). Matching without <c>OffCyclePaymentDate</c> merges
    /// recovery rows across different off-cycle runs and over-deducts -- do not "simplify" it back.
    /// <paramref name="offCyclePaymentDate"/> is <c>default(DateTime)</c> on a regular run (legacy's own
    /// sentinel) and is normalised to <c>null</c> in the column.</para>
    ///
    /// <para>Legacy's <c>SysCreator</c>/<c>SysModifier</c> stamping is superseded by ABP's
    /// <c>FullAuditedEntity</c>, but <c>IsSystemGenerated</c> (legacy line 2983) is BUSINESS data and is
    /// stamped <c>true</c>, exactly as legacy does.</para>
    /// </summary>
    Task AdvancePaymentDedAsync(
        int employeeId,
        DateTime paymentDate,
        DateTime offCyclePaymentDate,
        double amount,
        PayrollRunType payrollRunType,
        string offCycleReason,
        string advancePayComment,
        PayrollEngineSettings settings);

    /// <summary>
    /// Legacy <c>Program.GetRecoveryPeriods(payDate, offCycleEndDate)</c> (legacy <c>Program.cs</c> lines
    /// 2922-2939): the distinct first-of-month dates from <paramref name="payStartDate"/>'s month through
    /// <paramref name="payEndDate"/>, inclusive.
    /// </summary>
    List<DateTime> GetRecoveryPeriods(DateTime payStartDate, DateTime payEndDate);

    /// <summary>
    /// Legacy <c>Program.DeleteAdvancePaymentDed</c> (legacy <c>Program.cs</c> lines 2988-3000), called once
    /// per employee from the off-cycle arm BEFORE the period loop (legacy line 1201). HARD-DELETES every
    /// wage-code-2005 <c>EmployeePayment</c> row of this employee carrying this
    /// <paramref name="offCyclePaymentDate"/>, whatever their payment date.
    ///
    /// <para>This is the one that makes an off-cycle RE-RUN correct: the run is about to post fresh recovery
    /// rows with <see cref="AdvancePaymentDedAsync"/>, which accumulates, so the previous run's rows must go
    /// first or the employee is recovered twice.</para>
    ///
    /// <para><b>Legacy does NOT filter on run type</b> even though it takes one as a parameter, and does not
    /// exempt End-Of-Service. Both preserved -- the only call site is the off-cycle arm, so the run type is
    /// off-cycle by construction, and an End-Of-Service run never created 2005 rows to begin with.</para>
    ///
    /// <para>Legacy sets <c>IsSystemGenerated = true</c> on each row immediately before deleting it (legacy
    /// line 2996) -- pointless, and not reproduced. ABP soft-deletes (<c>FullAuditedEntity</c>), where legacy's
    /// XPO <c>Delete()</c> was a hard delete; the rows stop being read either way, and the soft-delete leaves
    /// the audit trail legacy lacked.</para>
    /// </summary>
    /// <returns>How many rows were deleted -- the engine logs it; callers may ignore it.</returns>
    Task<int> DeleteAdvancePaymentDedAsync(int employeeId, DateTime offCyclePaymentDate);

    /// <summary>
    /// Legacy's Regular-run "Clear EmployeePayment Advance recovery" block (legacy <c>Program.cs</c> lines
    /// 1619-1653). ZEROES (does not delete) the employee's wage-code-2005 rows for FUTURE months that were
    /// raised by an off-cycle whose payment date was <paramref name="payDate"/> -- but ONLY when the employee
    /// no longer has an advance-paid leave on that date.
    ///
    /// <para><b>What it is for:</b> the advance-paid leave was cancelled or its payment date moved after the
    /// off-cycle ran. The advance is still being recovered month by month from a leave that no longer exists,
    /// so the remaining months are zeroed. The CURRENT month's row is deliberately left alone
    /// (<c>PaymentDate &gt; payDate</c>): that month's advance was genuinely paid out and must still come
    /// back.</para>
    ///
    /// <para><b>Note how narrow the match is, and that this is legacy's:</b> <c>OffCyclePaymentDate ==
    /// payDate</c> compares against the period being paid, which on a normal regular run is the MONTH END. An
    /// off-cycle paid on the 15th therefore does not match and its recovery is never cleared this way. Ported
    /// verbatim; widening it would zero recoveries legacy still collects.</para>
    ///
    /// <para>Legacy's own <c>if (salaryRegister.InPeriod == salaryRegister.ForPeriod)</c> guard is commented
    /// out at line 1621, so the block runs on retro periods too. Preserved.</para>
    /// </summary>
    /// <param name="hasAdvancePaidLeaveOnPayDate">
    /// Legacy's <c>empAbsences.Any()</c> test (lines 1623-1629): the employee has an absence with a PAID
    /// absence type, <c>LeavePaymentType.PayInAdvance</c> and <c>PaymentDate == payDate</c>. Passed in rather
    /// than queried here because the caller already holds the absence list.
    /// </param>
    Task<int> ZeroFutureAdvanceRecoveryAsync(int employeeId, DateTime payDate, bool hasAdvancePaidLeaveOnPayDate);

    /// <summary>
    /// Legacy's off-cycle "Clear EmployeePayment Advance recovery" block (legacy <c>Program.cs</c> lines
    /// 1796-1819). ZEROES the employee's wage-code-2005 rows whose payment date AND off-cycle payment date
    /// both fall inside this run's window, so the run's own postings start from a clean slate.
    ///
    /// <para>Skipped entirely on an End-Of-Service off-cycle (legacy line 1798), which never raises a
    /// recovery.</para>
    ///
    /// <para>This overlaps <see cref="DeleteAdvancePaymentDedAsync"/> but is not redundant: that one keys on an
    /// EXACT off-cycle payment date across all payment dates, this one on a WINDOW and only for non-EOS runs.
    /// Legacy runs both, in that order, and so does this port.</para>
    /// </summary>
    Task<int> ZeroAdvanceRecoveryInWindowAsync(int employeeId, DateTime payStartDate, DateTime offCycleEndDate);

    /// <summary>
    /// Legacy's off-cycle "Clear EmployeePayment Advance recovery" block for wage code <b>2011</b> "Last Month
    /// Recovery Deduction" (legacy <c>Program.cs</c> lines 1832-1850, comment dated 26.06.2018: "to clear Last
    /// month adjustment if payroll rerun for the same month"). ZEROES the employee's 2011 rows dated the FIRST
    /// OF THE MONTH AFTER <paramref name="payDate"/>.
    ///
    /// <para>Those rows are written by <see cref="NetPayWageTypeCalculator"/> when a month's net pay would have
    /// gone negative: the shortfall is carried into the next month. On a re-run the previous attempt's
    /// carry-forward must be cleared, or the shortfall is collected twice.</para>
    ///
    /// <para>Unlike the 2005 block above, legacy applies NO End-Of-Service exemption here. Preserved.</para>
    /// </summary>
    Task<int> ZeroLastMonthRecoveryAsync(int employeeId, DateTime payDate);
}
