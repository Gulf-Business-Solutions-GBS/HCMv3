using System;
using System.Linq;
using System.Transactions;
using Abp.Dependency;
using Abp.Domain.Uow;
using Abp.EntityFrameworkCore.Uow;
using Abp.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using HCMv3.EntityFrameworkCore;
using HCMv3.Migrations.Seed.Host;
using HCMv3.Migrations.Seed.Tenants;

namespace HCMv3.Migrations.Seed;

public static class SeedHelper
{
    public static void SeedHostDb(IIocResolver iocResolver)
    {
        WithDbContext<HCMv3DbContext>(iocResolver, SeedHostDb);
    }

    public static void SeedHostDb(HCMv3DbContext context)
    {
        context.SuppressAutoSetTenantId = true;

        //Host seed
        new InitialHostDbBuilder(context).Create();

        //Default tenant seed (in host database).
        new DefaultTenantBuilder(context).Create();
        new TenantRoleAndUserBuilder(context, 1).Create();

        //Print report catalog (rows the "Print Reports" launcher lists) for EVERY tenant, not only the default one:
        //tenants created before tenant creation seeded it had an empty launcher. Existing rows are skipped.
        var tenantIds = context.Tenants.IgnoreQueryFilters().Where(t => !t.IsDeleted).Select(t => t.Id).ToList();
        foreach (var tenantId in tenantIds)
        {
            new PrintReportCatalogBuilder(context, tenantId).Create();

            //Default payroll-sensitive list, which the payroll lock reads. Companies that already had one keep it.
            new PayrollSensitiveObjectBuilder(context, tenantId).Create();
        }

        //Workflow/ESS transaction-type catalog. Seeded dormant (workflow and ESS both off) -- registering a
        //type must not start routing live transactions through approval. See the builder's remarks.
        new WorkflowTransactionTypeBuilder(context, 1).Create();

        //The "Employee" role, granted Ess.* and nothing else. This is what lets one application serve HR
        //and employees without a separate ESS project.
        new EmployeeRoleBuilder(context, 1).Create();
    }

    private static void WithDbContext<TDbContext>(IIocResolver iocResolver, Action<TDbContext> contextAction)
        where TDbContext : DbContext
    {
        using (var uowManager = iocResolver.ResolveAsDisposable<IUnitOfWorkManager>())
        {
            using (var uow = uowManager.Object.Begin(TransactionScopeOption.Suppress))
            {
                var context = uowManager.Object.Current.GetDbContext<TDbContext>(MultiTenancySides.Host);

                contextAction(context);

                uow.Complete();
            }
        }
    }
}

