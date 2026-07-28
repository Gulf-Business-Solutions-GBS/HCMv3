using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Services;
using Abp.Domain.Uow;
using HCMv3.OrganizationManagement;
using HCMv3.PersonnelAdministration;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// Resolves "which <see cref="PayrollArea"/> is this employee in, as of a date" -- and its inverse, "which
/// employees are in this payroll area, as of a date".
///
/// <para><b>Why this seam exists.</b> The legacy engine reads
/// <c>EmployeeOrgAssignment.PayrollArea</c> directly, in two hot paths: the employee-selection queries
/// (legacy <c>HCM10.PayrollEngine/Program.cs</c> lines 432-448, 472-474, 483-485, 498-500) and the
/// per-employee <c>empPayrollArea</c> lookup that feeds the control-record guard (line 832). That FK was
/// deferred in Wave B and this resolver reached PayrollArea through <c>OrgUnitProfile</c> instead. The seam
/// paid off exactly as intended: <c>EmployeeOrgAssignment.PayrollAreaId</c> now exists (nullable, migration
/// <c>20260728103447_Added_EmployeeOrgAssignment_Group_SubGroup_PayrollArea_FKs</c>) and only this ONE
/// implementation changed.</para>
///
/// <para><b>Resolution order (both paths live).</b> The assignment's own <c>PayrollAreaId</c> wins when set
/// -- that is the legacy-faithful read. The <c>OrgUnitProfile</c> chain below is the FALLBACK and must NOT be
/// deleted: the column is nullable and every row created before the migration has <c>null</c> in it. The
/// deltas listed further down apply to the fallback path only.</para>
///
/// <para><b>The substitute lookup is not invented here.</b> <see cref="EmployeeOrgAssignment"/> carries
/// <c>OrganizationUnitId</c>, and <see cref="OrgUnitProfile"/> (the 1:1 HR companion row of an
/// <c>Abp.Organizations.OrganizationUnit</c>) carries <c>PayrollAreaId</c>, added by the Wave 4 gap-fill for
/// exactly this reason. The identical org-assignment -&gt; org-unit-profile -&gt; payroll-area chain is
/// already shipped and documented in <c>HCMv3.GeneralMasterInfo.Services.CurrencyService.RoundAsync</c> as
/// this codebase's stand-in for the legacy <c>Employee.CurrentPayrollArea</c> computed property. This
/// resolver follows that established precedent.</para>
///
/// <para><b>Known behavioural deltas vs. legacy</b> (call them out in any P4-P6 review):</para>
/// <list type="bullet">
/// <item><description>Legacy matched the assignment's own payroll area; this matches the payroll area of the
/// assignment's ORGANISATION UNIT. An employee assigned to a unit whose profile has no
/// <c>PayrollAreaId</c> resolves to <c>null</c> and will fail the "Payroll area not found in OrgAssignment"
/// guard (legacy line 833-834), rather than being silently mis-assigned.</description></item>
/// <item><description>No walk up the organisation-unit parent chain is performed. Legacy had no such
/// concept, and inventing an inheritance rule here would be exactly the kind of divergent business rule this
/// port must avoid.</description></item>
/// </list>
/// </summary>
public interface IEmployeePayrollAreaResolver : IDomainService
{
    /// <summary>
    /// Legacy <c>empOrgAssignmentList.Where(r =&gt; r.StartDate &lt;= payInDate &amp;&amp; r.EndDate &gt;= payInDate)
    /// .Select(r =&gt; r.PayrollArea).FirstOrDefault()</c> (legacy <c>Program.cs</c> line 832). Returns
    /// <c>null</c> when the employee has no assignment covering <paramref name="asOnDate"/> or that
    /// assignment's organisation unit has no payroll area configured.
    /// </summary>
    Task<PayrollArea> GetEmployeePayrollAreaAsync(int employeeId, DateTime asOnDate);

    /// <summary>
    /// Ids of every employee whose org assignment covering <paramref name="asOnDate"/> maps to the payroll
    /// area named <paramref name="payrollAreaName"/>. Legacy equivalent: the
    /// <c>r.PayrollArea.Name == payrollArea</c> predicate of the employee-selection queries (legacy
    /// <c>Program.cs</c> lines 435, 446, 474, 485, 500). Ordered by employee id, matching legacy's
    /// <c>.OrderBy(r =&gt; r.Employee.Oid)</c>.
    /// </summary>
    Task<List<int>> GetEmployeeIdsInPayrollAreaAsync(string payrollAreaName, DateTime asOnDate);
}

