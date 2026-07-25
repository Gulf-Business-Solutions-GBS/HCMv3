using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.BenefitsManagement;

[AbpAuthorize(AppPermissions.Pages_BenefitsManagement_InsurancePlans)]
public class InsurancePlansAppService : SimpleLookupAppServiceBase<InsurancePlan>, IInsurancePlansAppService
{
    public InsurancePlansAppService(IRepository<InsurancePlan> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_BenefitsManagement_InsurancePlans_Create;
    protected override string EditPermissionName => AppPermissions.Pages_BenefitsManagement_InsurancePlans_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_BenefitsManagement_InsurancePlans_Delete;
}
