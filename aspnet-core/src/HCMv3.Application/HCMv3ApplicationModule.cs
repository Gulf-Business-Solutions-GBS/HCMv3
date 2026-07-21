using Abp.Modules;
using Abp.Reflection.Extensions;
using HCMv3.Authorization;

namespace HCMv3;

/// <summary>
/// Application layer module of the application.
/// </summary>
[DependsOn(
    typeof(HCMv3ApplicationSharedModule),
    typeof(HCMv3CoreModule)
    )]
public class HCMv3ApplicationModule : AbpModule
{
    public override void PreInitialize()
    {
        //Adding authorization providers
        Configuration.Authorization.Providers.Add<AppAuthorizationProvider>();
    }

    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(HCMv3ApplicationModule).GetAssembly());
    }
}
