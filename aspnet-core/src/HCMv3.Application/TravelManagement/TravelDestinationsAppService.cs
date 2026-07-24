using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.TravelManagement;

[AbpAuthorize(AppPermissions.Pages_TravelManagement_TravelDestinations)]
public class TravelDestinationsAppService : SimpleLookupAppServiceBase<TravelDestination>, ITravelDestinationsAppService
{
    public TravelDestinationsAppService(IRepository<TravelDestination> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_TravelManagement_TravelDestinations_Create;
    protected override string EditPermissionName => AppPermissions.Pages_TravelManagement_TravelDestinations_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_TravelManagement_TravelDestinations_Delete;
}
