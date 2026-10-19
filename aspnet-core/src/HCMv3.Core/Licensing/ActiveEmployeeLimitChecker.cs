using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Features;
using Abp.Domain.Repositories;
using Abp.UI;
using HCMv3.PersonnelAdministration;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Licensing;

/// <inheritdoc cref="IActiveEmployeeLimitChecker"/>
/// <remarks>
/// Counts as legacy <c>LicenseManagerService.GetAssignedUserLicenseCount</c> did for this line: employees whose status
/// is Active (soft-deleted ones excluded by ABP's filter).
/// </remarks>
public class ActiveEmployeeLimitChecker : HCMv3DomainServiceBase, IActiveEmployeeLimitChecker
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IFeatureChecker _featureChecker;
    private readonly IFeatureManager _featureManager;

    public ActiveEmployeeLimitChecker(
        IRepository<Employee> employeeRepository,
        IFeatureChecker featureChecker,
        IFeatureManager featureManager)
    {
        _employeeRepository = employeeRepository;
        _featureChecker = featureChecker;
        _featureManager = featureManager;
    }

    public async Task EnsureCanActivateAsync(int tenantId, int employeeId)
    {
        var limit = GetLimit(await GetValueAsync(tenantId));
        if (limit == 0)
        {
            return;
        }

        // Its own (or the joined) unit of work: the entity hook can run inside a SaveChanges that has none.
        int othersActive;
        using (var uow = UnitOfWorkManager.Begin())
        {
            using (UnitOfWorkManager.Current.SetTenantId(tenantId))
            {
                othersActive = await _employeeRepository.GetAll()
                    .CountAsync(e => e.Status == EmployeeStatus.Active && e.Id != employeeId);
            }

            await uow.CompleteAsync();
        }

        Refuse(othersActive, limit);
    }

    public void EnsureCanActivate(int tenantId, int employeeId)
    {
        var limit = GetLimit(GetValue(tenantId));
        if (limit == 0)
        {
            return;
        }

        int othersActive;
        using (var uow = UnitOfWorkManager.Begin())
        {
            using (UnitOfWorkManager.Current.SetTenantId(tenantId))
            {
                othersActive = _employeeRepository.GetAll()
                    .Count(e => e.Status == EmployeeStatus.Active && e.Id != employeeId);
            }

            uow.Complete();
        }

        Refuse(othersActive, limit);
    }

    private void Refuse(int othersActive, int limit)
    {
        if (othersActive >= limit)
        {
            throw new UserFriendlyException(L("MaxActiveEmployeesReached", limit));
        }
    }

    private async Task<string> GetValueAsync(int tenantId)
    {
        // An unregistered feature would throw on every employee save; unregistered means unlimited.
        return _featureManager.GetOrNull(HcmFeatures.MaxActiveEmployees) == null
            ? null
            : await _featureChecker.GetValueAsync(tenantId, HcmFeatures.MaxActiveEmployees);
    }

    private string GetValue(int tenantId)
    {
        return _featureManager.GetOrNull(HcmFeatures.MaxActiveEmployees) == null
            ? null
            : _featureChecker.GetValue(tenantId, HcmFeatures.MaxActiveEmployees);
    }

    private static int GetLimit(string value)
    {
        return int.TryParse(value, out var limit) && limit > 0 ? limit : 0;
    }
}
