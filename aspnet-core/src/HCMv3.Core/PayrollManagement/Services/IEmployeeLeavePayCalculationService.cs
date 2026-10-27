using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Domain.Services;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// Port of legacy <c>HCM10.PayrollEngine/EmployeeLeavePayService.Main</c> (legacy lines 17-956).
///
/// <para><b>Why this is NOT an <see cref="IWageTypeCalculator"/>.</b> Same reason as
/// <see cref="IEmployeeBasicPayCalculationService"/>: legacy dispatched here via
/// <c>wtLeavePayWageList.Contains(currWageType.Oid)</c> (legacy <c>Program.cs</c> line 2555), and that list is
/// config-driven, not compile-time --
/// <c>wtLeavePayWageList = AbsenceTypeList.Select(r =&gt; r.WageType.Oid).ToList()</c> plus the single wage type
/// whose code is "1052" (legacy <c>Program.cs</c> lines 2675-2676). Every configured
/// <see cref="AbsenceType"/> contributes its own wage code, so the set is a runtime property of the
/// <c>AbsenceType</c> master table.</para>
///
/// <para><b>The literal codes in the legacy body, classified.</b> Unlike basic pay, leave pay does contain ONE
/// genuine branch on the current wage type -- <c>currWageType.Code == "1052"</c> (legacy line 289, Leave
/// Encashment Pay). All the others are not:</para>
/// <list type="bullet">
/// <item><description><c>"2003"</c> (legacy lines 256, 448, 511, 659, 720, 893) is
/// <c>AbsenceType.WageType.Code</c> -- the LEAVE TYPE's wage type, used to detect "sick leave unpaid" and swap
/// in a different <c>AbsenceType</c>. Never <c>currWageType</c>.</description></item>
/// <item><description><c>"1"</c> and <c>"3"</c> (legacy lines 259, 299, 306, 315, 902) are
/// <c>AbsenceType.Code</c> values (annual leave / sick leave), not wage codes at all.</description></item>
/// <item><description><c>"1000"</c> (legacy lines 386, 390) is a lookup of the Basic Pay register line's
/// <c>Rate</c>.</description></item>
/// </list>
///
/// <para><b>Consequence.</b> The 1052 branch alone COULD be an <see cref="IWageTypeCalculator"/>, but only if
/// the engine still applied legacy's category-level <c>if (!onHold_LeavePay)</c> gate (legacy
/// <c>Program.cs</c> line 2557) -- a gate <see cref="IWageTypeCalculatorResolver"/> does NOT apply. Splitting
/// it out would therefore either lose that gate or duplicate it, so 1052 stays inside this service exactly
/// where legacy put it, and category dispatch stays in the engine (wave P1b).</para>
/// </summary>
public interface IEmployeeLeavePayCalculationService : IDomainService
{
    /// <summary>
    /// Legacy <c>Program.wtLeavePayWageList</c> (legacy <c>Program.cs</c> lines 2675-2676): every
    /// <c>AbsenceType.WageTypeId</c>, plus the id of the wage type whose <c>Code</c> is "1052".
    /// Run-wide, not per-employee.
    /// </summary>
    Task<List<int>> GetLeavePayWageTypeIdsAsync();

    /// <summary>
    /// Legacy <c>EmployeeLeavePayService.Main</c>.
    ///
    /// <para><b>Category gate NOT applied here.</b> Legacy wraps the call in
    /// <c>if (!onHold_LeavePay)</c> (legacy <c>Program.cs</c> line 2557). That is the engine's job -- see
    /// <see cref="PayrollCalculationContext.OnHold"/>.<c>LeavePay</c>. It is deliberately not re-tested inside
    /// this service, consistent with the "gating is the dispatcher's job" rule on
    /// <see cref="IWageTypeCalculator"/>.</para>
    /// </summary>
    Task CalculateAsync(PayrollCalculationContext context);
}
