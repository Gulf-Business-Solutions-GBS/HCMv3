using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.TimeManagement;

[AbpAuthorize(AppPermissions.Pages_TimeManagement_TransTypes)]
public class TransTypesAppService : SimpleLookupAppServiceBase<TransType>, ITransTypesAppService
{
    public TransTypesAppService(IRepository<TransType> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_TimeManagement_TransTypes_Create;
    protected override string EditPermissionName => AppPermissions.Pages_TimeManagement_TransTypes_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_TimeManagement_TransTypes_Delete;
}
