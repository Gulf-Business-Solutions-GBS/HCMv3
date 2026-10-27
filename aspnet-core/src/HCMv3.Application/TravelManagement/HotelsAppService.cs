using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.TravelManagement;

[AbpAuthorize(AppPermissions.Pages_TravelManagement_Hotels)]
public class HotelsAppService : SimpleLookupAppServiceBase<Hotel>, IHotelsAppService
{
    public HotelsAppService(IRepository<Hotel> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_TravelManagement_Hotels_Create;
    protected override string EditPermissionName => AppPermissions.Pages_TravelManagement_Hotels_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_TravelManagement_Hotels_Delete;
}
