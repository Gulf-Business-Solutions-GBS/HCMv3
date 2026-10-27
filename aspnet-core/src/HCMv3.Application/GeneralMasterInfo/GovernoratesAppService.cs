using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;

namespace HCMv3.GeneralMasterInfo;

[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_Governorates)]
public class GovernoratesAppService : SimpleLookupAppServiceBase<Governorate>, IGovernoratesAppService
{
    public GovernoratesAppService(IRepository<Governorate> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_GeneralMasterInfo_Governorates_Create;
    protected override string EditPermissionName => AppPermissions.Pages_GeneralMasterInfo_Governorates_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_GeneralMasterInfo_Governorates_Delete;
}
