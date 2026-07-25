using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.SuccessionManagement;

[AbpAuthorize(AppPermissions.Pages_SuccessionManagement_Readinesses)]
public class ReadinessesAppService : SimpleLookupAppServiceBase<Readiness>, IReadinessesAppService
{
    public ReadinessesAppService(IRepository<Readiness> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_SuccessionManagement_Readinesses_Create;
    protected override string EditPermissionName => AppPermissions.Pages_SuccessionManagement_Readinesses_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_SuccessionManagement_Readinesses_Delete;
}
