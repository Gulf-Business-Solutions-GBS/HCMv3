using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.GatePassManagement;

[AbpAuthorize(AppPermissions.Pages_GatePassManagement_GatePassStatuses)]
public class GatePassStatusesAppService : SimpleLookupAppServiceBase<GatePassStatus>, IGatePassStatusesAppService
{
    public GatePassStatusesAppService(IRepository<GatePassStatus> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_GatePassManagement_GatePassStatuses_Create;
    protected override string EditPermissionName => AppPermissions.Pages_GatePassManagement_GatePassStatuses_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_GatePassManagement_GatePassStatuses_Delete;
}
