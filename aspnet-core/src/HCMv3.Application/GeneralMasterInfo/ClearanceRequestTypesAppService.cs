using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo.Dto;

namespace HCMv3.GeneralMasterInfo;

[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_ClearanceRequestTypes)]
public class ClearanceRequestTypesAppService : SimpleLookupAppServiceBase<ClearanceRequestType>, IClearanceRequestTypesAppService
{
    public ClearanceRequestTypesAppService(IRepository<ClearanceRequestType> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_GeneralMasterInfo_ClearanceRequestTypes_Create;
    protected override string EditPermissionName => AppPermissions.Pages_GeneralMasterInfo_ClearanceRequestTypes_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_GeneralMasterInfo_ClearanceRequestTypes_Delete;

    /// <inheritdoc/>
    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_ClearanceRequestTypes_Edit)]
    public async Task SetIncludeInWorkflow(SetClearanceRequestTypeIncludeInWorkflowInput input)
    {
        var entity = await Repository.GetAsync(input.Id);

        entity.IncludeInWorkflow = input.IncludeInWorkflow;

        await Repository.UpdateAsync(entity);
    }
}
