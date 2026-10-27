using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;

namespace HCMv3.GeneralMasterInfo;

[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_TaskTypes)]
public class TaskTypesAppService : SimpleLookupAppServiceBase<TaskType>, ITaskTypesAppService
{
    public TaskTypesAppService(IRepository<TaskType> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_GeneralMasterInfo_TaskTypes_Create;
    protected override string EditPermissionName => AppPermissions.Pages_GeneralMasterInfo_TaskTypes_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_GeneralMasterInfo_TaskTypes_Delete;
}
