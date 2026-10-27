using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.OrganizationManagement;

[AbpAuthorize(AppPermissions.Pages_OrganizationManagement_JobTitles)]
public class JobTitlesAppService : SimpleLookupAppServiceBase<JobTitle>, IJobTitlesAppService
{
    public JobTitlesAppService(IRepository<JobTitle> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_OrganizationManagement_JobTitles_Create;
    protected override string EditPermissionName => AppPermissions.Pages_OrganizationManagement_JobTitles_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_OrganizationManagement_JobTitles_Delete;
}
