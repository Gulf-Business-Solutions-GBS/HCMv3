using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;

namespace HCMv3.GeneralMasterInfo;

[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_VisaTypes)]
public class VisaTypesAppService : SimpleLookupAppServiceBase<VisaType>, IVisaTypesAppService
{
    public VisaTypesAppService(IRepository<VisaType> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_GeneralMasterInfo_VisaTypes_Create;
    protected override string EditPermissionName => AppPermissions.Pages_GeneralMasterInfo_VisaTypes_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_GeneralMasterInfo_VisaTypes_Delete;
}
