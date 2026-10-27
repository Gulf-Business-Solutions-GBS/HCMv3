using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.TimeManagement.Dto;

namespace HCMv3.TimeManagement;

public interface IRosterDutyPeriodsAppService : IApplicationService
{
    Task<PagedResultDto<RosterDutyPeriodDto>> GetAll(GetAllRosterDutyPeriodsInput input);

    Task<GetRosterDutyPeriodForEditOutput> GetRosterDutyPeriodForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditRosterDutyPeriodDto input);

    Task Delete(EntityDto input);
}
