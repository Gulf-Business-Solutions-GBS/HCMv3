using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;

namespace HCMv3.GeneralMasterInfo;

[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_LanguageInfos)]
public class LanguageInfosAppService : SimpleLookupAppServiceBase<LanguageInfo>, ILanguageInfosAppService
{
    public LanguageInfosAppService(IRepository<LanguageInfo> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_GeneralMasterInfo_LanguageInfos_Create;
    protected override string EditPermissionName => AppPermissions.Pages_GeneralMasterInfo_LanguageInfos_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_GeneralMasterInfo_LanguageInfos_Delete;
}
