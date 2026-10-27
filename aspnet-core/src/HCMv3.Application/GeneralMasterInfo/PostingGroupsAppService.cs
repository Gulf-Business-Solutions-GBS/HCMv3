using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;

namespace HCMv3.GeneralMasterInfo;

[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_PostingGroups)]
public class PostingGroupsAppService : SimpleLookupAppServiceBase<PostingGroup>, IPostingGroupsAppService
{
    public PostingGroupsAppService(IRepository<PostingGroup> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_GeneralMasterInfo_PostingGroups_Create;
    protected override string EditPermissionName => AppPermissions.Pages_GeneralMasterInfo_PostingGroups_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_GeneralMasterInfo_PostingGroups_Delete;
}
