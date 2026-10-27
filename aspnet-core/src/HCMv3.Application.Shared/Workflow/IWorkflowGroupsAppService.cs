using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Workflow.Dto;

namespace HCMv3.Workflow;

public interface IWorkflowGroupsAppService : IApplicationService
{
    Task<PagedResultDto<WorkflowGroupDto>> GetAll(GetAllWorkflowGroupsInput input);

    Task<GetWorkflowGroupForEditOutput> GetWorkflowGroupForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditWorkflowGroupDto input);

    Task Delete(EntityDto input);
}
