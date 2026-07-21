using Abp.Modules;
using Abp.Reflection.Extensions;

namespace HCMv3;

[DependsOn(typeof(HCMv3CoreSharedModule))]
public class HCMv3ApplicationSharedModule : AbpModule
{
    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(HCMv3ApplicationSharedModule).GetAssembly());
    }
}

