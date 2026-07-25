using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.BenefitsManagement;

[AbpAuthorize(AppPermissions.Pages_BenefitsManagement_InsuranceProviders)]
public class InsuranceProvidersAppService : SimpleLookupAppServiceBase<InsuranceProvider>, IInsuranceProvidersAppService
{
    public InsuranceProvidersAppService(IRepository<InsuranceProvider> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_BenefitsManagement_InsuranceProviders_Create;
    protected override string EditPermissionName => AppPermissions.Pages_BenefitsManagement_InsuranceProviders_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_BenefitsManagement_InsuranceProviders_Delete;
}
