using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.HealthAndSafetyManagement;

[AbpAuthorize(AppPermissions.Pages_HealthAndSafetyManagement_IncidentTypes)]
public class IncidentTypesAppService : SimpleLookupAppServiceBase<IncidentType>, IIncidentTypesAppService
{
    public IncidentTypesAppService(IRepository<IncidentType> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_HealthAndSafetyManagement_IncidentTypes_Create;
    protected override string EditPermissionName => AppPermissions.Pages_HealthAndSafetyManagement_IncidentTypes_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_HealthAndSafetyManagement_IncidentTypes_Delete;
}
