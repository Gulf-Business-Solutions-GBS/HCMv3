using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;

namespace HCMv3.GeneralMasterInfo;

[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_DateTypes)]
public class DateTypesAppService : SimpleLookupAppServiceBase<DateType>, IDateTypesAppService
{
    public DateTypesAppService(IRepository<DateType> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_GeneralMasterInfo_DateTypes_Create;
    protected override string EditPermissionName => AppPermissions.Pages_GeneralMasterInfo_DateTypes_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_GeneralMasterInfo_DateTypes_Delete;
}
