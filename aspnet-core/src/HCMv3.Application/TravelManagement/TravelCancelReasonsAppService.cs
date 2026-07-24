using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.TravelManagement;

[AbpAuthorize(AppPermissions.Pages_TravelManagement_TravelCancelReasons)]
public class TravelCancelReasonsAppService : SimpleLookupAppServiceBase<TravelCancelReason>, ITravelCancelReasonsAppService
{
    public TravelCancelReasonsAppService(IRepository<TravelCancelReason> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_TravelManagement_TravelCancelReasons_Create;
    protected override string EditPermissionName => AppPermissions.Pages_TravelManagement_TravelCancelReasons_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_TravelManagement_TravelCancelReasons_Delete;
}
