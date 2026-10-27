using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.HealthAndSafetyManagement;

[AbpAuthorize(AppPermissions.Pages_HealthAndSafetyManagement_RiskImpacts)]
public class RiskImpactsAppService : SimpleLookupAppServiceBase<RiskImpact>, IRiskImpactsAppService
{
    public RiskImpactsAppService(IRepository<RiskImpact> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_HealthAndSafetyManagement_RiskImpacts_Create;
    protected override string EditPermissionName => AppPermissions.Pages_HealthAndSafetyManagement_RiskImpacts_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_HealthAndSafetyManagement_RiskImpacts_Delete;
}
