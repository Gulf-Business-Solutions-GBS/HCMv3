using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.TimeManagement.Dto;

namespace HCMv3.TimeManagement;

public interface IWorkLocationDutyPeriodsAppService : IApplicationService
{
    Task<PagedResultDto<WorkLocationDutyPeriodDto>> GetAll(GetAllWorkLocationDutyPeriodsInput input);

    Task<GetWorkLocationDutyPeriodForEditOutput> GetWorkLocationDutyPeriodForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditWorkLocationDutyPeriodDto input);

    Task Delete(EntityDto input);
}
