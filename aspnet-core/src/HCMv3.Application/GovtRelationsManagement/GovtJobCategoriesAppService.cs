using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.GovtRelationsManagement;

[AbpAuthorize(AppPermissions.Pages_GovtRelationsManagement_GovtJobCategories)]
public class GovtJobCategoriesAppService : SimpleLookupAppServiceBase<GovtJobCategory>, IGovtJobCategoriesAppService
{
    public GovtJobCategoriesAppService(IRepository<GovtJobCategory> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_GovtRelationsManagement_GovtJobCategories_Create;
    protected override string EditPermissionName => AppPermissions.Pages_GovtRelationsManagement_GovtJobCategories_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_GovtRelationsManagement_GovtJobCategories_Delete;
}
