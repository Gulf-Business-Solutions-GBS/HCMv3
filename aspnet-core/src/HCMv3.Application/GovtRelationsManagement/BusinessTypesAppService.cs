using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.GovtRelationsManagement;

[AbpAuthorize(AppPermissions.Pages_GovtRelationsManagement_BusinessTypes)]
public class BusinessTypesAppService : SimpleLookupAppServiceBase<BusinessType>, IBusinessTypesAppService
{
    public BusinessTypesAppService(IRepository<BusinessType> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_GovtRelationsManagement_BusinessTypes_Create;
    protected override string EditPermissionName => AppPermissions.Pages_GovtRelationsManagement_BusinessTypes_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_GovtRelationsManagement_BusinessTypes_Delete;
}
