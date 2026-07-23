using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.TimeManagement.Dto;

namespace HCMv3.TimeManagement;

public interface IEmployeeDutySchedulesAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeDutyScheduleDto>> GetAll(GetAllEmployeeDutySchedulesInput input);

    Task<GetEmployeeDutyScheduleForEditOutput> GetEmployeeDutyScheduleForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeDutyScheduleDto input);

    Task Delete(EntityDto input);
}
