using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.TimeManagement.Dto;

namespace HCMv3.TimeManagement;

public interface IRosterDutySchedulesAppService : IApplicationService
{
    Task<PagedResultDto<RosterDutyScheduleDto>> GetAll(GetAllRosterDutySchedulesInput input);

    Task<GetRosterDutyScheduleForEditOutput> GetRosterDutyScheduleForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditRosterDutyScheduleDto input);

    Task Delete(EntityDto input);
}
