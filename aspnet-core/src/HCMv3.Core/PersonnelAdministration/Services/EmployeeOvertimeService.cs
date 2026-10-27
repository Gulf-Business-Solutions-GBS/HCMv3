using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.UI;

namespace HCMv3.PersonnelAdministration.Services;

/// <inheritdoc cref="IEmployeeOvertimeService"/>
public class EmployeeOvertimeService : HCMv3DomainServiceBase, IEmployeeOvertimeService, ITransientDependency
{
    private readonly IRepository<EmployeeOvertime> _repository;

    public EmployeeOvertimeService(IRepository<EmployeeOvertime> repository)
    {
        _repository = repository;
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
