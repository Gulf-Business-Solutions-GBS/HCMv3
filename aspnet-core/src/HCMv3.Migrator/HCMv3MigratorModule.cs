using Abp.AspNetZeroCore;
using Abp.Events.Bus;
using Abp.Modules;
using Abp.Reflection.Extensions;
using Castle.MicroKernel.Registration;
using Microsoft.Extensions.Configuration;
using HCMv3.Configuration;
using HCMv3.EntityFrameworkCore;
using HCMv3.Migrator.DependencyInjection;

namespace HCMv3.Migrator;

[DependsOn(typeof(HCMv3EntityFrameworkCoreModule))]
public class HCMv3MigratorModule : AbpModule
{
    private readonly IConfigurationRoot _appConfiguration;

    public HCMv3MigratorModule(HCMv3EntityFrameworkCoreModule abpZeroTemplateEntityFrameworkCoreModule)
    {
        abpZeroTemplateEntityFrameworkCoreModule.SkipDbSeed = true;

        _appConfiguration = AppConfigurations.Get(
            typeof(HCMv3MigratorModule).GetAssembly().GetDirectoryPathOrNull(),
            addUserSecrets: true
        );
    }

    public override void PreInitialize()
    {
        Configuration.DefaultNameOrConnectionString = _appConfiguration.GetConnectionString(
            HCMv3Consts.ConnectionStringName
            );
        Configuration.Modules.AspNetZero().LicenseCode = _appConfiguration["AbpZeroLicenseCode"];

        Configuration.BackgroundJobs.IsJobExecutionEnabled = false;
        Configuration.ReplaceService(typeof(IEventBus), () =>
        {
            IocManager.IocContainer.Register(
                Component.For<IEventBus>().Instance(NullEventBus.Instance)
            );
        });
    }

    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(HCMv3MigratorModule).GetAssembly());
        ServiceCollectionRegistrar.Register(IocManager);
    }
}

