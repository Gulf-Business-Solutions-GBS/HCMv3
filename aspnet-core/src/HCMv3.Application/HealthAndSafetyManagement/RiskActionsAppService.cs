using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.HealthAndSafetyManagement;

[AbpAuthorize(AppPermissions.Pages_HealthAndSafetyManagement_RiskActions)]
public class RiskActionsAppService : SimpleLookupAppServiceBase<RiskAction>, IRiskActionsAppService
{
    public RiskActionsAppService(IRepository<RiskAction> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_HealthAndSafetyManagement_RiskActions_Create;
    protected override string EditPermissionName => AppPermissions.Pages_HealthAndSafetyManagement_RiskActions_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_HealthAndSafetyManagement_RiskActions_Delete;
}
