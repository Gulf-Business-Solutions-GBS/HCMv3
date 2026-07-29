using Abp.Events.Bus;
using Abp.Modules;
using Abp.Reflection.Extensions;
using Castle.MicroKernel.Registration;
using HCMv3.EntityFrameworkCore;

namespace HCMv3.TimeEngineRunner;

/// <summary>
/// Minimal ABP host for running the ported domain engines against a real database from the command line.
///
/// <para>It exists because the Time Engine is an <c>IDomainService</c> in <c>HCMv3.Core</c> with no HTTP
/// surface, so the only other ways to exercise it are the test harness (which uses an in-memory/seeded
/// database and therefore cannot be pointed at migrated customer data) or the web host (which needs an
/// authenticated session). Neither is appropriate for a migration-verification run.</para>
///
/// <para>The connection string is supplied by the caller -- never read from the application's
/// <c>appsettings.json</c>, which holds a shared team database.</para>
/// </summary>
[DependsOn(typeof(HCMv3EntityFrameworkCoreModule))]
public class HCMv3TimeEngineRunnerModule : AbpModule
{
    /// <summary>Set by <c>Program</c> before bootstrapping.</summary>
    public static string ConnectionString { get; set; }

    public HCMv3TimeEngineRunnerModule(HCMv3EntityFrameworkCoreModule efModule)
    {
        // Never seed: the target already holds migrated customer data.
        efModule.SkipDbSeed = true;
    }

    public override void PreInitialize()
    {
        Configuration.DefaultNameOrConnectionString = ConnectionString;

        Configuration.BackgroundJobs.IsJobExecutionEnabled = false;
        Configuration.ReplaceService(typeof(IEventBus), () =>
        {
            IocManager.IocContainer.Register(Component.For<IEventBus>().Instance(NullEventBus.Instance));
        });
    }

    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(HCMv3TimeEngineRunnerModule).GetAssembly());
        ServiceCollectionRegistrar.Register(IocManager);
    }
}
