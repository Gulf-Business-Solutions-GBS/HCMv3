using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;

namespace HCMv3.GeneralMasterInfo;

[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_EducationBranches)]
public class EducationBranchesAppService : SimpleLookupAppServiceBase<EducationBranch>, IEducationBranchesAppService
{
    public EducationBranchesAppService(IRepository<EducationBranch> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_GeneralMasterInfo_EducationBranches_Create;
    protected override string EditPermissionName => AppPermissions.Pages_GeneralMasterInfo_EducationBranches_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_GeneralMasterInfo_EducationBranches_Delete;
}
