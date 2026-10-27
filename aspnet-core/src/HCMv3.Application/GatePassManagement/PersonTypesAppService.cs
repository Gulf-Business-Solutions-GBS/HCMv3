using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.GatePassManagement;

[AbpAuthorize(AppPermissions.Pages_GatePassManagement_PersonTypes)]
public class PersonTypesAppService : SimpleLookupAppServiceBase<PersonType>, IPersonTypesAppService
{
    public PersonTypesAppService(IRepository<PersonType> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_GatePassManagement_PersonTypes_Create;
    protected override string EditPermissionName => AppPermissions.Pages_GatePassManagement_PersonTypes_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_GatePassManagement_PersonTypes_Delete;
}
