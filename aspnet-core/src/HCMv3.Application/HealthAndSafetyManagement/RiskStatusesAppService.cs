using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.HealthAndSafetyManagement;

[AbpAuthorize(AppPermissions.Pages_HealthAndSafetyManagement_RiskStatuses)]
public class RiskStatusesAppService : SimpleLookupAppServiceBase<RiskStatus>, IRiskStatusesAppService
{
    public RiskStatusesAppService(IRepository<RiskStatus> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_HealthAndSafetyManagement_RiskStatuses_Create;
    protected override string EditPermissionName => AppPermissions.Pages_HealthAndSafetyManagement_RiskStatuses_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_HealthAndSafetyManagement_RiskStatuses_Delete;
}
