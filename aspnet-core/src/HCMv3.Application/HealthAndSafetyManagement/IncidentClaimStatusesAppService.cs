using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.HealthAndSafetyManagement;

[AbpAuthorize(AppPermissions.Pages_HealthAndSafetyManagement_IncidentClaimStatuses)]
public class IncidentClaimStatusesAppService : SimpleLookupAppServiceBase<IncidentClaimStatus>, IIncidentClaimStatusesAppService
{
    public IncidentClaimStatusesAppService(IRepository<IncidentClaimStatus> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_HealthAndSafetyManagement_IncidentClaimStatuses_Create;
    protected override string EditPermissionName => AppPermissions.Pages_HealthAndSafetyManagement_IncidentClaimStatuses_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_HealthAndSafetyManagement_IncidentClaimStatuses_Delete;
}
