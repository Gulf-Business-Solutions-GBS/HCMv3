using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.OrganizationManagement;

[AbpAuthorize(AppPermissions.Pages_OrganizationManagement_CostCenterGroups)]
public class CostCenterGroupsAppService : SimpleLookupAppServiceBase<CostCenterGroup>, ICostCenterGroupsAppService
{
    public CostCenterGroupsAppService(IRepository<CostCenterGroup> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_OrganizationManagement_CostCenterGroups_Create;
    protected override string EditPermissionName => AppPermissions.Pages_OrganizationManagement_CostCenterGroups_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_OrganizationManagement_CostCenterGroups_Delete;
}
