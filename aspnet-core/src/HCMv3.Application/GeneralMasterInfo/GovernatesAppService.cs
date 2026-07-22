using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;

namespace HCMv3.GeneralMasterInfo;

[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_Governates)]
public class GovernatesAppService : SimpleLookupAppServiceBase<Governate>, IGovernatesAppService
{
    public GovernatesAppService(IRepository<Governate> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_GeneralMasterInfo_Governates_Create;
    protected override string EditPermissionName => AppPermissions.Pages_GeneralMasterInfo_Governates_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_GeneralMasterInfo_Governates_Delete;
}
