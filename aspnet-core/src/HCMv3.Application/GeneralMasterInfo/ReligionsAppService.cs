using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;

namespace HCMv3.GeneralMasterInfo;

[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_Religions)]
public class ReligionsAppService : SimpleLookupAppServiceBase<Religion>, IReligionsAppService
{
    public ReligionsAppService(IRepository<Religion> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_GeneralMasterInfo_Religions_Create;
    protected override string EditPermissionName => AppPermissions.Pages_GeneralMasterInfo_Religions_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_GeneralMasterInfo_Religions_Delete;
}
