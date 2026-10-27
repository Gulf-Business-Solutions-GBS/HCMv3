using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.SuccessionManagement;

[AbpAuthorize(AppPermissions.Pages_SuccessionManagement_Rankings)]
public class RankingsAppService : SimpleLookupAppServiceBase<Ranking>, IRankingsAppService
{
    public RankingsAppService(IRepository<Ranking> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_SuccessionManagement_Rankings_Create;
    protected override string EditPermissionName => AppPermissions.Pages_SuccessionManagement_Rankings_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_SuccessionManagement_Rankings_Delete;
}
