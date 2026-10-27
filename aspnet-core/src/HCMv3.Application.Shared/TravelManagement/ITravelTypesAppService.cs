using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.TravelManagement.Dto;

namespace HCMv3.TravelManagement;

public interface ITravelTypesAppService : IApplicationService
{
    Task<PagedResultDto<TravelTypeDto>> GetAll(GetAllTravelTypesInput input);

    Task<GetTravelTypeForEditOutput> GetTravelTypeForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditTravelTypeDto input);

    Task Delete(EntityDto input);
}
