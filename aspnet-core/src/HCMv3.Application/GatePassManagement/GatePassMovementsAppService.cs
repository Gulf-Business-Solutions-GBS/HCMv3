using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.GatePassManagement;

[AbpAuthorize(AppPermissions.Pages_GatePassManagement_GatePassMovements)]
public class GatePassMovementsAppService : SimpleLookupAppServiceBase<GatePassMovement>, IGatePassMovementsAppService
{
    public GatePassMovementsAppService(IRepository<GatePassMovement> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_GatePassManagement_GatePassMovements_Create;
    protected override string EditPermissionName => AppPermissions.Pages_GatePassManagement_GatePassMovements_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_GatePassManagement_GatePassMovements_Delete;
}
