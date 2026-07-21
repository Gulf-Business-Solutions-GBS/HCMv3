using Abp;
using Abp.Dependency;
using Abp.EntityFrameworkCore.Configuration;
using Abp.Modules;
using Abp.OpenIddict.EntityFrameworkCore;
using Abp.Reflection.Extensions;
using Abp.Zero.EntityFrameworkCore;
using HCMv3.Configuration;
using HCMv3.EntityHistory;
using HCMv3.Migrations.Seed;

namespace HCMv3.EntityFrameworkCore;

[DependsOn(
    typeof(AbpZeroCoreEntityFrameworkCoreModule),
    typeof(HCMv3CoreModule),
    typeof(AbpZeroCoreOpenIddictEntityFrameworkCoreModule)
)]
public class HCMv3EntityFrameworkCoreModule : AbpModule
{
    /* Used it tests to skip DbContext registration, in order to use in-memory database of EF Core */
    public bool SkipDbContextRegistration { get; set; }

    public bool SkipDbSeed { get; set; }

    public override void PreInitialize()
    {
        if (!SkipDbContextRegistration)
        {
            Configuration.Modules.AbpEfCore().AddDbContext<HCMv3DbContext>(options =>
            {
                if (options.ExistingConnection != null)
                {
                    HCMv3DbContextConfigurer.Configure(options.DbContextOptions,
                        options.ExistingConnection);
                }
                else
                {
                    HCMv3DbContextConfigurer.Configure(options.DbContextOptions,
                        options.ConnectionString);
                }
            });
        }

        // Set this setting to true for enabling entity history.
        Configuration.EntityHistory.IsEnabled = true;

        // Uncomment below line to write change logs for the entities below:
        Configuration.EntityHistory.Selectors.Add("HCMv3Entities", EntityHistoryHelper.TrackedTypes);
        Configuration.CustomConfigProviders.Add(new EntityHistoryConfigProvider(Configuration));
    }

    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(HCMv3EntityFrameworkCoreModule).GetAssembly());
    }

    public override void PostInitialize()
    {
        var configurationAccessor = IocManager.Resolve<IAppConfigurationAccessor>();

        using (var scope = IocManager.CreateScope())
        {
            if (!SkipDbSeed && scope.Resolve<DatabaseCheckHelper>()
                    .Exist(configurationAccessor.Configuration["ConnectionStrings:Default"]))
            {
                SeedHelper.SeedHostDb(IocManager);
            }
        }
    }
}

