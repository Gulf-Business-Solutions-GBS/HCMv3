using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;

namespace HCMv3.GeneralMasterInfo;

[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_Titles)]
public class TitlesAppService : SimpleLookupAppServiceBase<Title>, ITitlesAppService
{
    public TitlesAppService(IRepository<Title> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_GeneralMasterInfo_Titles_Create;
    protected override string EditPermissionName => AppPermissions.Pages_GeneralMasterInfo_Titles_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_GeneralMasterInfo_Titles_Delete;
}
