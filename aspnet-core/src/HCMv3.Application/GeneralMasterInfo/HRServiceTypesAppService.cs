using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;

namespace HCMv3.GeneralMasterInfo;

[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_HRServiceTypes)]
public class HRServiceTypesAppService : SimpleLookupAppServiceBase<HRServiceType>, IHRServiceTypesAppService
{
    public HRServiceTypesAppService(IRepository<HRServiceType> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_GeneralMasterInfo_HRServiceTypes_Create;
    protected override string EditPermissionName => AppPermissions.Pages_GeneralMasterInfo_HRServiceTypes_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_GeneralMasterInfo_HRServiceTypes_Delete;
}
