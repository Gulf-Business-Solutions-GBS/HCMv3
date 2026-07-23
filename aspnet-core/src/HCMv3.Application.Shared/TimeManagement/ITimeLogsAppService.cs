using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.TimeManagement.Dto;

namespace HCMv3.TimeManagement;

public interface ITimeLogsAppService : IApplicationService
{
    Task<PagedResultDto<TimeLogDto>> GetAll(GetAllTimeLogsInput input);

    Task<GetTimeLogForEditOutput> GetTimeLogForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditTimeLogDto input);

    Task Delete(EntityDto input);
}
