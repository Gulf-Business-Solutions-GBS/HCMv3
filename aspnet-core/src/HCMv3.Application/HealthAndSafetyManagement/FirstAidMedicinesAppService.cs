using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.HealthAndSafetyManagement;

[AbpAuthorize(AppPermissions.Pages_HealthAndSafetyManagement_FirstAidMedicines)]
public class FirstAidMedicinesAppService : SimpleLookupAppServiceBase<FirstAidMedicine>, IFirstAidMedicinesAppService
{
    public FirstAidMedicinesAppService(IRepository<FirstAidMedicine> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_HealthAndSafetyManagement_FirstAidMedicines_Create;
    protected override string EditPermissionName => AppPermissions.Pages_HealthAndSafetyManagement_FirstAidMedicines_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_HealthAndSafetyManagement_FirstAidMedicines_Delete;
}
