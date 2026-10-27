using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;

namespace HCMv3.GeneralMasterInfo;

[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_BudgetingGroups)]
public class BudgetingGroupsAppService : SimpleLookupAppServiceBase<BudgetingGroup>, IBudgetingGroupsAppService
{
    public BudgetingGroupsAppService(IRepository<BudgetingGroup> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_GeneralMasterInfo_BudgetingGroups_Create;
    protected override string EditPermissionName => AppPermissions.Pages_GeneralMasterInfo_BudgetingGroups_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_GeneralMasterInfo_BudgetingGroups_Delete;
}
