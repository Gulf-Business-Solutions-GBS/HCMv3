using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo.Dto;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// Asset types. No longer a simple lookup: legacy <c>AssetType</c> also carries
/// <c>IncludeInWorkflow</c> and a <c>WageType</c>.
/// </summary>
public interface IAssetTypesAppService : IApplicationService
{
    Task<PagedResultDto<AssetTypeDto>> GetAll(GetAllAssetTypesInput input);

    Task<GetAssetTypeForEditOutput> GetAssetTypeForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditAssetTypeDto input);

    Task Delete(EntityDto input);
}
