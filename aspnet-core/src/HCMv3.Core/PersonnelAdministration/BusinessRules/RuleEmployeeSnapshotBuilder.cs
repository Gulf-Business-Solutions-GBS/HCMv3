using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Services;
using HCMv3.OrganizationManagement;

namespace HCMv3.PersonnelAdministration.BusinessRules;

/// <summary>
/// Builds the <see cref="RuleEmployeeSnapshot"/> a rule branches on.
///
/// <para>
/// One service, one place, three queries. Concentrating it here is what stops "is this person a manager"
/// from being answered differently in different callers, and it is what makes the cost of that question
/// visible: a rule author writing <c>Applier.IsManager</c> has no idea what it costs, so the cost has to be
/// bounded by whoever wrote this.
/// </para>
/// </summary>
public interface IRuleEmployeeSnapshotBuilder : IDomainService
{
    /// <summary>
    /// The snapshot for an employee as of a date, or null when the employee does not exist. Returns null
    /// rather than an empty snapshot: a rule branching on a person who is not there should not be handed a
    /// record that looks like a real person with every field blank.
    /// </summary>
    Task<RuleEmployeeSnapshot> BuildAsync(int employeeId, DateTime asOf);
}

/// <inheritdoc cref="IRuleEmployeeSnapshotBuilder"/>
/// <remarks>
/// Uses the synchronous <c>IQueryable</c> from <c>IRepository.GetAll()</c>, as the rest of Core does --
/// <c>HCMv3.Core</c> has no reference to EntityFrameworkCore, so the async LINQ extensions are not
/// available here. Same pattern as <c>RequestMasterResolver</c> and <c>WorkflowApproverResolver</c>.
/// </remarks>
public class RuleEmployeeSnapshotBuilder : HCMv3DomainServiceBase, IRuleEmployeeSnapshotBuilder
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly IRepository<EmployeeManager> _employeeManagerRepository;
    private readonly IRepository<JobTitle> _jobTitleRepository;

    public RuleEmployeeSnapshotBuilder(
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        IRepository<EmployeeManager> employeeManagerRepository,
        IRepository<JobTitle> jobTitleRepository)
    {
        _employeeRepository = employeeRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _employeeManagerRepository = employeeManagerRepository;
        _jobTitleRepository = jobTitleRepository;
    }

    public Task<RuleEmployeeSnapshot> BuildAsync(int employeeId, DateTime asOf)
    {
        var date = asOf.Date;

        var employee = _employeeRepository.GetAll()
            .Where(e => e.Id == employeeId)
            .Select(e => new { e.Id, e.Code, e.JoiningDate, e.Status, e.Gender })
            .FirstOrDefault();

        if (employee == null)
        {
            return Task.FromResult<RuleEmployeeSnapshot>(null);
        }

        var snapshot = new RuleEmployeeSnapshot
        {
            EmployeeId = employee.Id,
            Code = employee.Code,
            JoiningDate = employee.JoiningDate,
            Status = employee.Status,
            Gender = employee.Gender
        };

        // The assignment effective on the date, not simply the latest row. Org assignments are dated
        // history, so "the newest row" would answer with a future transfer that has not happened yet.
        var assignment = _orgAssignmentRepository.GetAll()
            .Where(a => a.EmployeeId == employeeId && a.StartDate <= date && a.EndDate >= date)
            .OrderByDescending(a => a.StartDate)
            .Select(a => new
            {
                a.JobTitleId,
                a.EmployeeGroupId,
                a.EmployeeSubGroupId,
                a.OrganizationUnitId,
                a.CostCenterId,
                a.WorkLocationId,
                a.WorkflowGroupId
            })
            .FirstOrDefault();

        if (assignment != null)
        {
            snapshot.JobTitleId = assignment.JobTitleId;
            snapshot.EmployeeGroupId = assignment.EmployeeGroupId;
            snapshot.EmployeeSubGroupId = assignment.EmployeeSubGroupId;
            snapshot.OrganizationUnitId = assignment.OrganizationUnitId;
            snapshot.CostCenterId = assignment.CostCenterId;
            snapshot.WorkLocationId = assignment.WorkLocationId;
            snapshot.WorkflowGroupId = assignment.WorkflowGroupId;

            if (assignment.JobTitleId.HasValue)
            {
                // Resolved to its code so rules can say "CHRO" instead of an id that differs per
                // environment. See RuleEmployeeSnapshot.JobTitleCode.
                snapshot.JobTitleCode = _jobTitleRepository.GetAll()
                    .Where(j => j.Id == assignment.JobTitleId.Value)
                    .Select(j => j.Code)
                    .FirstOrDefault();
            }
        }

        // Current reports only -- a manager whose last report left last year is not a manager today.
        snapshot.DirectReportCount = _employeeManagerRepository.GetAll()
            .Count(m => m.ManagerId == employeeId && m.StartDate <= date && m.EndDate >= date);

        snapshot.IsManager = snapshot.DirectReportCount > 0;

        return Task.FromResult(snapshot);
    }
}
