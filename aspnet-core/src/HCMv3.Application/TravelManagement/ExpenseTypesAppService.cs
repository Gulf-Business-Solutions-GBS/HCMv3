using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.TravelManagement;

[AbpAuthorize(AppPermissions.Pages_TravelManagement_ExpenseTypes)]
public class ExpenseTypesAppService : SimpleLookupAppServiceBase<ExpenseType>, IExpenseTypesAppService
{
    public ExpenseTypesAppService(IRepository<ExpenseType> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_TravelManagement_ExpenseTypes_Create;
    protected override string EditPermissionName => AppPermissions.Pages_TravelManagement_ExpenseTypes_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_TravelManagement_ExpenseTypes_Delete;
}
