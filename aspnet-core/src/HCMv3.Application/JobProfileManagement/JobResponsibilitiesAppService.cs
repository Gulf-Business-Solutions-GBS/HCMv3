using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.JobProfileManagement;

[AbpAuthorize(AppPermissions.Pages_JobProfileManagement_JobResponsibilities)]
public class JobResponsibilitiesAppService : SimpleLookupAppServiceBase<JobResponsibility>, IJobResponsibilitiesAppService
{
    public JobResponsibilitiesAppService(IRepository<JobResponsibility> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_JobProfileManagement_JobResponsibilities_Create;
    protected override string EditPermissionName => AppPermissions.Pages_JobProfileManagement_JobResponsibilities_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_JobProfileManagement_JobResponsibilities_Delete;
}
