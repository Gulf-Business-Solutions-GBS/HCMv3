using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.AccommodationManagement.Dto;

namespace HCMv3.AccommodationManagement;

public interface IFloorsAppService : IApplicationService
{
    Task<PagedResultDto<FloorDto>> GetAll(GetAllFloorsInput input);

    Task<GetFloorForEditOutput> GetFloorForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditFloorDto input);

    Task Delete(EntityDto input);
}
