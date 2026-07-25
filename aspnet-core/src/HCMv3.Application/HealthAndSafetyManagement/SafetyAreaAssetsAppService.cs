using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.HealthAndSafetyManagement;

[AbpAuthorize(AppPermissions.Pages_HealthAndSafetyManagement_SafetyAreaAssets)]
public class SafetyAreaAssetsAppService : SimpleLookupAppServiceBase<SafetyAreaAsset>, ISafetyAreaAssetsAppService
{
    public SafetyAreaAssetsAppService(IRepository<SafetyAreaAsset> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_HealthAndSafetyManagement_SafetyAreaAssets_Create;
    protected override string EditPermissionName => AppPermissions.Pages_HealthAndSafetyManagement_SafetyAreaAssets_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_HealthAndSafetyManagement_SafetyAreaAssets_Delete;
}
