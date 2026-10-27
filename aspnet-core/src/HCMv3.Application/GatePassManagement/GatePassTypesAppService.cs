using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.GatePassManagement;

[AbpAuthorize(AppPermissions.Pages_GatePassManagement_GatePassTypes)]
public class GatePassTypesAppService : SimpleLookupAppServiceBase<GatePassType>, IGatePassTypesAppService
{
    public GatePassTypesAppService(IRepository<GatePassType> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_GatePassManagement_GatePassTypes_Create;
    protected override string EditPermissionName => AppPermissions.Pages_GatePassManagement_GatePassTypes_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_GatePassManagement_GatePassTypes_Delete;
}
