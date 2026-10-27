using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Services;
using HCMv3.PersonnelAdministration;

namespace HCMv3.ProjectManagement.Services;

/// <summary>One employee the Project Cost Engine screen's employee picker offers, and whether it has been costed.</summary>
public class ProjectCostEngineCandidate
{
    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public string FullNameEn { get; set; }

    public string FullNameAr { get; set; }

    /// <summary>A project register already exists for this employee and period (legacy "Processed").</summary>
    public bool IsProcessed { get; set; }
}

/// <summary>
/// Who the Project Cost Engine screen's employee picker lists for a payroll area and period. Port of legacy
/// <c>uscProjectCosting.BuildEmployeePopupList</c> (<c>uscProjectCosting.ascx.cs</c> lines 1728-1958).
///
/// <para>This only GUIDES the operator: it decides what the picker shows, not what the engine accepts. The engine
/// still applies its own selection and checks to whatever codes are sent.</para>
/// </summary>
public interface IProjectCostEngineCandidateSelector : IDomainService
{
    /// <param name="payrollAreaName">Matched against <c>PayrollArea.Name</c> (legacy <c>cmbPayrollArea.Text</c>).</param>
    /// <param name="payrollDate">The period end the screen shows (legacy <c>txtEndDate</c>).</param>
    Task<List<ProjectCostEngineCandidate>> GetCandidatesAsync(string payrollAreaName, DateTime payrollDate);
}

/// <inheritdoc cref="IProjectCostEngineCandidateSelector"/>
/// <remarks>
/// Only the Regular arm: legacy's off-cycle arm (lines 1830-1940) is entirely commented out, and the engine runs
/// Regular only. Synchronous LINQ over <c>IRepository.GetAll()</c> wrapped in a task, as the engine itself
/// (Core has no EF Core reference).
/// </remarks>
public class ProjectCostEngineCandidateSelector : HCMv3DomainServiceBase, IProjectCostEngineCandidateSelector
{
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<ProjectRegister> _projectRegisterRepository;

    public ProjectCostEngineCandidateSelector(
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        IRepository<Employee> employeeRepository,
        IRepository<ProjectRegister> projectRegisterRepository)
    {
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _employeeRepository = employeeRepository;
        _projectRegisterRepository = projectRegisterRepository;
    }

    public Task<List<ProjectCostEngineCandidate>> GetCandidatesAsync(string payrollAreaName, DateTime payrollDate)
    {
        // Legacy guard checks (lines 1731-1744): no area, no period date, or an area that does not exist => nothing.
        if (string.IsNullOrWhiteSpace(payrollAreaName) || payrollDate == default(DateTime))
        {
            return Task.FromResult(new List<ProjectCostEngineCandidate>());
        }

        // Legacy parses txtEndDate as dd/MM/yyyy, so the period end has no time of day.
        payrollDate = payrollDate.Date;

        // Legacy lines 1774-1786: employees with an org assignment in this payroll area covering the period end,
        // who had joined by then. One row per assignment, as legacy's query -- de-duplicated below.
        var employeeIdsInArea = _employeeOrgAssignmentRepository.GetAll()
            .Where(r => r.PayrollArea != null
                        && r.PayrollArea.Name == payrollAreaName
                        && r.StartDate <= payrollDate
                        && r.EndDate >= payrollDate)
            .Select(r => r.EmployeeId)
            .Distinct()
            .ToList();

        if (!employeeIdsInArea.Any())
        {
            return Task.FromResult(new List<ProjectCostEngineCandidate>());
        }

        var employees = _employeeRepository.GetAll()
            .Where(e => employeeIdsInArea.Contains(e.Id) && e.JoiningDate <= payrollDate)
            .Select(e => new { e.Id, e.Code, e.FullNameEn, e.FullNameAr, e.Status })
            .ToList();

        // Legacy lines 1812-1827: eligible = every ACTIVE employee; processed = one with a project register for
        // this very period (the register's Period is the month end, as the engine writes it).
        var activeIds = employees.Where(e => e.Status == EmployeeStatus.Active).Select(e => e.Id).ToList();

        if (!activeIds.Any())
        {
            return Task.FromResult(new List<ProjectCostEngineCandidate>());
        }

        var processed = new HashSet<int>(_projectRegisterRepository.GetAll()
            .Where(r => activeIds.Contains(r.EmployeeId) && r.Period == payrollDate)
            .Select(r => r.EmployeeId)
            .ToList());

        // Legacy lines 1943-1956.
        var candidates = employees
            .Where(e => e.Status == EmployeeStatus.Active)
            .Select(e => new ProjectCostEngineCandidate
            {
                EmployeeId = e.Id,
                EmployeeCode = e.Code,
                FullNameEn = e.FullNameEn,
                FullNameAr = e.FullNameAr,
                IsProcessed = processed.Contains(e.Id)
            })
            .OrderBy(c => c.EmployeeCode)
            .ToList();

        return Task.FromResult(candidates);
    }
}
