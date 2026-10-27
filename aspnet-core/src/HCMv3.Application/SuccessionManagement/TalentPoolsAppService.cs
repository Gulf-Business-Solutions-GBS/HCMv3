using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.SuccessionManagement;

[AbpAuthorize(AppPermissions.Pages_SuccessionManagement_TalentPools)]
public class TalentPoolsAppService : SimpleLookupAppServiceBase<TalentPool>, ITalentPoolsAppService
{
    public TalentPoolsAppService(IRepository<TalentPool> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_SuccessionManagement_TalentPools_Create;
    protected override string EditPermissionName => AppPermissions.Pages_SuccessionManagement_TalentPools_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_SuccessionManagement_TalentPools_Delete;
}
