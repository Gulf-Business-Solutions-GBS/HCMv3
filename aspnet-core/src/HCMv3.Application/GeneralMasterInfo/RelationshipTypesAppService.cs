using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;

namespace HCMv3.GeneralMasterInfo;

[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_RelationshipTypes)]
public class RelationshipTypesAppService : SimpleLookupAppServiceBase<RelationshipType>, IRelationshipTypesAppService
{
    public RelationshipTypesAppService(IRepository<RelationshipType> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_GeneralMasterInfo_RelationshipTypes_Create;
    protected override string EditPermissionName => AppPermissions.Pages_GeneralMasterInfo_RelationshipTypes_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_GeneralMasterInfo_RelationshipTypes_Delete;
}
