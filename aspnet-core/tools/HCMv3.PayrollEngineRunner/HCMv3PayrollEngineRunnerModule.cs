using Abp.Events.Bus;
using Abp.Modules;
using Abp.Reflection.Extensions;
using Castle.MicroKernel.Registration;
using HCMv3.EntityFrameworkCore;

namespace HCMv3.PayrollEngineRunner;

/// <summary>
/// Minimal ABP host for running the ported Payroll Engine against a real database from the command line --
/// the payroll twin of <c>HCMv3.TimeEngineRunner</c>, and it exists for exactly the same reason:
/// <see cref="HCMv3.PayrollManagement.Services.IPayrollEngineService"/> is an <c>IDomainService</c> in
/// <c>HCMv3.Core</c> with no HTTP surface, so neither the test harness (seeded database) nor the web host
/// (authenticated session) is appropriate for a migration-verification run.
///
/// <para>The connection string is supplied by the caller -- never read from the application's
/// <c>appsettings.json</c>, which holds a shared team database.</para>
/// </summary>
[DependsOn(typeof(HCMv3EntityFrameworkCoreModule))]
public class HCMv3PayrollEngineRunnerModule : AbpModule
{
    /// <summary>Set by <c>Program</c> before bootstrapping.</summary>
    public static string ConnectionString { get; set; }

    public HCMv3PayrollEngineRunnerModule(HCMv3EntityFrameworkCoreModule efModule)
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
        IocManager.RegisterAssemblyByConvention(typeof(HCMv3PayrollEngineRunnerModule).GetAssembly());
        ServiceCollectionRegistrar.Register(IocManager);
    }
}
