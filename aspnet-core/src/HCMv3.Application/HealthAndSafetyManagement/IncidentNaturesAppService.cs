using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.HealthAndSafetyManagement;

[AbpAuthorize(AppPermissions.Pages_HealthAndSafetyManagement_IncidentNatures)]
public class IncidentNaturesAppService : SimpleLookupAppServiceBase<IncidentNature>, IIncidentNaturesAppService
{
    public IncidentNaturesAppService(IRepository<IncidentNature> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_HealthAndSafetyManagement_IncidentNatures_Create;
    protected override string EditPermissionName => AppPermissions.Pages_HealthAndSafetyManagement_IncidentNatures_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_HealthAndSafetyManagement_IncidentNatures_Delete;
}
