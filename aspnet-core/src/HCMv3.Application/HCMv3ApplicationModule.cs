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
        // Must come after AppAuthorizationProvider: it attaches HCM licence checks to the permissions defined there.
        Configuration.Authorization.Providers.Add<HCMv3.Licensing.HcmLicensingAuthorizationProvider>();
    }

    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(HCMv3ApplicationModule).GetAssembly());
    }
}
