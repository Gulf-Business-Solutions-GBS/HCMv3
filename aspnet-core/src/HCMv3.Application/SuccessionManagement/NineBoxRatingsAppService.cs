using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.SuccessionManagement;

[AbpAuthorize(AppPermissions.Pages_SuccessionManagement_NineBoxRatings)]
public class NineBoxRatingsAppService : SimpleLookupAppServiceBase<NineBoxRating>, INineBoxRatingsAppService
{
    public NineBoxRatingsAppService(IRepository<NineBoxRating> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_SuccessionManagement_NineBoxRatings_Create;
    protected override string EditPermissionName => AppPermissions.Pages_SuccessionManagement_NineBoxRatings_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_SuccessionManagement_NineBoxRatings_Delete;
}
