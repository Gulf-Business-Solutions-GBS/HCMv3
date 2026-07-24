using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.AccommodationManagement.Dto;

namespace HCMv3.AccommodationManagement;

public interface IBuildingsAppService : IApplicationService
{
    Task<PagedResultDto<BuildingDto>> GetAll(GetAllBuildingsInput input);

    Task<GetBuildingForEditOutput> GetBuildingForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditBuildingDto input);

    Task Delete(EntityDto input);
}
