using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.UI;
using HCMv3.PayrollManagement;

namespace HCMv3.PersonnelAdministration.Services;

/// <inheritdoc cref="IEmployeeOvertimeService"/>
public class EmployeeOvertimeService : HCMv3DomainServiceBase, IEmployeeOvertimeService, ITransientDependency
{
    private readonly IRepository<EmployeeOvertime> _repository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly IRepository<EmployeeSubGroup> _subGroupRepository;
    private readonly IRepository<Employee> _employeeRepository;

    public EmployeeOvertimeService(
        IRepository<EmployeeOvertime> repository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        IRepository<EmployeeSubGroup> subGroupRepository,
        IRepository<Employee> employeeRepository)
    {
        _repository = repository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _subGroupRepository = subGroupRepository;
        _employeeRepository = employeeRepository;
    }

    /// <inheritdoc/>
    public Task EnsureEmployeeEligibleForOvertimeAsync(int employeeId, DateTime effectiveDate)
    {
        var day = effectiveDate.Date;

        // Legacy lines 178-185: the first org assignment covering the date.
        var assignment = _orgAssignmentRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId && r.StartDate <= day && r.EndDate >= day)
            .OrderBy(r => r.Id)
            .Select(r => new { r.EmployeeSubGroupId })
            .FirstOrDefault();

        var employee = _employeeRepository.GetAll()
            .Where(e => e.Id == employeeId)
            .Select(e => new { e.Code, e.FullNameEn })
            .FirstOrDefault();
        var employeeName = employee == null ? employeeId.ToString() : $"{employee.Code} - {employee.FullNameEn}";

        // Legacy lines 186-187.
        if (assignment == null)
        {
            throw new UserFriendlyException(L("OvertimeOrgAssignmentNotFound", employeeName, day.ToString("dd/MM/yyyy")));
        }

        // Legacy lines 189-190.
        if (!assignment.EmployeeSubGroupId.HasValue)
        {
            throw new UserFriendlyException(L("OvertimeEmployeeSubGroupNotFound", employeeName));
        }

        // Legacy lines 192-193.
        var payOvertime = _subGroupRepository.GetAll()
            .Where(s => s.Id == assignment.EmployeeSubGroupId.Value)
            .Select(s => s.PayOvertime)
            .FirstOrDefault();

        if (!payOvertime)
        {
            throw new UserFriendlyException(L("EmployeeNotEligibleForOvertime", employeeName));
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task EnsureNoOvertimeOnDateAsync(int employeeId, DateTime effectiveDate, int excludeId = 0)
    {
        var day = effectiveDate.Date;

        var hasDuplicate = _repository.GetAll()
            .Any(e => e.Id != excludeId && e.EmployeeId == employeeId && e.EffectiveDate == day);

        if (hasDuplicate)
        {
            throw new UserFriendlyException(L("DuplicateRecordFound"));
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<HashSet<(int EmployeeId, DateTime Date)>> GetDaysWithOvertimeAsync(
        IReadOnlyCollection<int> employeeIds,
        DateTime startDate,
        DateTime endDate)
    {
        var result = new HashSet<(int EmployeeId, DateTime Date)>();

        if (employeeIds == null || employeeIds.Count == 0)
        {
            return Task.FromResult(result);
        }

        var ids = employeeIds.ToList();
        var from = startDate.Date;
        var to = endDate.Date;

        var rows = _repository.GetAll()
            .Where(e => ids.Contains(e.EmployeeId) && e.EffectiveDate >= from && e.EffectiveDate <= to)
            .Select(e => new { e.EmployeeId, e.EffectiveDate })
            .ToList();

        foreach (var row in rows)
        {
            result.Add((row.EmployeeId, row.EffectiveDate.Date));
        }

        return Task.FromResult(result);
    }
}
