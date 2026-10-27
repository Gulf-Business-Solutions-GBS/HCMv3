using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;

namespace HCMv3.GeneralMasterInfo;

[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_TrainingTypes)]
public class TrainingTypesAppService : SimpleLookupAppServiceBase<TrainingType>, ITrainingTypesAppService
{
    public TrainingTypesAppService(IRepository<TrainingType> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_GeneralMasterInfo_TrainingTypes_Create;
    protected override string EditPermissionName => AppPermissions.Pages_GeneralMasterInfo_TrainingTypes_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_GeneralMasterInfo_TrainingTypes_Delete;
}
