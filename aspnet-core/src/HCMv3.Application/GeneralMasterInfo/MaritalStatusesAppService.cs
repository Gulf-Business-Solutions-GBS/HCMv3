using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;

namespace HCMv3.GeneralMasterInfo;

[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_MaritalStatuses)]
public class MaritalStatusesAppService : SimpleLookupAppServiceBase<MaritalStatus>, IMaritalStatusesAppService
{
    public MaritalStatusesAppService(IRepository<MaritalStatus> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_GeneralMasterInfo_MaritalStatuses_Create;
    protected override string EditPermissionName => AppPermissions.Pages_GeneralMasterInfo_MaritalStatuses_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_GeneralMasterInfo_MaritalStatuses_Delete;
}
