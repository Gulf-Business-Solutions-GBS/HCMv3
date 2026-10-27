using System.Threading.Tasks;
using Abp.Domain.Services;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// One wage-type calculation rule. This is the strategy contract that the ~34 rules of waves P4-P6 implement,
/// replacing the legacy engine's monolithic <c>if (currWageType.Code == "xxxx") ... else if ...</c> chains
/// inside <c>HCM10.PayrollEngine/EmployeeCalculatedPayService.Main</c> (legacy lines 44-1149) and its five
/// sibling services (<c>EmployeeBasicPayService</c>, <c>EmployeeLeavePayService</c>,
/// <c>EmployeeOvertimePayService</c>, <c>EmployeePaymentService</c>, <c>EmployeeAttendancePayService</c>,
/// <c>PayrollArrearService</c>).
///
/// <para><b>Registration.</b> Implementations are conventionally registered by ABP/Castle and injected as
/// <c>IEnumerable&lt;IWageTypeCalculator&gt;</c> into <see cref="IWageTypeCalculatorResolver"/>; adding a rule
/// requires no change to the engine or the resolver. Mark implementations with
/// <c>Abp.Dependency.ITransientDependency</c>. <see cref="IDomainService"/> is inherited so
/// implementations can extend <c>HCMv3DomainServiceBase</c> like every other domain service in this port.</para>
///
/// <para><b>Registration is silent when it fails.</b> A sibling wave (TimeEngine T3,
/// <c>IPunchResolutionStrategy</c>) learned this the hard way: if conventional registration does not match an
/// implementation, the injected <c>IEnumerable</c> is simply EMPTY and every dispatch falls through to the
/// "no calculator for this wage code" arm with no error at all. Wave P1 ships a DI regression test
/// (<c>WageTypeCalculatorRegistration_Tests</c>) that asserts every type in the Core assembly implementing
/// this interface actually resolves from the container. Keep it green.</para>
///
/// <para><b>Gating is the dispatcher's job, not yours.</b> The two legacy helpers
/// <c>EmployeeCalculatedPayService.IsOnHold</c> (legacy line 1160) and
/// <c>EmployeeCalculatedPayService.AllowToCalculate</c> (legacy line 1164), plus legacy's
/// <c>Program.Allow_CalcWageType</c> wage-type-exception check (legacy <c>Program.cs</c> line 2752) and the
/// <c>IsNotReturnFromLeave</c> gate (legacy line 35), are all applied ONCE by
/// <see cref="IWageTypeCalculatorResolver"/> before <see cref="CalculateAsync"/> is called. Do not re-implement
/// them per rule -- that is precisely how the legacy code accumulated 34 slightly-divergent copies of the
/// same guard.</para>
///
/// <para><b>Register I/O.</b> Read and write register lines exclusively through
/// <see cref="IPayrollRegisterStore"/> using <see cref="PayrollCalculationContext.Register"/>. Never take a
/// direct dependency on <see cref="SalaryRegisterDetail"/>: wave P7 collapses the legacy
/// <c>SimulationPayrollHelper.cs</c> clone by flipping
/// <see cref="PayrollRegisterRef.Target"/>, and that only stays trivial while the store is the single
/// concrete-entity boundary.</para>
///
/// <para><b>Legacy "zero means delete".</b> Most legacy calculated-pay branches create a detail row, run the
/// calculation into it, and then <c>Delete()</c> it when <c>Amount == 0</c> (e.g. legacy
/// <c>EmployeeCalculatedPayService.cs</c> lines 211-216 for 3000 Indemnity Provision, 421-426, 456-461).
/// <see cref="IPayrollRegisterStore.SaveLineAsync"/> reproduces that convention -- see its remarks.</para>
/// </summary>
public interface IWageTypeCalculator : IDomainService
{
    /// <summary>
    /// The <see cref="WageType.Code"/> this rule serves (legacy's <c>currWageType.Code</c> literal, e.g.
    /// "1006", "2000", "3000", "9000"). Matched case-sensitively by
    /// <see cref="IWageTypeCalculatorResolver"/>, exactly like legacy's string equality.
    ///
    /// <para>Codes are unique: two calculators claiming the same code is a configuration error and the
    /// resolver throws rather than silently picking one (legacy could not express the ambiguity at all, since
    /// its branches were literal <c>else if</c>s).</para>
    /// </summary>
    string WageCode { get; }

