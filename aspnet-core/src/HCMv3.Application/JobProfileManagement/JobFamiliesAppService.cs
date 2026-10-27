using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.JobProfileManagement;

[AbpAuthorize(AppPermissions.Pages_JobProfileManagement_JobFamilies)]
public class JobFamiliesAppService : SimpleLookupAppServiceBase<JobFamily>, IJobFamiliesAppService
{
    public JobFamiliesAppService(IRepository<JobFamily> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_JobProfileManagement_JobFamilies_Create;
    protected override string EditPermissionName => AppPermissions.Pages_JobProfileManagement_JobFamilies_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_JobProfileManagement_JobFamilies_Delete;
}
