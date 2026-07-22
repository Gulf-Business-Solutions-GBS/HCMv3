using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.OrganizationManagement;

[AbpAuthorize(AppPermissions.Pages_OrganizationManagement_CostElementTypes)]
public class CostElementTypesAppService : SimpleLookupAppServiceBase<CostElementType>, ICostElementTypesAppService
{
    public CostElementTypesAppService(IRepository<CostElementType> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_OrganizationManagement_CostElementTypes_Create;
    protected override string EditPermissionName => AppPermissions.Pages_OrganizationManagement_CostElementTypes_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_OrganizationManagement_CostElementTypes_Delete;
}
