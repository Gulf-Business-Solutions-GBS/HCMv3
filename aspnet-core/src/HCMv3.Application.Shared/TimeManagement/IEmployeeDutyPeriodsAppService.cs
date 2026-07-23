using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.TimeManagement.Dto;

namespace HCMv3.TimeManagement;

public interface IEmployeeDutyPeriodsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeDutyPeriodDto>> GetAll(GetAllEmployeeDutyPeriodsInput input);

    Task<GetEmployeeDutyPeriodForEditOutput> GetEmployeeDutyPeriodForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeDutyPeriodDto input);

    Task Delete(EntityDto input);
}
