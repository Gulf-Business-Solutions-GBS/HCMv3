using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;

namespace HCMv3.GeneralMasterInfo;

[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_Universities)]
public class UniversitiesAppService : SimpleLookupAppServiceBase<University>, IUniversitiesAppService
{
    public UniversitiesAppService(IRepository<University> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_GeneralMasterInfo_Universities_Create;
    protected override string EditPermissionName => AppPermissions.Pages_GeneralMasterInfo_Universities_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_GeneralMasterInfo_Universities_Delete;
}
