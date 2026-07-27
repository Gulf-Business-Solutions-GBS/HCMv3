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
    /// <para><b>PORT DEVIATION (schema).</b> Legacy matches the existing row on
    /// <c>Employee + PaymentDate + OffCyclePaymentDate + WageType.Code == "2005"</c>. This port's
    /// <c>EmployeePayment</c> entity DROPPED <c>OffCyclePaymentDate</c> deliberately (see that entity's XML
    /// remarks: "the internal-only OffCyclePaymentDate field (hidden from every legacy view) [is] dropped"),
    /// so the match is on <c>Employee + PaymentDate + 2005</c> only. Within a single off-cycle run this is
    /// equivalent, because <c>offCyclePaymentDate</c> is constant for the run; it differs only if two
    /// off-cycle runs with different payment dates target the same <c>paymentDate</c> month, where legacy
    /// would keep two rows and this port merges them into one. Restoring exact parity requires adding
    /// <c>OffCyclePaymentDate</c> back to the entity plus a migration -- reported, not done.</para>
    ///
    /// <para><b>PORT DEVIATION (auditing).</b> Legacy's <c>SysCreator</c>/<c>SysModifier</c>/
    /// <c>IsSystemGenerated</c> stamping is dropped: ABP's <c>FullAuditedEntity</c> covers auditing and this
    /// port's <c>EmployeePayment</c> carries no <c>IsSystemGenerated</c> column.</para>
    /// </summary>
    Task AdvancePaymentDedAsync(
        int employeeId,
        DateTime paymentDate,
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
}
