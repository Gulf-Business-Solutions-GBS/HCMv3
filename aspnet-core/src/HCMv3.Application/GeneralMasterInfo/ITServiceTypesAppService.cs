using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;

namespace HCMv3.GeneralMasterInfo;

[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_ITServiceTypes)]
public class ITServiceTypesAppService : SimpleLookupAppServiceBase<ITServiceType>, IITServiceTypesAppService
{
    public ITServiceTypesAppService(IRepository<ITServiceType> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_GeneralMasterInfo_ITServiceTypes_Create;
    protected override string EditPermissionName => AppPermissions.Pages_GeneralMasterInfo_ITServiceTypes_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_GeneralMasterInfo_ITServiceTypes_Delete;
}
