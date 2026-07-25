using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.SuccessionManagement;

[AbpAuthorize(AppPermissions.Pages_SuccessionManagement_ImpactOfLosses)]
public class ImpactOfLossesAppService : SimpleLookupAppServiceBase<ImpactOfLoss>, IImpactOfLossesAppService
{
    public ImpactOfLossesAppService(IRepository<ImpactOfLoss> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_SuccessionManagement_ImpactOfLosses_Create;
    protected override string EditPermissionName => AppPermissions.Pages_SuccessionManagement_ImpactOfLosses_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_SuccessionManagement_ImpactOfLosses_Delete;
}
