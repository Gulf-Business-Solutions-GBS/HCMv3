using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.TimeManagement.Dto;

namespace HCMv3.TimeManagement;

public interface IRosterDailyDutiesAppService : IApplicationService
{
    Task<PagedResultDto<RosterDailyDutyDto>> GetAll(GetAllRosterDailyDutiesInput input);

    Task<GetRosterDailyDutyForEditOutput> GetRosterDailyDutyForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditRosterDailyDutyDto input);

    Task Delete(EntityDto input);
}
