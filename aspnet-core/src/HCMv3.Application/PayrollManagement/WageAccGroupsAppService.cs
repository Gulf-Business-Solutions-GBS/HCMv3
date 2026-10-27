using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PayrollManagement;

[AbpAuthorize(AppPermissions.Pages_PayrollManagement_WageAccGroups)]
public class WageAccGroupsAppService : SimpleLookupAppServiceBase<WageAccGroup>, IWageAccGroupsAppService
{
    public WageAccGroupsAppService(IRepository<WageAccGroup> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_PayrollManagement_WageAccGroups_Create;
    protected override string EditPermissionName => AppPermissions.Pages_PayrollManagement_WageAccGroups_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_PayrollManagement_WageAccGroups_Delete;
}
