using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.GatePassManagement;

[AbpAuthorize(AppPermissions.Pages_GatePassManagement_VehicleModels)]
public class VehicleModelsAppService : SimpleLookupAppServiceBase<VehicleModel>, IVehicleModelsAppService
{
    public VehicleModelsAppService(IRepository<VehicleModel> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_GatePassManagement_VehicleModels_Create;
    protected override string EditPermissionName => AppPermissions.Pages_GatePassManagement_VehicleModels_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_GatePassManagement_VehicleModels_Delete;
}
