using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.SuccessionManagement;

[AbpAuthorize(AppPermissions.Pages_SuccessionManagement_ReasonForLeavings)]
public class ReasonForLeavingsAppService : SimpleLookupAppServiceBase<ReasonForLeaving>, IReasonForLeavingsAppService
{
    public ReasonForLeavingsAppService(IRepository<ReasonForLeaving> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_SuccessionManagement_ReasonForLeavings_Create;
    protected override string EditPermissionName => AppPermissions.Pages_SuccessionManagement_ReasonForLeavings_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_SuccessionManagement_ReasonForLeavings_Delete;
}
