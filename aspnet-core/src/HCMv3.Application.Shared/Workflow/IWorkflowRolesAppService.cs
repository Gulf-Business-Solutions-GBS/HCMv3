using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Workflow.Dto;

namespace HCMv3.Workflow;

public interface IWorkflowRolesAppService : IApplicationService
{
    Task<PagedResultDto<WorkflowRoleDto>> GetAll(GetAllWorkflowRolesInput input);

    Task<GetWorkflowRoleForEditOutput> GetWorkflowRoleForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditWorkflowRoleDto input);

    Task Delete(EntityDto input);
}
