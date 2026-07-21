using Abp.Mapperly;
using Abp.Configuration.Startup;
using Abp.Modules;
using Abp.Reflection.Extensions;
using HCMv3.ApiClient;
using HCMv3.Maui.Core;

namespace HCMv3.Maui;

[DependsOn(typeof(HCMv3ClientModule), typeof(AbpMapperlyModule))]
public class HCMv3MauiModule : AbpModule
{
    public override void PreInitialize()
    {
        Configuration.Localization.IsEnabled = false;
        Configuration.BackgroundJobs.IsJobExecutionEnabled = false;

        Configuration.ReplaceService<IApplicationContext, MauiApplicationContext>();
    }

    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(HCMv3MauiModule).GetAssembly());
    }
}