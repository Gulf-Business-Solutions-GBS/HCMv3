using System.Threading.Tasks;
using Abp.Dependency;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// Wave P5 -- wage code <b>3001, Annual Leave Provision</b> (monthly). Ported from legacy
/// <c>HCM10.PayrollEngine/EmployeeCalculatedPayService.cs</c> lines 259-295.
///
/// <para>The calculation body lives in <see cref="IAnnualLeaveProvisionService"/> (legacy
/// <c>GetAnnualLeaveProvision</c>, line 2281); this rule is the branch scaffolding: run-type gate,
/// find-or-create the line, zero-means-delete, then the salary-increment day-wise tail.</para>
///
/// <para><b>Legacy ORDER preserved:</b> the zero-means-delete (legacy lines 283-288) runs BEFORE the day-wise
/// entry (legacy lines 290-292) -- and the day-wise entry may re-create the very line just deleted, exactly
/// as in legacy.</para>
///
/// <para>The day-wise tail is gated on <c>Program.includeSalaryIncProvDiffInSalReg</c> and
/// <c>Program.includeSalaryIncProvDiffInSalRegEffDt &lt; payDate</c> (strictly less-than), read from
/// <see cref="PayrollCalculationContext.Settings"/>.</para>
///
/// <para><b>Preserved legacy quirk:</b> like 3000/3003, this branch tests no on-hold flag at all.</para>
/// </summary>
public class AnnualLeaveProvisionWageTypeCalculator : HCMv3DomainServiceBase, IWageTypeCalculator, ITransientDependency
{
    private readonly IAnnualLeaveProvisionService _annualLeaveProvisionService;
    private readonly IPayrollRegisterStore _registerStore;

    public AnnualLeaveProvisionWageTypeCalculator(
        IAnnualLeaveProvisionService annualLeaveProvisionService,
        IPayrollRegisterStore registerStore)
    {
        _annualLeaveProvisionService = annualLeaveProvisionService;
        _registerStore = registerStore;
    }

    /// <inheritdoc/>
    public string WageCode => "3001";

    /// <inheritdoc/>
    public async Task CalculateAsync(PayrollCalculationContext context)
    {
        // Legacy lines 262-264.
        if (!ProvisionCalculationGates.AllowAdvanceLeaveProvision(context))
        {
            return;
        }

        // Legacy lines 266-274.
        var line = await ProvisionRegisterLineHelper.GetOrCreateLineAsync(_registerStore, context);

        var amounts = await _annualLeaveProvisionService.GetAnnualLeaveProvisionAsync(context);

        // Legacy lines 283-288: zero means delete.
        line.Rate = amounts.Rate;
        line.Number = amounts.Number;
        line.Amount = amounts.Amount;

        await _registerStore.SaveLineAsync(context.Register, line);

        // Legacy lines 290-292.
        if (context.Settings != null
            && context.Settings.IncludeSalaryIncProvDiffInSalReg
            && context.Settings.IncludeSalaryIncProvDiffInSalRegEffDt < context.PayDate)
        {
            await _annualLeaveProvisionService.GetAnnualLeaveProvisionDayWiseEntryAsync(context);
        }
    }
}
