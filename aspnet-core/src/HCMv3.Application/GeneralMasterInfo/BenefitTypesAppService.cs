using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;

namespace HCMv3.GeneralMasterInfo;

[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_BenefitTypes)]
public class BenefitTypesAppService : SimpleLookupAppServiceBase<BenefitType>, IBenefitTypesAppService
{
    public BenefitTypesAppService(IRepository<BenefitType> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_GeneralMasterInfo_BenefitTypes_Create;
    protected override string EditPermissionName => AppPermissions.Pages_GeneralMasterInfo_BenefitTypes_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_GeneralMasterInfo_BenefitTypes_Delete;
}
