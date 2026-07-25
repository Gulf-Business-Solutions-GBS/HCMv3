using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.HealthAndSafetyManagement;

[AbpAuthorize(AppPermissions.Pages_HealthAndSafetyManagement_IncidentSeverities)]
public class IncidentSeveritiesAppService : SimpleLookupAppServiceBase<IncidentSeverity>, IIncidentSeveritiesAppService
{
    public IncidentSeveritiesAppService(IRepository<IncidentSeverity> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_HealthAndSafetyManagement_IncidentSeverities_Create;
    protected override string EditPermissionName => AppPermissions.Pages_HealthAndSafetyManagement_IncidentSeverities_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_HealthAndSafetyManagement_IncidentSeverities_Delete;
}
