using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo.Dto;

namespace HCMv3.GeneralMasterInfo;

public interface IAssetsAppService : IApplicationService
{
    Task<PagedResultDto<AssetDto>> GetAll(GetAllAssetsInput input);

    Task<GetAssetForEditOutput> GetAssetForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditAssetDto input);

    Task Delete(EntityDto input);
}
