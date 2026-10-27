using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;

namespace HCMv3.GeneralMasterInfo;

[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_BankFiles)]
public class BankFilesAppService : SimpleLookupAppServiceBase<BankFile>, IBankFilesAppService
{
    public BankFilesAppService(IRepository<BankFile> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_GeneralMasterInfo_BankFiles_Create;
    protected override string EditPermissionName => AppPermissions.Pages_GeneralMasterInfo_BankFiles_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_GeneralMasterInfo_BankFiles_Delete;
}
