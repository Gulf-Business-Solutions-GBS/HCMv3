using System.Threading.Tasks;
using Abp.Dependency;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// Wave P5 -- wage code <b>3000, Indemnity Provision</b> (monthly). Ported from legacy
/// <c>HCM10.PayrollEngine/EmployeeCalculatedPayService.cs</c> lines 187-219.
///
/// <para><b>Thin adapter.</b> The calculation body is legacy
/// <c>EmployeeCalculatedPayService.GetIndemnityProvision</c> (legacy line 2610), already ported in wave P3 as
/// <see cref="IIndemnityCalculationService.GetIndemnityProvisionAsync"/>. This rule contributes only the
/// legacy branch scaffolding: the run-type gate, the "find-or-create the 3000 line" step, and the
/// zero-means-delete afterwards.</para>
///
/// <para><b>Why the line is created BEFORE the calculation.</b> Legacy created the
/// <c>SalaryRegisterDetail</c>, handed it to <c>GetIndemnityProvision</c> (which writes Rate/Number/Amount
/// onto it by reference) and only then deleted it if the amount came out 0. Wave P3's port keeps that shape
/// -- <see cref="IIndemnityCalculationService.GetIndemnityProvisionAsync"/> takes a
/// <c>salaryRegisterDetailId</c> and requires the row to exist. So this rule persists a zero placeholder
/// through <see cref="IPayrollRegisterStore.SaveLineAsync"/> with
/// <c>deleteWhenAmountIsZero: false</c>, then re-saves with <c>true</c> to apply legacy's delete. Net effect
/// on the database is identical to legacy; the only difference is one extra write for the (rare) case where
/// the provision lands on exactly 0 and the row is created then removed inside the same unit of work.</para>
///
/// <para><b>Preserved legacy quirk (verified in wave P3, do not "fix"):</b> legacy's
/// <c>GetIndemnityProvision</c> ALWAYS returns 0 (it declares <c>double amount = 0</c>, assigns the real
/// figure to <c>salaryRegisterDetail.Amount</c> instead, and returns the untouched local). The real posted
/// amount is therefore taken from <see cref="IndemnityProvisionResult.Amount"/>, never from
/// <see cref="IndemnityProvisionResult.LegacyReturnValue"/>.</para>
///
/// <para><b>Preserved legacy quirk:</b> the 3000/3003 branches test NEITHER <c>IsOnHold</c> nor
/// <c>onHold_IndemnityPay</c> (unlike 1010, legacy line 177). No hold test is applied here.</para>
///
/// <para>For the <c>employeeSubGroupId == null</c> and slab-cache gaps see
/// <see cref="IndemnityPayWageTypeCalculator"/>; they apply identically.</para>
/// </summary>
public class IndemnityProvisionWageTypeCalculator : HCMv3DomainServiceBase, IWageTypeCalculator, ITransientDependency
{
    private readonly IIndemnityCalculationService _indemnityCalculationService;
    private readonly IPayrollRegisterStore _registerStore;

    public IndemnityProvisionWageTypeCalculator(
        IIndemnityCalculationService indemnityCalculationService,
        IPayrollRegisterStore registerStore)
    {
        _indemnityCalculationService = indemnityCalculationService;
        _registerStore = registerStore;
    }

    /// <inheritdoc/>
    public string WageCode => "3000";

    /// <inheritdoc/>
    public async Task CalculateAsync(PayrollCalculationContext context)
    {
        // Legacy lines 190-192.
        if (!ProvisionCalculationGates.AllowAdvanceLeaveProvision(context))
        {
            return;
        }

        // Legacy line 194-202: find the line for the CURRENT wage type, create it when absent.
        var line = await ProvisionRegisterLineHelper.GetOrCreateLineAsync(_registerStore, context);

        var result = await _indemnityCalculationService.GetIndemnityProvisionAsync(
            context.EmployeeId,
            line.Id,
            context.PayDate,
            context.PayrollRunType,
            context.OffCycleReason,
            employeeSubGroupId: null,
            indemnitySlabs: null);

        // Legacy lines 211-216: zero means delete.
        line.Rate = result.Rate;
        line.Number = result.Number;
        line.Amount = result.Amount;

        await _registerStore.SaveLineAsync(context.Register, line);
    }
}
