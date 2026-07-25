using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.JobProfileManagement;

[AbpAuthorize(AppPermissions.Pages_JobProfileManagement_Skills)]
public class SkillsAppService : SimpleLookupAppServiceBase<Skill>, ISkillsAppService
{
    public SkillsAppService(IRepository<Skill> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_JobProfileManagement_Skills_Create;
    protected override string EditPermissionName => AppPermissions.Pages_JobProfileManagement_Skills_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_JobProfileManagement_Skills_Delete;
}
