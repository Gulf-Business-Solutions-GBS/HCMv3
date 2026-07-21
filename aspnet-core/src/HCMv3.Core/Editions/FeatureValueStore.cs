using Abp.Application.Features;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.MultiTenancy;
using Abp.Runtime.Caching;
using HCMv3.Authorization.Users;
using HCMv3.MultiTenancy;

namespace HCMv3.Editions;

public class FeatureValueStore : AbpFeatureValueStore<Tenant, User>
{
    public FeatureValueStore(
        ICacheManager cacheManager,
        IRepository<TenantFeatureSetting, long> tenantFeatureSettingRepository,
        IRepository<Tenant> tenantRepository,
        IRepository<EditionFeatureSetting, long> editionFeatureSettingRepository,
        IFeatureManager featureManager,
        IUnitOfWorkManager unitOfWorkManager)
        : base(cacheManager,
              tenantFeatureSettingRepository,
              tenantRepository,
              editionFeatureSettingRepository,
              featureManager,
              unitOfWorkManager)
    {
    }
}

