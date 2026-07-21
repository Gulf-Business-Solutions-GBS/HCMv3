using Abp.Modules;
using Abp.Reflection.Extensions;
using Castle.Windsor.MsDependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using HCMv3.Configure;
using HCMv3.Startup;
using HCMv3.Test.Base;

namespace HCMv3.GraphQL.Tests;

[DependsOn(
    typeof(HCMv3GraphQLModule),
    typeof(HCMv3TestBaseModule))]
public class HCMv3GraphQLTestModule : AbpModule
{
    public override void PreInitialize()
    {
        IServiceCollection services = new ServiceCollection();

        services.AddAndConfigureGraphQL();

        WindsorRegistrationHelper.CreateServiceProvider(IocManager.IocContainer, services);
    }

    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(HCMv3GraphQLTestModule).GetAssembly());
    }
}