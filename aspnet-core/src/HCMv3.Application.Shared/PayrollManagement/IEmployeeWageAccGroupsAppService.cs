using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface IEmployeeWageAccGroupsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeWageAccGroupDto>> GetAll(GetAllEmployeeWageAccGroupsInput input);

    Task<GetEmployeeWageAccGroupForEditOutput> GetEmployeeWageAccGroupForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeWageAccGroupDto input);

    Task Delete(EntityDto input);
}
