using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.TimeManagement;

[AbpAuthorize(AppPermissions.Pages_TimeManagement_TimeTypes)]
public class TimeTypesAppService : SimpleLookupAppServiceBase<TimeType>, ITimeTypesAppService
{
    public TimeTypesAppService(IRepository<TimeType> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_TimeManagement_TimeTypes_Create;
    protected override string EditPermissionName => AppPermissions.Pages_TimeManagement_TimeTypes_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_TimeManagement_TimeTypes_Delete;
}
