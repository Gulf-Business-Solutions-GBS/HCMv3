using Abp.AspNetZeroCore;
using Abp.Configuration.Startup;
using Abp.Modules;
using Abp.Reflection.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using HCMv3.Configuration;
using HCMv3.EntityFrameworkCore;

namespace HCMv3.Web.Public.Startup;

[DependsOn(
    typeof(HCMv3WebCoreModule)
)]
public class HCMv3WebFrontEndModule : AbpModule
{
    private readonly IConfigurationRoot _appConfiguration;

    public HCMv3WebFrontEndModule(IWebHostEnvironment env, HCMv3EntityFrameworkCoreModule abpZeroTemplateEntityFrameworkCoreModule)
    {
        _appConfiguration = env.GetAppConfiguration();
        abpZeroTemplateEntityFrameworkCoreModule.SkipDbSeed = true;
    }

    public override void PreInitialize()
    {
        Configuration.Modules.AbpWebCommon().MultiTenancy.DomainFormat = _appConfiguration["App:WebSiteRootAddress"] ?? "https://localhost:44303/";
        Configuration.Modules.AspNetZero().LicenseCode = _appConfiguration["AbpZeroLicenseCode"];

        //Changed AntiForgery token/cookie names to not conflict to the main application while redirections.
        Configuration.Modules.AbpWebCommon().AntiForgery.TokenCookieName = "Public-XSRF-TOKEN";
        Configuration.Modules.AbpWebCommon().AntiForgery.TokenHeaderName = "Public-X-XSRF-TOKEN";

        Configuration.BackgroundJobs.IsJobExecutionEnabled = false;

        Configuration.Navigation.Providers.Add<FrontEndNavigationProvider>();
    }

    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(HCMv3WebFrontEndModule).GetAssembly());
    }
}

