using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.JobProfileManagement;

[AbpAuthorize(AppPermissions.Pages_JobProfileManagement_EducationDegrees)]
public class EducationDegreesAppService : SimpleLookupAppServiceBase<EducationDegree>, IEducationDegreesAppService
{
    public EducationDegreesAppService(IRepository<EducationDegree> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_JobProfileManagement_EducationDegrees_Create;
    protected override string EditPermissionName => AppPermissions.Pages_JobProfileManagement_EducationDegrees_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_JobProfileManagement_EducationDegrees_Delete;
}
