using Abp.Dependency;
using Castle.Windsor.MsDependencyInjection;
using HCMv3.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace HCMv3.TimeEngineRunner;

/// <summary>
/// Bridges a Microsoft DI <c>ServiceCollection</c> into the Windsor container, exactly as
/// <c>HCMv3.Migrator</c> does. Without it, ABP's <c>DbPerTenantConnectionStringResolver</c> cannot resolve
/// <c>IServiceProvider</c> when the tenant cache is first touched, and any tenant-scoped query fails with
/// "No component for supporting the service System.IServiceProvider was found".
/// </summary>
public static class ServiceCollectionRegistrar
{
    public static void Register(IIocManager iocManager)
    {
        var services = new ServiceCollection();

        IdentityRegistrar.Register(services);

        WindsorRegistrationHelper.CreateServiceProvider(iocManager.IocContainer, services);
    }
}
