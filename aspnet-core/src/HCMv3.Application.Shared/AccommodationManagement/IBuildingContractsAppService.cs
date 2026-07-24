using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.AccommodationManagement.Dto;

namespace HCMv3.AccommodationManagement;

public interface IBuildingContractsAppService : IApplicationService
{
    Task<PagedResultDto<BuildingContractDto>> GetAll(GetAllBuildingContractsInput input);

    Task<GetBuildingContractForEditOutput> GetBuildingContractForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditBuildingContractDto input);

    Task Delete(EntityDto input);
}
