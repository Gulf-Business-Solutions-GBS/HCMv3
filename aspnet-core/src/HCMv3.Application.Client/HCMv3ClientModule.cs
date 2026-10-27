using Abp.Modules;
using Abp.Reflection.Extensions;

namespace HCMv3;

public class HCMv3ClientModule : AbpModule
{
    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(HCMv3ClientModule).GetAssembly());
    }
}

