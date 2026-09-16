using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Uow;
using Abp.MultiTenancy;
using HCMv3.MultiTenancy;
using HCMv3.MultiTenancy.Dto;
using HCMv3.ReportManagement;
using HCMv3.Tests;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.ReportManagement;

/// <summary>
/// A tenant's "Print Reports" launcher lists its own PrintReports rows, so every tenant needs the standard catalog:
/// new tenants at creation, and without undoing what a tenant has changed when it runs again.
/// </summary>
// ReSharper disable once InconsistentNaming
public class PrintReportCatalogSeeder_Tests : AppTestBase
{
    [MultiTenantFact]
    public async Task Should_Give_A_New_Tenant_The_Whole_Catalog()
    {
        LoginAsHostAdmin();

        await Resolve<ITenantAppService>().CreateTenant(new CreateTenantInput
        {
            TenancyName = "reportsTenant",
            Name = "reportsTenantDisplay",
            AdminName = "admin",
            AdminSurname = "admin",
            AdminEmailAddress = "admin@reportstenant.com",
            AdminPassword = "123qwe",
            IsActive = true
        });

        var tenantId = UsingDbContext(null, context => context.Tenants.Single(t => t.TenancyName == "reportsTenant").Id);
        var names = UsingDbContext(null, context => context.PrintReports.IgnoreQueryFilters()
            .Where(r => r.TenantId == tenantId && !r.IsDeleted && r.IsActive)
            .Select(r => r.ReportName)
            .ToList());

        names.Count.ShouldBe(PrintReportCatalog.Entries.Count);
        names.ShouldContain("PayslipEn");
    }

    [Fact]
    public async Task Should_Add_Only_What_Is_Missing_And_Leave_A_Tenants_Changes_Alone()
    {
        LoginAsHostAdmin();

        // A tenant with no catalog rows yet (removing rows would only soft-delete them, and those are kept).
        var tenantId = UsingDbContext(null, context =>
        {
            var tenant = context.Tenants.Add(new Tenant("seedTenant", "seedTenant")).Entity;
            context.SaveChanges();
            return tenant.Id;
        });
        UsingDbContext(null, context =>
        {
            context.PrintReports.Add(new PrintReport
            {
                TenantId = tenantId,
                Code = "PR001",
                ReportName = "PayslipEn",
                NameEn = "Our renamed payslip",
                NameAr = "كشف",
                IsActive = false
            });
        });

        var added = await SeedAsync(tenantId);

        added.ShouldBe(PrintReportCatalog.Entries.Count - 1);
        var payslip = UsingDbContext(null, context => context.PrintReports.IgnoreQueryFilters()
            .Single(r => r.TenantId == tenantId && r.ReportName == "PayslipEn"));
        payslip.NameEn.ShouldBe("Our renamed payslip");
        payslip.IsActive.ShouldBeFalse();

        (await SeedAsync(tenantId)).ShouldBe(0);
    }

    private async Task<int> SeedAsync(int tenantId)
    {
        using (var uow = Resolve<IUnitOfWorkManager>().Begin())
        {
            var added = await Resolve<PrintReportCatalogSeeder>().SeedAsync(tenantId);
            await uow.CompleteAsync();
            return added;
        }
    }
}
