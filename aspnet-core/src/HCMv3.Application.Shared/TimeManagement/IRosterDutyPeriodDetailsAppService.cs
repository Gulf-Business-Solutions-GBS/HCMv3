using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.TimeManagement.Dto;

namespace HCMv3.TimeManagement;

public interface IRosterDutyPeriodDetailsAppService : IApplicationService
{
    Task<PagedResultDto<RosterDutyPeriodDetailDto>> GetAll(GetAllRosterDutyPeriodDetailsInput input);

    Task<GetRosterDutyPeriodDetailForEditOutput> GetRosterDutyPeriodDetailForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditRosterDutyPeriodDetailDto input);

    Task Delete(EntityDto input);
}
