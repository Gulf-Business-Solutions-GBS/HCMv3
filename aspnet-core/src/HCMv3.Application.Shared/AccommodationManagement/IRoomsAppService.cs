using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.AccommodationManagement.Dto;

namespace HCMv3.AccommodationManagement;

public interface IRoomsAppService : IApplicationService
{
    Task<PagedResultDto<RoomDto>> GetAll(GetAllRoomsInput input);

    Task<GetRoomForEditOutput> GetRoomForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditRoomDto input);

    Task Delete(EntityDto input);
}
