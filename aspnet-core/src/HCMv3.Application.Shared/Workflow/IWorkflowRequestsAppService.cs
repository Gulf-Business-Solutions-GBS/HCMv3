using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Workflow.Dto;

namespace HCMv3.Workflow;

public interface IWorkflowRequestsAppService : IApplicationService
{
    Task<PagedResultDto<WorkflowRequestDto>> GetAll(GetAllWorkflowRequestsInput input);

    Task<GetWorkflowRequestForEditOutput> GetWorkflowRequestForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditWorkflowRequestDto input);

    Task Delete(EntityDto input);
}
