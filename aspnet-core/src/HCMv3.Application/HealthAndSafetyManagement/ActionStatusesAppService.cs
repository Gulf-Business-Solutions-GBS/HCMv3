using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.HealthAndSafetyManagement;

[AbpAuthorize(AppPermissions.Pages_HealthAndSafetyManagement_ActionStatuses)]
public class ActionStatusesAppService : SimpleLookupAppServiceBase<ActionStatus>, IActionStatusesAppService
{
    public ActionStatusesAppService(IRepository<ActionStatus> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_HealthAndSafetyManagement_ActionStatuses_Create;
    protected override string EditPermissionName => AppPermissions.Pages_HealthAndSafetyManagement_ActionStatuses_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_HealthAndSafetyManagement_ActionStatuses_Delete;
}
