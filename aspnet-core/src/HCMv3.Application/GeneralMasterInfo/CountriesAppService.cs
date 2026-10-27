using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;

namespace HCMv3.GeneralMasterInfo;

[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_Countries)]
public class CountriesAppService : SimpleLookupAppServiceBase<Country>, ICountriesAppService
{
    public CountriesAppService(IRepository<Country> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_GeneralMasterInfo_Countries_Create;
    protected override string EditPermissionName => AppPermissions.Pages_GeneralMasterInfo_Countries_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_GeneralMasterInfo_Countries_Delete;
}
