using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;

namespace HCMv3.GeneralMasterInfo;

[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_Educations)]
public class EducationsAppService : SimpleLookupAppServiceBase<Education>, IEducationsAppService
{
    public EducationsAppService(IRepository<Education> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_GeneralMasterInfo_Educations_Create;
    protected override string EditPermissionName => AppPermissions.Pages_GeneralMasterInfo_Educations_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_GeneralMasterInfo_Educations_Delete;
}
