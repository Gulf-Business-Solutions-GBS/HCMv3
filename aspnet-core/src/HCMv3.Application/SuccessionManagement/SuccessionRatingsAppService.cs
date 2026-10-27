using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.SuccessionManagement;

[AbpAuthorize(AppPermissions.Pages_SuccessionManagement_SuccessionRatings)]
public class SuccessionRatingsAppService : SimpleLookupAppServiceBase<SuccessionRating>, ISuccessionRatingsAppService
{
    public SuccessionRatingsAppService(IRepository<SuccessionRating> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_SuccessionManagement_SuccessionRatings_Create;
    protected override string EditPermissionName => AppPermissions.Pages_SuccessionManagement_SuccessionRatings_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_SuccessionManagement_SuccessionRatings_Delete;
}
