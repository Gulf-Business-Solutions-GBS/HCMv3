using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;

namespace HCMv3.GeneralMasterInfo;

[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_Qualifications)]
public class QualificationsAppService : SimpleLookupAppServiceBase<Qualification>, IQualificationsAppService
{
    public QualificationsAppService(IRepository<Qualification> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_GeneralMasterInfo_Qualifications_Create;
    protected override string EditPermissionName => AppPermissions.Pages_GeneralMasterInfo_Qualifications_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_GeneralMasterInfo_Qualifications_Delete;
}
