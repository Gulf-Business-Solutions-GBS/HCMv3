using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface IEmployeeSubGroupsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeSubGroupDto>> GetAll(GetAllEmployeeSubGroupsInput input);

    Task<GetEmployeeSubGroupForEditOutput> GetEmployeeSubGroupForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeSubGroupDto input);

    Task Delete(EntityDto input);
}
