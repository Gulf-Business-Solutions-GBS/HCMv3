using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PayrollManagement;

[AbpAuthorize(AppPermissions.Pages_PayrollManagement_AccountTypes)]
public class AccountTypesAppService : SimpleLookupAppServiceBase<AccountType>, IAccountTypesAppService
{
    public AccountTypesAppService(IRepository<AccountType> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_PayrollManagement_AccountTypes_Create;
    protected override string EditPermissionName => AppPermissions.Pages_PayrollManagement_AccountTypes_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_PayrollManagement_AccountTypes_Delete;
}
