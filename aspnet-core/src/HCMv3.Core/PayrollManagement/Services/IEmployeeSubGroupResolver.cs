using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Services;
using Abp.Domain.Uow;
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
/// <para><b>GAP CLOSED.</b> This used to throw: <see cref="EmployeeOrgAssignment"/> had no
/// <c>EmployeeSubGroupId</c> FK (deferred in Wave B) and, unlike <c>PayrollArea</c>, there was no substitute
/// chain to stand in for it. Option 1 of the two documented fixes was taken -- the FK now exists on
/// <see cref="EmployeeOrgAssignment"/> (nullable, <c>DeleteBehavior.Restrict</c>, migration
/// <c>20260728103447_Added_EmployeeOrgAssignment_Group_SubGroup_PayrollArea_FKs</c>) and this resolver reads
/// it date-effectively, exactly as legacy did.</para>
///
/// <para>The FK is nullable because rows created before that migration have no value, so <c>null</c> remains
/// a reachable result. The four wage-code rules keep their legacy "EmployeeSubGroup Not found." throw on
/// <c>null</c> -- that is legacy behaviour, not a port gap.</para>
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
[UnitOfWork]
public class EmployeeSubGroupResolver : HCMv3DomainServiceBase, IEmployeeSubGroupResolver, ITransientDependency
{
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;

    public EmployeeSubGroupResolver(IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository)
    {
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
    }

    /// <inheritdoc/>
    public virtual Task<int?> GetEmployeeSubGroupIdAsync(int employeeId, DateTime asOnDate)
    {
        // Legacy Program.empOrgAssignmentList.Where(r => r.StartDate <= payDate && r.EndDate >= payDate)
        //   .Select(r => r.EmployeeSubGroup).FirstOrDefault()
        // (HCM10.PayrollEngine/EmployeeCalculatedPayService.cs lines 3644, 3875, 3967, 4091).
        // Assignments whose EmployeeSubGroupId is null are skipped rather than short-circuiting the lookup:
        // pre-migration rows carry null, and legacy never had such a row.
        var subGroupId = _employeeOrgAssignmentRepository.GetAll()
            .Where(a => a.EmployeeId == employeeId
                        && a.StartDate <= asOnDate
                        && a.EndDate >= asOnDate
                        && a.EmployeeSubGroupId != null)
            .OrderByDescending(a => a.StartDate)
            .Select(a => a.EmployeeSubGroupId)
            .FirstOrDefault();

        return Task.FromResult(subGroupId);
    }
}
