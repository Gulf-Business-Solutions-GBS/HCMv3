using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.TravelManagement;

[AbpAuthorize(AppPermissions.Pages_TravelManagement_TravelAirlines)]
public class TravelAirlinesAppService : SimpleLookupAppServiceBase<TravelAirline>, ITravelAirlinesAppService
{
    public TravelAirlinesAppService(IRepository<TravelAirline> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_TravelManagement_TravelAirlines_Create;
    protected override string EditPermissionName => AppPermissions.Pages_TravelManagement_TravelAirlines_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_TravelManagement_TravelAirlines_Delete;
}
