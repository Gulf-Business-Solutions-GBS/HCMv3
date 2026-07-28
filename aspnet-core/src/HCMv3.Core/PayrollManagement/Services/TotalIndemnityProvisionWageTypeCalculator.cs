using System.Threading.Tasks;
using Abp.Dependency;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// Wave P5 -- wage code <b>3003, Total Indemnity Provision</b> (closing). Ported from legacy
/// <c>HCM10.PayrollEngine/EmployeeCalculatedPayService.cs</c> lines 221-257.
///
/// <para><b>Thin adapter.</b> Both calculation bodies are already ported in wave P3:
/// <see cref="IIndemnityCalculationService.GetTotalIndemnityProvisionAsync"/> (legacy
/// <c>GetTotalIndemnityProvision</c>, line 2643) and
/// <see cref="IIndemnityCalculationService.GetIndemnityProvisionDayWiseEntryAsync"/> (legacy
/// <c>GetIndemnityProvisionDayWiseEntry</c>, line 2743). This rule contributes only the branch scaffolding.</para>
///
/// <para><b>Legacy ORDER is significant and preserved:</b> the zero-means-delete on the 3003 line runs FIRST
/// (legacy lines 245-250), and only then the salary-increment day-wise catch-up (legacy lines 252-254) --
/// which operates on the <b>3000</b> line, not 3003, and may itself create that 3000 line. Reversing the two
/// would let the day-wise pass see a 3003 row legacy had already deleted.</para>
///
/// <para><b>Preserved legacy quirk (verified in wave P3):</b> unlike its 3000 sibling,
/// <c>GetTotalIndemnityProvision</c> DOES assign and return the real posted amount. Both quirks are
/// test-pinned; neither is a bug to be harmonised.</para>
///
/// <para>The day-wise tail is additionally gated on the two run settings legacy read as
/// <c>Program.includeSalaryIncProvDiffInSalReg</c> and <c>Program.includeSalaryIncProvDiffInSalRegEffDt</c>,
/// taken from <see cref="PayrollCalculationContext.Settings"/> (never re-read from
/// <c>ISysSettingsAccessor</c> -- see <see cref="PayrollEngineSettings"/>). Note legacy's comparison is
/// STRICTLY less-than against <c>payDate</c>.</para>
///
/// <para>For the <c>employeeSubGroupId == null</c> and slab-cache gaps see
/// <see cref="IndemnityPayWageTypeCalculator"/>; they apply identically.</para>
/// </summary>
public class TotalIndemnityProvisionWageTypeCalculator : HCMv3DomainServiceBase, IWageTypeCalculator, ITransientDependency
{
    private readonly IIndemnityCalculationService _indemnityCalculationService;
    private readonly IPayrollRegisterStore _registerStore;
    private readonly IEmployeeSubGroupResolver _employeeSubGroupResolver;

    public TotalIndemnityProvisionWageTypeCalculator(
        IIndemnityCalculationService indemnityCalculationService,
        IPayrollRegisterStore registerStore,
        IEmployeeSubGroupResolver employeeSubGroupResolver)
    {
        _indemnityCalculationService = indemnityCalculationService;
        _registerStore = registerStore;
        _employeeSubGroupResolver = employeeSubGroupResolver;
    }

    /// <inheritdoc/>
    public string WageCode => "3003";

    /// <inheritdoc/>
    public async Task CalculateAsync(PayrollCalculationContext context)
    {
        // Legacy lines 224-226.
        if (!ProvisionCalculationGates.AllowAdvanceLeaveProvision(context))
        {
            return;
        }

        // Legacy lines 228-236.
        var line = await ProvisionRegisterLineHelper.GetOrCreateLineAsync(_registerStore, context);

        var employeeSubGroupId = await _employeeSubGroupResolver
            .GetEmployeeSubGroupIdAsync(context.EmployeeId, context.PayDate);

        var result = await _indemnityCalculationService.GetTotalIndemnityProvisionAsync(
            context.EmployeeId,
            line.Id,
            context.PayDate,
            context.PayrollRunType,
            context.OffCycleReason,
            employeeSubGroupId,
            indemnitySlabs: null);

        // Legacy lines 245-250: zero means delete. Runs BEFORE the day-wise tail, as in legacy.
        line.Rate = result.Rate;
        line.Number = result.Number;
        line.Amount = result.Amount;

        await _registerStore.SaveLineAsync(context.Register, line);

        // Legacy lines 252-254: add the previous month's indemnity-provision difference when the employee
        // got an increment inside the current period. Operates on the 3000 line.
        if (context.Settings != null
            && context.Settings.IncludeSalaryIncProvDiffInSalReg
            && context.Settings.IncludeSalaryIncProvDiffInSalRegEffDt < context.PayDate)
        {
            await _indemnityCalculationService.GetIndemnityProvisionDayWiseEntryAsync(
                context.EmployeeId,
                context.Register.RegisterId,
                context.PayDate);
        }
    }
}
