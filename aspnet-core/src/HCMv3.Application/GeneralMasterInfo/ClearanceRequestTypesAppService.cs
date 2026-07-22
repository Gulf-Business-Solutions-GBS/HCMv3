using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;

namespace HCMv3.GeneralMasterInfo;

[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_ClearanceRequestTypes)]
public class ClearanceRequestTypesAppService : SimpleLookupAppServiceBase<ClearanceRequestType>, IClearanceRequestTypesAppService
{
    public ClearanceRequestTypesAppService(IRepository<ClearanceRequestType> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_GeneralMasterInfo_ClearanceRequestTypes_Create;
    protected override string EditPermissionName => AppPermissions.Pages_GeneralMasterInfo_ClearanceRequestTypes_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_GeneralMasterInfo_ClearanceRequestTypes_Delete;
}
