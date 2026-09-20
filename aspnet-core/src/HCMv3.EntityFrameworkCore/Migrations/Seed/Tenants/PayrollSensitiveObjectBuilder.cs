using System;
using System.Linq;
using HCMv3.EntityFrameworkCore;
using HCMv3.PayrollManagement.PayrollLock;
using HCMv3.SystemSecurity;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Migrations.Seed.Tenants;

/// <summary>
/// Seeds the default payroll-sensitive list (<see cref="PayrollSensitiveDefaults"/>) for one company at startup,
/// so companies created before the payroll lock existed get it too. New companies get the same rows at creation
/// from <c>PayrollSensitiveObjectSeeder</c>.
///
/// IDEMPOTENT: keyed on (TenantId, FullName), deleted rows included. A row the company has unticked or removed is
/// left alone -- re-running must not put back something the company took out.
/// </summary>
public class PayrollSensitiveObjectBuilder
{
    private readonly HCMv3DbContext _context;
    private readonly int _tenantId;

    public PayrollSensitiveObjectBuilder(HCMv3DbContext context, int tenantId)
    {
        _context = context;
        _tenantId = tenantId;
    }

    public void Create()
    {
        var existing = _context.SysObjects
            .IgnoreQueryFilters()
            .Where(r => r.TenantId == _tenantId && r.FullName != null)
            .Select(r => r.FullName)
            .ToList()
            .ToHashSet(StringComparer.Ordinal);

        var added = false;
        foreach (var type in PayrollSensitiveDefaults.Types.Where(t => !existing.Contains(t.FullName)))
        {
            _context.SysObjects.Add(new SysObject
            {
                // SuppressAutoSetTenantId is on during seeding, so TenantId must be set explicitly.
                TenantId = _tenantId,
                Name = type.Name,
                FullName = type.FullName,
                IsPayrollSensitive = true,
                IsTimeSensitive = PayrollSensitiveDefaults.TimeSensitiveTypes.Contains(type),
            });

            added = true;
        }

        if (added)
        {
            _context.SaveChanges();
        }
    }
}