    /// <summary>
    /// Runs this wage type's calculation for one employee and one pay period, writing its result into
    /// <c>context.Register</c> via <see cref="IPayrollRegisterStore"/>.
    ///
    /// <para><paramref name="context"/> is owned by the engine and reused across the wage-type loop; treat
    /// every member except <see cref="PayrollCalculationContext.WageType"/> as read-only.</para>
    ///
    /// <para>Throwing aborts the whole employee (legacy behaviour: the exception propagates out of
    /// <c>RunPayrollCalculation</c>, is wrapped with "Error in calculating WageType '{code} : {name}'" at
    /// legacy <c>Program.cs</c> lines 2594-2599, logged against the employee, and the run continues with the
    /// next employee). Throw a real exception for a real data problem; never swallow one to "keep going".</para>
    /// </summary>
    Task CalculateAsync(PayrollCalculationContext context);
}

/// <summary>
/// Picks and invokes the <see cref="IWageTypeCalculator"/> for the wage type currently being processed,
/// applying every gate legacy applied before entering a wage-type branch. Reproduces, in one place:
///
/// <list type="number">
/// <item><description><c>Program.Allow_CalcWageType</c> (legacy <c>Program.cs</c> lines 2752-2770): skip the
/// wage type when any of its <see cref="WageTypeException"/> partners already has a non-zero
/// Rate/Number/Amount line on this register.</description></item>
/// <item><description>The <c>IsNotReturnFromLeave</c> gate (legacy
/// <c>EmployeeCalculatedPayService.cs</c> line 35): when the employee has not returned from leave, skip every
/// wage type except <c>BillingType.Provision</c> ones and 9000 Net Pay.</description></item>
/// <item><description><c>EmployeeCalculatedPayService.IsOnHold</c> (legacy line 1160): skip Earning wage types
/// when all earnings are on hold, and Deduction wage types when all deductions are on hold.</description></item>
/// <item><description><c>EmployeeCalculatedPayService.AllowToCalculate</c> (legacy line 1164): skip when the
/// employee has an un-revoked separation whose <c>LastDayInService</c> month-end is before the period being
/// calculated.</description></item>
/// </list>
/// </summary>
public interface IWageTypeCalculatorResolver : IDomainService
{
    /// <summary>
    /// Returns the calculator registered for <paramref name="wageCode"/>, or <c>null</c> when no rule claims
    /// it. <c>null</c> is a normal, expected outcome: the legacy engine iterated EVERY active non-TimeType
    /// wage type and simply fell through its <c>else if</c> chain for codes it had no branch for.
    /// </summary>
    IWageTypeCalculator Resolve(string wageCode);

    /// <summary>
    /// Legacy's <c>wtCalculatedWageList.Contains(currWageType.Oid)</c> membership test (legacy
    /// <c>Program.cs</c> line 2574) -- i.e. "does the calculated-pay arm own this wage type?", asked BEFORE
    /// any gate runs so the engine knows not to fall through to the attendance arm.
    ///
    /// <para>This is <see cref="Resolve"/> by code OR a claim from any
    /// <see cref="IMasterDataDispatchedWageTypeCalculator"/>. It exists because the by-code test alone is
    /// not the whole of legacy's list: legacy also adds every <c>DisciplinaryType</c>/<c>TicketType</c>/
    /// <c>LoanType</c>/<c>TravelType</c> wage type to it (legacy <c>Program.cs</c> lines 2698-2701).</para>
    /// </summary>
    bool HasCalculatorFor(WageType wageType);

    /// <summary>
    /// Applies gates 1-4 above and then, if all pass, invokes -- in legacy's own order (legacy
    /// <c>EmployeeCalculatedPayService.Main</c> runs its master-table regions at lines 44-130, before the
    /// by-code branches from line 131 on):
    /// <list type="number">
    /// <item><description>every <see cref="IMasterDataDispatchedWageTypeCalculator"/> whose master table
    /// claims <c>context.WageType.Id</c>;</description></item>
    /// <item><description>the single by-code calculator for <c>context.WageType.Code</c>, if any.</description></item>
    /// </list>
    /// </summary>
    /// <returns>
    /// <c>true</c> if at least one calculator ran; <c>false</c> if the wage type was gated out or unclaimed.
    /// </returns>
    Task<bool> TryCalculateAsync(PayrollCalculationContext context);
}
