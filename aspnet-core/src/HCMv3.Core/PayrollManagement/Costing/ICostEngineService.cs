using System.Threading.Tasks;
using Abp.Domain.Services;

namespace HCMv3.PayrollManagement.Costing;

/// <summary>
/// The cost engine: turns a completed <see cref="SalaryRegister"/> into <see cref="CostRegister"/> /
/// <see cref="CostRegisterDetail"/> rows, splitting each wage-type amount across the employee's configured
/// cost-element combinations.
///
/// <para>Ported from legacy <c>HCM10.Web/UserControls/PayrollManagement/uscCostingFile.ascx.cs</c>
/// <c>CreateCostRegister(...)</c>, lines 397-703. This exists ONLY in the costing-file screen;
/// <c>uscPostJV</c> consumes the cost register but never produces it.</para>
///
/// <para><b>The allocation rule (legacy line 590).</b> For each wage type carried on the salary register with
/// <c>WageType.IncludeInFIPosting == true</c>, the engine sums that wage type's register amount and then
/// emits ONE <see cref="CostRegisterDetail"/> per matching <see cref="EmployeeWageAccount"/> row, each
/// carrying <c>Amount = (totalAmount * empWageAccount.CostPercentage) / 100</c> and that row's seven
/// cost-element slots. Several wage-account rows for the same employee+wage type with percentages summing to
/// 100 is how a cost SPLIT is expressed.</para>
///
/// <para><b>Rounding remainder: legacy leaves it.</b> Legacy performs NO rounding and NO
/// largest-remainder redistribution on the split (line 590 is a bare multiply/divide on
/// <c>double</c>). If the configured percentages do not sum to exactly 100, the emitted details do not sum
/// to the register amount, and legacy silently accepts that. This port preserves the behaviour verbatim --
/// introducing a remainder-correction pass here would change every number finance currently reconciles
/// against. The consequence is confined: any residue shows up in the JV's Net Pay line, which is computed
/// from the cost-register details rather than from the salary register.</para>
///
/// <para><b>Arrear folding (legacy line 539).</b> A wage type with <c>IncludeInArrearPay == true</c> is
/// replaced by its <c>ArrearSourceWageType</c> before account lookup, so arrears post to the same GL account
/// as the wage type they are arrears of.</para>
/// </summary>
public interface ICostEngineService : IDomainService
{
    /// <summary>
    /// Runs the cost engine for every employee in the payroll area (or the explicitly listed employee
    /// codes). Existing cost registers for the same employee/period/run type are DELETED and rebuilt
    /// (legacy lines 486-507).
    /// </summary>
    Task<CostingRunResult> RunAsync(CostingRunInput input);

    /// <summary>
    /// The same run, reporting each finished employee to <paramref name="observer"/> as it happens and
    /// asking it before each employee whether to stop. A <c>null</c> observer behaves exactly like
    /// <see cref="RunAsync(CostingRunInput)"/>.
    /// </summary>
    Task<CostingRunResult> RunAsync(CostingRunInput input, ICostingRunObserver observer);
}
