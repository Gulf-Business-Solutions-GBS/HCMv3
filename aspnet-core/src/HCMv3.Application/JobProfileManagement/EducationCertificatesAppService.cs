using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.JobProfileManagement;

[AbpAuthorize(AppPermissions.Pages_JobProfileManagement_EducationCertificates)]
public class EducationCertificatesAppService : SimpleLookupAppServiceBase<EducationCertificate>, IEducationCertificatesAppService
{
    public EducationCertificatesAppService(IRepository<EducationCertificate> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_JobProfileManagement_EducationCertificates_Create;
    protected override string EditPermissionName => AppPermissions.Pages_JobProfileManagement_EducationCertificates_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_JobProfileManagement_EducationCertificates_Delete;
}
