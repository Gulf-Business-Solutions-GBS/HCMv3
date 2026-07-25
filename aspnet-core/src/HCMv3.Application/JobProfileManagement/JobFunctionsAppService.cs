using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.JobProfileManagement;

[AbpAuthorize(AppPermissions.Pages_JobProfileManagement_JobFunctions)]
public class JobFunctionsAppService : SimpleLookupAppServiceBase<JobFunction>, IJobFunctionsAppService
{
    public JobFunctionsAppService(IRepository<JobFunction> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_JobProfileManagement_JobFunctions_Create;
    protected override string EditPermissionName => AppPermissions.Pages_JobProfileManagement_JobFunctions_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_JobProfileManagement_JobFunctions_Delete;
}
