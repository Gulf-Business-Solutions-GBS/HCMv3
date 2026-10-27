using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.UI;

namespace HCMv3.PersonnelAdministration.Services;

/// <inheritdoc cref="IEmployeeOnHoldService"/>
public class EmployeeOnHoldService : HCMv3DomainServiceBase, IEmployeeOnHoldService, ITransientDependency
{
    private readonly IRepository<EmployeeOnHold> _repository;

    public EmployeeOnHoldService(IRepository<EmployeeOnHold> repository)
    {
        _repository = repository;
    }

    /// <inheritdoc/>
    public Task ValidateAsync(EmployeeOnHold entity)
    {
        if (entity.EndDate < entity.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        // Legacy: !(r.StartDate > this.EndDate || r.EndDate < this.StartDate), same employee, not itself.
        var overlaps = _repository.GetAll()
            .Any(o => o.Id != entity.Id
                      && o.EmployeeId == entity.EmployeeId
                      && o.StartDate.Date <= entity.EndDate.Date
                      && o.EndDate.Date >= entity.StartDate.Date);

        if (overlaps)
        {
            throw new UserFriendlyException(L("OverlapConflictFound"));
        }

        return Task.CompletedTask;
    }
}
