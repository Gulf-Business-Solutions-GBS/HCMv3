using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.HealthAndSafetyManagement;

[AbpAuthorize(AppPermissions.Pages_HealthAndSafetyManagement_RiskPossibilities)]
public class RiskPossibilitiesAppService : SimpleLookupAppServiceBase<RiskPossibility>, IRiskPossibilitiesAppService
{
    public RiskPossibilitiesAppService(IRepository<RiskPossibility> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_HealthAndSafetyManagement_RiskPossibilities_Create;
    protected override string EditPermissionName => AppPermissions.Pages_HealthAndSafetyManagement_RiskPossibilities_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_HealthAndSafetyManagement_RiskPossibilities_Delete;
}
