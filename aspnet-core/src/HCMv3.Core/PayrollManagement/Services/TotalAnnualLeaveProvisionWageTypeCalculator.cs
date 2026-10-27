using System.Threading.Tasks;
using Abp.Dependency;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// Wave P5 -- wage code <b>3004, Total Annual Leave Provision</b> (closing). Ported from legacy
/// <c>HCM10.PayrollEngine/EmployeeCalculatedPayService.cs</c> lines 297-329.
///
/// <para>The calculation body lives in <see cref="IAnnualLeaveProvisionService"/> (legacy
/// <c>GetTotalAnnualLeaveProvision</c>, line 2317). Unlike its 3001 sibling this branch has NO day-wise tail
/// -- legacy attaches the salary-increment catch-up to 3001 for annual leave, but to 3003 (not 3000) for
/// indemnity. That asymmetry is legacy's and is preserved.</para>
///
/// <para><b>Preserved legacy quirk:</b> like 3000/3001/3003, this branch tests no on-hold flag at all.</para>
/// </summary>
public class TotalAnnualLeaveProvisionWageTypeCalculator : HCMv3DomainServiceBase, IWageTypeCalculator, ITransientDependency
{
    private readonly IAnnualLeaveProvisionService _annualLeaveProvisionService;
    private readonly IPayrollRegisterStore _registerStore;

    public TotalAnnualLeaveProvisionWageTypeCalculator(
        IAnnualLeaveProvisionService annualLeaveProvisionService,
        IPayrollRegisterStore registerStore)
    {
        _annualLeaveProvisionService = annualLeaveProvisionService;
        _registerStore = registerStore;
    }

    /// <inheritdoc/>
    public string WageCode => "3004";

    /// <inheritdoc/>
    public async Task CalculateAsync(PayrollCalculationContext context)
    {
        // Legacy lines 300-302.
        if (!ProvisionCalculationGates.AllowAdvanceLeaveProvision(context))
        {
            return;
        }

        // Legacy lines 304-312.
        var line = await ProvisionRegisterLineHelper.GetOrCreateLineAsync(_registerStore, context);

        var amounts = await _annualLeaveProvisionService.GetTotalAnnualLeaveProvisionAsync(context);

        // Legacy lines 321-326: zero means delete.
        line.Rate = amounts.Rate;
        line.Number = amounts.Number;
        line.Amount = amounts.Amount;

        await _registerStore.SaveLineAsync(context.Register, line);
    }
}
