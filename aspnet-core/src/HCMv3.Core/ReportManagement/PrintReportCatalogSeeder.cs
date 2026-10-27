using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.ReportManagement;

/// <summary>
/// Gives a tenant the standard Print Reports catalog (<see cref="PrintReportCatalog"/>). Called when a tenant is
/// created, so its "Print Reports" launcher is not empty; existing tenants get the same rows from the startup seed.
///
/// IDEMPOTENT, keyed on report name: a row the tenant already has -- renamed, deactivated or deleted -- is left
/// alone, so running this again never undoes a tenant's own change.
/// </summary>
public class PrintReportCatalogSeeder : ITransientDependency
{
    private readonly IRepository<PrintReport> _printReportRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public PrintReportCatalogSeeder(IRepository<PrintReport> printReportRepository, IUnitOfWorkManager unitOfWorkManager)
    {
        _printReportRepository = printReportRepository;
        _unitOfWorkManager = unitOfWorkManager;
    }

    /// <summary>Adds the catalog rows the tenant does not have yet; returns how many were added.</summary>
    public async Task<int> SeedAsync(int tenantId)
    {
        using (_unitOfWorkManager.Current.SetTenantId(tenantId))
        using (_unitOfWorkManager.Current.DisableFilter(AbpDataFilters.SoftDelete))
        {
            var existing = (await _printReportRepository.GetAll()
                    .Select(r => r.ReportName)
                    .ToListAsync())
                .Where(name => name != null)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var added = 0;
            foreach (var entry in PrintReportCatalog.Entries.Where(e => !existing.Contains(e.ReportName)))
            {
                await _printReportRepository.InsertAsync(new PrintReport
                {
                    TenantId = tenantId,
                    Code = entry.Code,
                    ReportName = entry.ReportName,
                    NameEn = entry.NameEn,
                    NameAr = entry.NameAr,
                    ReportCategory = entry.Category,
                    CustomPercentage = 0m,
                    ReportCreatedOn = DateTime.Now,
                    IsActive = true
                });
                added++;
            }

            await _unitOfWorkManager.Current.SaveChangesAsync();
            return added;
        }
    }
}
