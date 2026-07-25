using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.HealthAndSafetyManagement;

[AbpAuthorize(AppPermissions.Pages_HealthAndSafetyManagement_IncidentStatuses)]
public class IncidentStatusesAppService : SimpleLookupAppServiceBase<IncidentStatus>, IIncidentStatusesAppService
{
    public IncidentStatusesAppService(IRepository<IncidentStatus> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_HealthAndSafetyManagement_IncidentStatuses_Create;
    protected override string EditPermissionName => AppPermissions.Pages_HealthAndSafetyManagement_IncidentStatuses_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_HealthAndSafetyManagement_IncidentStatuses_Delete;
}
