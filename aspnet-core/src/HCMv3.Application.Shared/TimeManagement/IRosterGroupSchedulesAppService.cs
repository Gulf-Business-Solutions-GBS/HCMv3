using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.TimeManagement.Dto;

namespace HCMv3.TimeManagement;

public interface IRosterGroupSchedulesAppService : IApplicationService
{
    Task<PagedResultDto<RosterGroupScheduleDto>> GetAll(GetAllRosterGroupSchedulesInput input);

    Task<GetRosterGroupScheduleForEditOutput> GetRosterGroupScheduleForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditRosterGroupScheduleDto input);

    Task Delete(EntityDto input);
}
