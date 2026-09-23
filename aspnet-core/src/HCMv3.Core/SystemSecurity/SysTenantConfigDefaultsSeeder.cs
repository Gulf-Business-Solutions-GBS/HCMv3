using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.SystemSecurity;

/// <summary>
/// Gives a tenant the default <see cref="SysTenantConfig"/> rows (<see cref="SysTenantConfigDefaults"/>).
/// Called when a tenant is created; existing tenants get the same rows from the startup seed.
///
/// IDEMPOTENT, keyed on <c>Key</c>: a key the tenant already has, whatever its value, is left alone, so
/// running this again never overwrites a tenant's own configuration.
/// </summary>
public class SysTenantConfigDefaultsSeeder : ITransientDependency
{
    private readonly IRepository<SysTenantConfig> _sysTenantConfigRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public SysTenantConfigDefaultsSeeder(
        IRepository<SysTenantConfig> sysTenantConfigRepository,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _sysTenantConfigRepository = sysTenantConfigRepository;
        _unitOfWorkManager = unitOfWorkManager;
    }

    /// <summary>Adds the default rows the tenant does not have yet; returns how many were added.</summary>
    /// <remarks>Joins the caller's unit of work (tenant creation) or opens its own.</remarks>
    [UnitOfWork]
    public virtual async Task<int> SeedAsync(int tenantId)
    {
        using (_unitOfWorkManager.Current.SetTenantId(tenantId))
        using (_unitOfWorkManager.Current.DisableFilter(AbpDataFilters.SoftDelete))
        {
            var existing = (await _sysTenantConfigRepository.GetAll()
                    .Select(r => r.Key)
                    .ToListAsync())
                .Where(key => key != null)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var added = 0;
            foreach (var entry in SysTenantConfigDefaults.Entries.Where(e => !existing.Contains(e.Key)))
            {
                await _sysTenantConfigRepository.InsertAsync(new SysTenantConfig
                {
                    TenantId = tenantId,
                    Key = entry.Key,
                    KeyValue = entry.KeyValue,
                    StartDate = SysTenantConfigDefaults.LegacyStartDate,
                    EndDate = SysTenantConfigDefaults.LegacyEndDate,
                    ConfigCategory = entry.Category,
                    ConfigDescription = entry.Description
                });
                added++;
            }

            await _unitOfWorkManager.Current.SaveChangesAsync();
            return added;
        }
    }
}
