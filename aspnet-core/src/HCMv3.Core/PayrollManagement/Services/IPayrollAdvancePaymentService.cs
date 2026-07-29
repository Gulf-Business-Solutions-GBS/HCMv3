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
}
