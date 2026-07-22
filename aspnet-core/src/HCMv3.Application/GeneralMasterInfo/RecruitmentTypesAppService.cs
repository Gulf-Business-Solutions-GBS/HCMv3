using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;

namespace HCMv3.GeneralMasterInfo;

[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_RecruitmentTypes)]
public class RecruitmentTypesAppService : SimpleLookupAppServiceBase<RecruitmentType>, IRecruitmentTypesAppService
{
    public RecruitmentTypesAppService(IRepository<RecruitmentType> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_GeneralMasterInfo_RecruitmentTypes_Create;
    protected override string EditPermissionName => AppPermissions.Pages_GeneralMasterInfo_RecruitmentTypes_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_GeneralMasterInfo_RecruitmentTypes_Delete;
}
