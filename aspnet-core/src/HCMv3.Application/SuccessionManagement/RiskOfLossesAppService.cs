using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.SuccessionManagement;

[AbpAuthorize(AppPermissions.Pages_SuccessionManagement_RiskOfLosses)]
public class RiskOfLossesAppService : SimpleLookupAppServiceBase<RiskOfLoss>, IRiskOfLossesAppService
{
    public RiskOfLossesAppService(IRepository<RiskOfLoss> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_SuccessionManagement_RiskOfLosses_Create;
    protected override string EditPermissionName => AppPermissions.Pages_SuccessionManagement_RiskOfLosses_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_SuccessionManagement_RiskOfLosses_Delete;
}
