using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;

namespace HCMv3.GeneralMasterInfo;

[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_States)]
public class StatesAppService : SimpleLookupAppServiceBase<State>, IStatesAppService
{
    public StatesAppService(IRepository<State> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_GeneralMasterInfo_States_Create;
    protected override string EditPermissionName => AppPermissions.Pages_GeneralMasterInfo_States_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_GeneralMasterInfo_States_Delete;
}
