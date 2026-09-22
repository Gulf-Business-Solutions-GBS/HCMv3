using System;
using System.IO;
using System.Net.Http;
using Abp.Modules;
using Castle.MicroKernel.Registration;
using HCMv3.Storage;
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

        // Stored files go to a throwaway folder per test run, never into the build output or a real store.
        IocManager.IocContainer.Register(
            Component.For<IFileStore>()
                     .Instance(new LocalFileStore(Path.Combine(Path.GetTempPath(), "hcmv3-test-files", Guid.NewGuid().ToString("N"))))
                     .Named("TestFileStore")
                     .IsDefault());
    }
}
