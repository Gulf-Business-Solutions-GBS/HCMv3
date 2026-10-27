using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Workflow.Dto;

namespace HCMv3.Workflow;

public interface IEmployeeWorkflowRolesAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeWorkflowRoleDto>> GetAll(GetAllEmployeeWorkflowRolesInput input);

    Task<GetEmployeeWorkflowRoleForEditOutput> GetEmployeeWorkflowRoleForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeWorkflowRoleDto input);

    Task Delete(EntityDto input);
}
