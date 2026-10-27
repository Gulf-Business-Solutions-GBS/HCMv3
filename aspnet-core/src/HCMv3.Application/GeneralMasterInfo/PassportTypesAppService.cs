using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;

namespace HCMv3.GeneralMasterInfo;

[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_PassportTypes)]
public class PassportTypesAppService : SimpleLookupAppServiceBase<PassportType>, IPassportTypesAppService
{
    public PassportTypesAppService(IRepository<PassportType> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_GeneralMasterInfo_PassportTypes_Create;
    protected override string EditPermissionName => AppPermissions.Pages_GeneralMasterInfo_PassportTypes_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_GeneralMasterInfo_PassportTypes_Delete;
}
