using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;

namespace HCMv3.GeneralMasterInfo;

[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_DocumentPlaces)]
public class DocumentPlacesAppService : SimpleLookupAppServiceBase<DocumentPlace>, IDocumentPlacesAppService
{
    public DocumentPlacesAppService(IRepository<DocumentPlace> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_GeneralMasterInfo_DocumentPlaces_Create;
    protected override string EditPermissionName => AppPermissions.Pages_GeneralMasterInfo_DocumentPlaces_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_GeneralMasterInfo_DocumentPlaces_Delete;
}
