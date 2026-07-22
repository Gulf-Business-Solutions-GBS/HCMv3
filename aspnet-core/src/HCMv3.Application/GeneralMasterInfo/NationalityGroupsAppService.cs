using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;

namespace HCMv3.GeneralMasterInfo;

[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_NationalityGroups)]
public class NationalityGroupsAppService : SimpleLookupAppServiceBase<NationalityGroup>, INationalityGroupsAppService
{
    public NationalityGroupsAppService(IRepository<NationalityGroup> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_GeneralMasterInfo_NationalityGroups_Create;
    protected override string EditPermissionName => AppPermissions.Pages_GeneralMasterInfo_NationalityGroups_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_GeneralMasterInfo_NationalityGroups_Delete;
}
