using Abp.Dependency;
using Abp.Reflection.Extensions;
using Microsoft.Extensions.Configuration;
using HCMv3.Configuration;

namespace HCMv3.Test.Base;

public class TestAppConfigurationAccessor : IAppConfigurationAccessor, ISingletonDependency
{
    public IConfigurationRoot Configuration { get; }

    public TestAppConfigurationAccessor()
    {
        Configuration = AppConfigurations.Get(
            typeof(HCMv3TestBaseModule).GetAssembly().GetDirectoryPathOrNull()
        );
    }
}
