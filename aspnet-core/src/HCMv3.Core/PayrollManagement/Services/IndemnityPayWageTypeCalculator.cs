using System.Threading.Tasks;
using Abp.Dependency;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// Wave P5 -- wage code <b>1010, Indemnity Pay</b>. Ported from legacy
/// <c>HCM10.PayrollEngine/EmployeeCalculatedPayService.cs</c> lines 174-185.
///
/// <para><b>Thin adapter.</b> The entire calculation body is legacy
/// <c>EmployeeCalculatedPayService.GetIndemnityPay</c> (legacy line 1716), already ported in wave P3 as
/// <see cref="IIndemnityCalculationService.GetIndemnityPayAsync"/>. This rule contributes only the two
/// legacy guards that sit AROUND that call in the 1010 branch and nothing else -- it deliberately
/// re-derives no indemnity maths.</para>
///
/// <para><b>Register I/O note.</b> Unlike the 3000/3003 rules, this rule does NOT create the register line
/// itself: legacy's <c>GetIndemnityPay</c> owns the whole create/update decision (including its own
/// "write nothing when amount == 0" rule, which is subtly different from the generic zero-means-delete --
/// it never DELETES an existing line, it just leaves it alone), and wave P3's port kept that ownership
/// inside <see cref="IndemnityCalculationService"/>. Introducing an
/// <see cref="IPayrollRegisterStore"/> round-trip here would change that behaviour.</para>
///
/// <para><b>Preserved legacy asymmetry.</b> The 1010 branch tests <c>onHold_IndemnityPay</c> specifically
/// (legacy line 177) rather than the generic <c>IsOnHold</c> earnings/deductions gate. That per-hold test is
/// this rule's business (see <see cref="WageTypeCalculatorResolver"/> remarks), so it is applied here.</para>
///
/// <para><b>Known port gap (whole-wave, not specific to this rule):</b> legacy derived the employee's
/// <c>EmployeeSubGroup</c> from <c>emp.OrgAssignment</c>; this port's <c>EmployeeOrgAssignment</c> carries no
/// <c>EmployeeSubGroupId</c> FK yet (deferred in Wave B, see that entity's remarks) and
/// <see cref="PayrollCalculationContext"/> carries no sub-group either, so <c>null</c> is passed. Effect:
/// the <c>EmployeeSubGroupGratuity</c> change-year slab lookup inside
/// <see cref="IIndemnityCalculationService.GetIndemnityBasisDaysAsync"/> falls back to the wage type's
/// <c>FixedDays</c>. Identical to the gap already documented on <see cref="IAccrualService"/>.</para>
///
/// <para><b>Known port gap -- indemnity slab cache.</b> Legacy kept the slab table in the run-level static
/// <c>Program.IndemnitySlabList</c>, lazily built on the first End-Of-Service bifurcation and reused for the
/// rest of the run. <see cref="PayrollCalculationContext"/> is documented read-only to rules, so there is
/// nowhere to park the refreshed list returned on <see cref="IndemnityPayResult.IndemnitySlabs"/>;
/// <c>null</c> is passed and the service rebuilds it. Same numbers, more queries.</para>
/// </summary>
public class IndemnityPayWageTypeCalculator : HCMv3DomainServiceBase, IWageTypeCalculator, ITransientDependency
{
    private readonly IIndemnityCalculationService _indemnityCalculationService;

    public IndemnityPayWageTypeCalculator(IIndemnityCalculationService indemnityCalculationService)
    {
        _indemnityCalculationService = indemnityCalculationService;
    }

    /// <inheritdoc/>
    public string WageCode => "1010";

    /// <inheritdoc/>
    public async Task CalculateAsync(PayrollCalculationContext context)
    {
        // Legacy line 177: the 1010 branch gates on the SPECIFIC indemnity hold, not the generic IsOnHold.
        if (context.OnHold != null && context.OnHold.IndemnityPay)
        {
            return;
        }

        // Legacy line 179.
        if (!ProvisionCalculationGates.AllowEndOfServiceProvision(context))
        {
            return;
        }

        await _indemnityCalculationService.GetIndemnityPayAsync(
            context.EmployeeId,
            context.Register.RegisterId,
            context.WageType.Id,
            context.PayDate,
            context.PayrollRunType,
            context.OffCycleReason,
            employeeSubGroupId: null,
            indemnitySlabs: null);
    }
}
