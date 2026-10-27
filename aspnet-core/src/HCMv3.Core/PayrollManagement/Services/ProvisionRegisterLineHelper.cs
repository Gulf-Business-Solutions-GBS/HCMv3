using System;
using System.Threading.Tasks;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// Wave P5. The "find the register line for the current wage type, or create it with zeros" step that
/// legacy repeated verbatim at the head of every provision branch (legacy
/// <c>HCM10.PayrollEngine/EmployeeCalculatedPayService.cs</c> lines 194-202, 228-236, 266-274, 304-312,
/// 336-344, 368-376 -- six identical copies).
///
/// <para>Kept as a helper rather than a base class so that each rule stays a flat, independently registered
/// <see cref="IWageTypeCalculator"/> (an abstract base would still be excluded from the DI regression test's
/// reflection scan, but a shared base tends to accumulate exactly the divergence this port is trying to
/// remove).</para>
///
/// <para>The zero placeholder is written with <c>deleteWhenAmountIsZero: false</c> precisely BECAUSE the row
/// must exist for the wave-P3 provision services, which take a <c>salaryRegisterDetailId</c>. The caller is
/// responsible for the follow-up <see cref="IPayrollRegisterStore.SaveLineAsync"/> that re-applies legacy's
/// zero-means-delete once the amount is known.</para>
/// </summary>
internal static class ProvisionRegisterLineHelper
{
    public static async Task<PayrollRegisterLine> GetOrCreateLineAsync(IPayrollRegisterStore registerStore, PayrollCalculationContext context)
    {
        if (context?.WageType == null)
        {
            throw new ArgumentException("PayrollCalculationContext.WageType must be set.", nameof(context));
        }

        var line = await registerStore.GetLineByWageTypeIdAsync(context.Register, context.WageType.Id);
        if (line != null)
        {
            return line;
        }

        // Legacy created the object with Rate/Number left at their defaults (the explicit `= 0` assignments
        // are commented out in the legacy source) and did NOT set Amount either.
        return await registerStore.SaveLineAsync(
            context.Register,
            new PayrollRegisterLine
            {
                WageTypeId = context.WageType.Id,
                WageCode = context.WageType.Code,
                Rate = 0,
                Number = 0,
                Amount = 0
            },
            deleteWhenAmountIsZero: false);
    }
}
