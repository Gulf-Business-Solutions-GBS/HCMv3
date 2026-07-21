using Abp.Modules;
using Abp.Reflection.Extensions;

namespace HCMv3;

public class HCMv3CoreSharedModule : AbpModule
{
    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(HCMv3CoreSharedModule).GetAssembly());
    }
}

