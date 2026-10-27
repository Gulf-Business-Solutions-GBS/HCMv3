using Abp.Dependency;

namespace HCMv3.DashboardCustomization.Definitions.Cache;

public interface IDashboardDefinitionCacheManager : ITransientDependency
{
    DashboardDefinition Get(string name);

    void Set(DashboardDefinition definition);
}

