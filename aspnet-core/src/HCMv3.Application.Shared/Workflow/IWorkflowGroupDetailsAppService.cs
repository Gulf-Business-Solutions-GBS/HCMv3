using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Workflow.Dto;

namespace HCMv3.Workflow;

public interface IWorkflowGroupDetailsAppService : IApplicationService
{
    Task<PagedResultDto<WorkflowGroupDetailDto>> GetAll(GetAllWorkflowGroupDetailsInput input);

    Task<GetWorkflowGroupDetailForEditOutput> GetWorkflowGroupDetailForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditWorkflowGroupDetailDto input);

    Task Delete(EntityDto input);
}
