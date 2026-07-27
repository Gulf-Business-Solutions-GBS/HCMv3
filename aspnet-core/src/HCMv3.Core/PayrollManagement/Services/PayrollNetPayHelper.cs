using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// Legacy <c>EmployeeCalculatedPayService.GetNetPay(salaryRegister, objectSpace)</c> (legacy
/// <c>HCM10.PayrollEngine/EmployeeCalculatedPayService.cs</c> lines 4221-4232):
/// <code>
/// sum(Amount where BillingType == Earning   &amp;&amp; !DontImpactNetPay)
/// - sum(Amount where BillingType == Deduction &amp;&amp; !DontImpactNetPay)
/// </code>
///
/// <para>Called from FOUR places in legacy, three of which are mid-calculation rather than at the end:
/// the 9000 Net Pay branch (line 1077), <c>GetCompanyInsuranceRecoveryDed</c> (line 3302),
/// <c>GetEmployeeInsuranceRecoveryDed</c> (line 3479) and <c>GetFundDed</c> (line 3724). The last three use
/// it as a "how much net pay is left to deduct against" budget, which is why <b>wage-type ordering matters</b>
/// -- see the wave P6 report's ordering notes.</para>
///
/// <para>Note <c>BillingType.Provision</c> and <c>BillingType.TimeType</c> lines are excluded by construction:
/// legacy tests for Earning and Deduction explicitly, never "not deduction".</para>
/// </summary>
public static class PayrollNetPayHelper
{
    public static async Task<double> GetNetPayAsync(
        IPayrollRegisterStore store,
        PayrollRegisterRef register,
        IRepository<WageType> wageTypeRepository)
    {
        var lines = await store.GetLinesAsync(register);
        if (lines.Count == 0)
        {
            return 0;
        }

        var wageTypes = wageTypeRepository.GetAll()
            .Select(w => new { w.Id, w.BillingType, w.DontImpactNetPay })
            .ToList()
            .ToDictionary(w => w.Id, w => w);

        double earnings = 0;
        double deductions = 0;

        foreach (var line in lines)
        {
            if (!wageTypes.TryGetValue(line.WageTypeId, out var wageType) || wageType.DontImpactNetPay)
            {
                continue;
            }

            if (wageType.BillingType == BillingType.Earning)
            {
                earnings += line.Amount;
            }
            else if (wageType.BillingType == BillingType.Deduction)
            {
                deductions += line.Amount;
            }
        }

        return earnings - deductions;
    }
}
