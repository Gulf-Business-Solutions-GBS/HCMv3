using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using HCMv3.SystemSecurity;

namespace HCMv3.PayrollManagement.PayrollLock;

/// <summary>
/// Gives a company the default payroll-sensitive list (<see cref="PayrollSensitiveDefaults"/>) as System Objects
/// rows. Called when a company is created; existing companies get the same rows from the startup seed
/// (<c>PayrollSensitiveObjectBuilder</c>).
///
/// IDEMPOTENT, keyed on the type's full name, deleted rows included: a row the company already has -- unticked, or
/// removed -- is left alone, so running this again never undoes the company's own choice.
/// </summary>
public class PayrollSensitiveObjectSeeder : ITransientDependency
{
    private readonly IRepository<SysObject> _sysObjectRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public PayrollSensitiveObjectSeeder(IRepository<SysObject> sysObjectRepository, IUnitOfWorkManager unitOfWorkManager)
    {
        _sysObjectRepository = sysObjectRepository;
        _unitOfWorkManager = unitOfWorkManager;
    }

    /// <summary>Adds the default rows the company does not have yet; returns how many were added.</summary>
    public async Task<int> SeedAsync(int tenantId)
    {
        using (_unitOfWorkManager.Current.SetTenantId(tenantId))
        using (_unitOfWorkManager.Current.DisableFilter(AbpDataFilters.SoftDelete))
        {
            var existing = _sysObjectRepository.GetAll()
                .Where(r => r.FullName != null)
                .Select(r => r.FullName)
                .ToList()
                .ToHashSet(StringComparer.Ordinal);

            var added = 0;
            foreach (var type in PayrollSensitiveDefaults.Types.Where(t => !existing.Contains(t.FullName)))
            {
                await _sysObjectRepository.InsertAsync(new SysObject
                {
                    TenantId = tenantId,
                    Name = type.Name,
                    FullName = type.FullName,
                    IsPayrollSensitive = true,
                });
                added++;
            }

            await _unitOfWorkManager.Current.SaveChangesAsync();
            return added;
        }
    }
}
