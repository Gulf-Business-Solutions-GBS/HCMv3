using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.SystemSecurity.Dto;

namespace HCMv3.SystemSecurity;

public interface ITaskEventsAppService : IApplicationService
{
    Task<PagedResultDto<TaskEventDto>> GetAll(GetAllTaskEventsInput input);

    Task<GetTaskEventForEditOutput> GetTaskEventForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditTaskEventDto input);

    Task Delete(EntityDto input);
}
