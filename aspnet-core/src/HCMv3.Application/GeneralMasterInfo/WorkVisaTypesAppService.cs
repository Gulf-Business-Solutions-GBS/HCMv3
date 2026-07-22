using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;

namespace HCMv3.GeneralMasterInfo;

[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_WorkVisaTypes)]
public class WorkVisaTypesAppService : SimpleLookupAppServiceBase<WorkVisaType>, IWorkVisaTypesAppService
{
    public WorkVisaTypesAppService(IRepository<WorkVisaType> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_GeneralMasterInfo_WorkVisaTypes_Create;
    protected override string EditPermissionName => AppPermissions.Pages_GeneralMasterInfo_WorkVisaTypes_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_GeneralMasterInfo_WorkVisaTypes_Delete;
}
