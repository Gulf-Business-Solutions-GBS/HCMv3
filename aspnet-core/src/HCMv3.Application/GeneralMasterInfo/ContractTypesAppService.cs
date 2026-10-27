using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;

namespace HCMv3.GeneralMasterInfo;

[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_ContractTypes)]
public class ContractTypesAppService : SimpleLookupAppServiceBase<ContractType>, IContractTypesAppService
{
    public ContractTypesAppService(IRepository<ContractType> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_GeneralMasterInfo_ContractTypes_Create;
    protected override string EditPermissionName => AppPermissions.Pages_GeneralMasterInfo_ContractTypes_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_GeneralMasterInfo_ContractTypes_Delete;
}
