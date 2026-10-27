using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.GatePassManagement;

[AbpAuthorize(AppPermissions.Pages_GatePassManagement_VehicleBrands)]
public class VehicleBrandsAppService : SimpleLookupAppServiceBase<VehicleBrand>, IVehicleBrandsAppService
{
    public VehicleBrandsAppService(IRepository<VehicleBrand> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_GatePassManagement_VehicleBrands_Create;
    protected override string EditPermissionName => AppPermissions.Pages_GatePassManagement_VehicleBrands_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_GatePassManagement_VehicleBrands_Delete;
}
