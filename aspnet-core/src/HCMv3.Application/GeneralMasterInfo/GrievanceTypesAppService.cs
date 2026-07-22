using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;

namespace HCMv3.GeneralMasterInfo;

[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_GrievanceTypes)]
public class GrievanceTypesAppService : SimpleLookupAppServiceBase<GrievanceType>, IGrievanceTypesAppService
{
    public GrievanceTypesAppService(IRepository<GrievanceType> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_GeneralMasterInfo_GrievanceTypes_Create;
    protected override string EditPermissionName => AppPermissions.Pages_GeneralMasterInfo_GrievanceTypes_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_GeneralMasterInfo_GrievanceTypes_Delete;
}
