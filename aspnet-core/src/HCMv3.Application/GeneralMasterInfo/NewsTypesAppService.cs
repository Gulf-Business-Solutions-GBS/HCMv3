using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;

namespace HCMv3.GeneralMasterInfo;

[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_NewsTypes)]
public class NewsTypesAppService : SimpleLookupAppServiceBase<NewsType>, INewsTypesAppService
{
    public NewsTypesAppService(IRepository<NewsType> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_GeneralMasterInfo_NewsTypes_Create;
    protected override string EditPermissionName => AppPermissions.Pages_GeneralMasterInfo_NewsTypes_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_GeneralMasterInfo_NewsTypes_Delete;
}
