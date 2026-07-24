using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.TravelManagement.Dto;

namespace HCMv3.TravelManagement;

public interface ITravelDetailCostsAppService : IApplicationService
{
    Task<PagedResultDto<TravelDetailCostDto>> GetAll(GetAllTravelDetailCostsInput input);

    Task<GetTravelDetailCostForEditOutput> GetTravelDetailCostForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditTravelDetailCostDto input);

    Task Delete(EntityDto input);
}