/// <inheritdoc cref="IEmployeePayrollAreaResolver"/>
[UnitOfWork]
public class EmployeePayrollAreaResolver : HCMv3DomainServiceBase, IEmployeePayrollAreaResolver
{
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly IRepository<OrgUnitProfile> _orgUnitProfileRepository;
    private readonly IRepository<PayrollArea> _payrollAreaRepository;

    public EmployeePayrollAreaResolver(
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        IRepository<OrgUnitProfile> orgUnitProfileRepository,
        IRepository<PayrollArea> payrollAreaRepository)
    {
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _orgUnitProfileRepository = orgUnitProfileRepository;
        _payrollAreaRepository = payrollAreaRepository;
    }

    public Task<PayrollArea> GetEmployeePayrollAreaAsync(int employeeId, DateTime asOnDate)
    {
        // PREFERRED (legacy-faithful): the assignment's own PayrollAreaId, now that the FK exists.
        var directPayrollAreaId = _employeeOrgAssignmentRepository.GetAll()
            .Where(a => a.EmployeeId == employeeId
                        && a.StartDate <= asOnDate
                        && a.EndDate >= asOnDate
                        && a.PayrollAreaId != null)
            .Select(a => a.PayrollAreaId)
            .FirstOrDefault();

        if (directPayrollAreaId != null)
        {
            return Task.FromResult(
                _payrollAreaRepository.GetAll().FirstOrDefault(a => a.Id == directPayrollAreaId.Value));
        }

        // FALLBACK (retained, load-bearing): rows created before the FK migration have PayrollAreaId == null.
        var organizationUnitId = _employeeOrgAssignmentRepository.GetAll()
            .Where(a => a.EmployeeId == employeeId
                        && a.StartDate <= asOnDate
                        && a.EndDate >= asOnDate
                        && a.OrganizationUnitId != null)
            .Select(a => a.OrganizationUnitId)
            .FirstOrDefault();

        if (organizationUnitId == null)
        {
            return Task.FromResult<PayrollArea>(null);
        }

        var payrollAreaId = _orgUnitProfileRepository.GetAll()
            .Where(p => p.OrganizationUnitId == organizationUnitId.Value && p.PayrollAreaId != null)
            .Select(p => p.PayrollAreaId)
            .FirstOrDefault();

        if (payrollAreaId == null)
        {
            return Task.FromResult<PayrollArea>(null);
        }

        return Task.FromResult(_payrollAreaRepository.GetAll().FirstOrDefault(a => a.Id == payrollAreaId.Value));
    }

    public Task<List<int>> GetEmployeeIdsInPayrollAreaAsync(string payrollAreaName, DateTime asOnDate)
    {
        var payrollAreaIds = _payrollAreaRepository.GetAll()
            .Where(a => a.Name == payrollAreaName)
            .Select(a => a.Id)
            .ToList();

        var employeeIds = new List<int>();

        // PREFERRED (legacy-faithful): the assignment's own PayrollAreaId.
        if (payrollAreaIds.Any())
        {
            employeeIds.AddRange(_employeeOrgAssignmentRepository.GetAll()
                .Where(a => a.StartDate <= asOnDate
                            && a.EndDate >= asOnDate
                            && a.PayrollAreaId != null
                            && payrollAreaIds.Contains(a.PayrollAreaId.Value))
                .Select(a => a.EmployeeId)
                .ToList());
        }

        // FALLBACK (retained, load-bearing): assignments predating the FK migration carry a null
        // PayrollAreaId and are still reachable only through their organisation unit's profile.
        var organizationUnitIds = _orgUnitProfileRepository.GetAll()
            .Where(p => p.PayrollAreaId != null && p.PayrollArea.Name == payrollAreaName)
            .Select(p => p.OrganizationUnitId)
            .ToList();

        if (organizationUnitIds.Any())
        {
            employeeIds.AddRange(_employeeOrgAssignmentRepository.GetAll()
                .Where(a => a.StartDate <= asOnDate
                            && a.EndDate >= asOnDate
                            && a.PayrollAreaId == null
                            && a.OrganizationUnitId != null
                            && organizationUnitIds.Contains(a.OrganizationUnitId.Value))
                .Select(a => a.EmployeeId)
                .ToList());
        }

        return Task.FromResult(employeeIds.Distinct().OrderBy(id => id).ToList());
    }
}
