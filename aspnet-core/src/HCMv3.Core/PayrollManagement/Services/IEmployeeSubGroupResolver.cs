using System;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Services;
using HCMv3.PersonnelAdministration;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// Resolves "which <see cref="EmployeeSubGroup"/> is this employee in, as of a date" -- legacy
/// <c>Program.empOrgAssignmentList.Where(r =&gt; r.StartDate &lt;= payDate &amp;&amp; r.EndDate &gt;= payDate)
/// .Select(r =&gt; r.EmployeeSubGroup).FirstOrDefault()</c>, which appears verbatim in FOUR wave-P6 rules:
/// <c>GetFundDed</c> (legacy <c>HCM10.PayrollEngine/EmployeeCalculatedPayService.cs</c> lines 3644-3645),
/// <c>GetCommissionProvision</c> (lines 3875-3876), <c>GetAverageTicket</c> (lines 3967-3968) and
/// <c>GetServiceBonus</c> (lines 4091-4092).
///
/// <para><b>THIS IS A KNOWN, LOUD PORT GAP -- READ BEFORE USING.</b> This port's
/// <see cref="EmployeeOrgAssignment"/> has NO <c>EmployeeSubGroupId</c> FK: it was explicitly deferred in
/// Wave B (see that entity's class remarks -- "DEFERRED ... Position/EmployeeGroup/<b>EmployeeSubGroup</b>/
/// PayrollArea/WorkflowGroup fields"). Unlike <c>PayrollArea</c>, there is NO substitute chain: neither
/// <c>OrgUnitProfile</c> nor any other ported entity carries an <c>EmployeeSubGroupId</c> reachable from an
/// org assignment. (<c>ManpowerPlanningManagement.Position</c> does carry one, but
/// <see cref="EmployeeOrgAssignment"/> has no <c>PositionId</c> either -- also deferred.)</para>
///
/// <para>Rather than invent a lookup and quietly compute a wrong Fund Deduction / Average Commission /
/// Average Ticket / Service Bonus, <see cref="EmployeeSubGroupResolver"/> throws. Wave P6's brief is explicit:
/// "a loud gap beats a plausible-looking guess". Two ways to close it, either is fine:</para>
/// <list type="number">
/// <item><description>Add <c>EmployeeSubGroupId</c> to <see cref="EmployeeOrgAssignment"/> (+ DbSet config
/// and migration) and implement this resolver against it -- the legacy-faithful fix.</description></item>
/// <item><description>Add an <c>EmployeeSubGroupId</c> member to <see cref="PayrollCalculationContext"/>
/// (that class's remarks explicitly sanction adding missing legacy statics) resolved once per employee by the
/// engine, and have the four rules read it from there instead.</description></item>
/// </list>
/// </summary>
public interface IEmployeeSubGroupResolver : IDomainService
{
    /// <summary>
    /// The employee's sub-group id as of <paramref name="asOnDate"/>, or <c>null</c> when the employee has no
    /// covering assignment. Legacy callers all throw "EmployeeSubGroup Not found." on <c>null</c>.
    /// </summary>
    Task<int?> GetEmployeeSubGroupIdAsync(int employeeId, DateTime asOnDate);
}

/// <inheritdoc cref="IEmployeeSubGroupResolver"/>
public class EmployeeSubGroupResolver : HCMv3DomainServiceBase, IEmployeeSubGroupResolver, ITransientDependency
{
    /// <inheritdoc/>
    public Task<int?> GetEmployeeSubGroupIdAsync(int employeeId, DateTime asOnDate)
    {
        throw new NotImplementedException(
            "EmployeeSubGroup cannot be resolved for an employee in this port: EmployeeOrgAssignment has no "
            + "EmployeeSubGroupId FK (deferred in Wave B -- see EmployeeOrgAssignment's class remarks) and no "
            + "substitute chain exists, unlike PayrollArea's org-assignment -> OrgUnitProfile.PayrollAreaId "
            + "chain used by EmployeePayrollAreaResolver. Legacy reads "
            + "Program.empOrgAssignmentList.Where(r => r.StartDate <= payDate && r.EndDate >= payDate)"
            + ".Select(r => r.EmployeeSubGroup).FirstOrDefault() at "
            + "HCM10.PayrollEngine/EmployeeCalculatedPayService.cs lines 3644, 3875, 3967 and 4091. "
            + "Affected wave-P6 wage codes: 23 (Fund Deduction), 27 (Average Commission), "
            + "29 (Average Ticket), 35 (Service Bonus). See IEmployeeSubGroupResolver's remarks for the two "
            + "supported ways to close this gap.");
    }
}
