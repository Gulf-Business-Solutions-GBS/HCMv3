using Abp.Modules;
using Abp.Reflection.Extensions;

namespace HCMv3.Startup;

[DependsOn(typeof(HCMv3CoreModule))]
public class HCMv3GraphQLModule : AbpModule
{
    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(HCMv3GraphQLModule).GetAssembly());
    }
}

