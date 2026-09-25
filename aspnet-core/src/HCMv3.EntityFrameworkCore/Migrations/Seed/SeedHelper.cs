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

            //Default tenant configuration keys (legacy DefaultDataService.CreateDefaultSysTenantConfig). A tenant's
            //first Manage*Settings rows are built from these, so they must exist before settings are first read.
            new SysTenantConfigDefaultsBuilder(context, tenantId).Create();

            //Workflow/ESS transaction-type catalog. Seeded dormant (workflow and ESS both off) -- registering a
            //type must not start routing live transactions through approval. See the builder's remarks.
            //
            //INSIDE the loop, like the print report catalog above and for the same reason. Seeded against
            //tenant 1 alone, every other tenant began life with an empty catalog: no request type could be
            //routed, because the resolver looks the type up by code WITHIN the tenant and finds nothing. The
            //symptom is not an error -- an unregistered type is treated as not workflow-managed, so requests
            //save straight to the table and never reach an approver, which reads as "approvals do not work".
            new WorkflowTransactionTypeBuilder(context, tenantId).Create();

            //The two workflow notification configurations. Without one the dispatcher finds nothing for a
            //step and returns silently, so a tenant routes approvals correctly and tells nobody -- the
            //Approvals list fills while the bell stays empty.
            new WorkflowNotificationConfigBuilder(context, tenantId).Create();

            //The "Employee" role, granted Ess.* and nothing else. This is what lets one application serve HR
            //and employees without a separate ESS project.
            //
            //Also per tenant, and for the same reason: a tenant without it has no role to give employees, so
            //self-service is unreachable there until somebody builds one by hand.
            new EmployeeRoleBuilder(context, tenantId).Create();
        }
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

