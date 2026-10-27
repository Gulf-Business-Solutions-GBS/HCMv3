using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.ProjectManagement;

[AbpAuthorize(AppPermissions.Pages_ProjectManagement_ProjectTypes)]
public class ProjectTypesAppService : SimpleLookupAppServiceBase<ProjectType>, IProjectTypesAppService
{
    public ProjectTypesAppService(IRepository<ProjectType> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_ProjectManagement_ProjectTypes_Create;
    protected override string EditPermissionName => AppPermissions.Pages_ProjectManagement_ProjectTypes_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_ProjectManagement_ProjectTypes_Delete;
}
