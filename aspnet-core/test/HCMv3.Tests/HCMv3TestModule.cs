using System.Net.Http;
using Abp.Modules;
using Castle.MicroKernel.Registration;
using HCMv3.Test.Base;

namespace HCMv3.Tests;

[DependsOn(typeof(HCMv3TestBaseModule))]
public class HCMv3TestModule : AbpModule
{
    public override void PreInitialize()
    {
        // The web host registers this through AddHttpClient; a test module has no equivalent, so without
        // it every service that reaches an external system fails to resolve.
        IocManager.IocContainer.Register(
            Component.For<IHttpClientFactory>()
                     .ImplementedBy<TestHttpClientFactory>()
                     .LifestyleSingleton());
    }
}
