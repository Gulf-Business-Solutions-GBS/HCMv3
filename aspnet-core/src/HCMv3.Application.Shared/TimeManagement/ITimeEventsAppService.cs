using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.TimeManagement.Dto;

namespace HCMv3.TimeManagement;

public interface ITimeEventsAppService : IApplicationService
{
    Task<PagedResultDto<TimeEventDto>> GetAll(GetAllTimeEventsInput input);

    Task<GetTimeEventForEditOutput> GetTimeEventForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditTimeEventDto input);

    Task Delete(EntityDto input);
}
