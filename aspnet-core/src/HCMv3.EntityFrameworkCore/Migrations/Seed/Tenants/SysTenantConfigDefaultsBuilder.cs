using System;
using System.Linq;
using HCMv3.EntityFrameworkCore;
using HCMv3.SystemSecurity;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Migrations.Seed.Tenants;

/// <summary>
/// Seeds the default <see cref="SysTenantConfig"/> rows (<see cref="SysTenantConfigDefaults"/>) for one
/// tenant at startup. New tenants get the same rows at creation from <see cref="SysTenantConfigDefaultsSeeder"/>.
///
/// IDEMPOTENT: keyed on (TenantId, Key). A key the tenant already has is left completely alone.
/// </summary>
public class SysTenantConfigDefaultsBuilder
{
    private readonly HCMv3DbContext _context;
    private readonly int _tenantId;

    public SysTenantConfigDefaultsBuilder(HCMv3DbContext context, int tenantId)
    {
        _context = context;
        _tenantId = tenantId;
    }

    public void Create()
    {
        var existing = _context.SysTenantConfigs
            .IgnoreQueryFilters()
            .Where(r => r.TenantId == _tenantId)
            .Select(r => r.Key)
            .ToList()
            .Where(key => key != null)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var added = false;

        foreach (var entry in SysTenantConfigDefaults.Entries.Where(e => !existing.Contains(e.Key)))
        {
            _context.SysTenantConfigs.Add(new SysTenantConfig
            {
                // SuppressAutoSetTenantId is on during seeding, so TenantId must be set explicitly.
                TenantId = _tenantId,
                Key = entry.Key,
                KeyValue = entry.KeyValue,
                StartDate = SysTenantConfigDefaults.LegacyStartDate,
                EndDate = SysTenantConfigDefaults.LegacyEndDate,
                ConfigCategory = entry.Category,
                ConfigDescription = entry.Description
            });
            added = true;
        }

        if (added)
        {
            _context.SaveChanges();
        }
    }
}
