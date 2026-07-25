using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Workflow.Dto;

namespace HCMv3.Workflow;

public interface IWorkflowRemarksAppService : IApplicationService
{
    Task<PagedResultDto<WorkflowRemarkDto>> GetAll(GetAllWorkflowRemarksInput input);

    Task<GetWorkflowRemarkForEditOutput> GetWorkflowRemarkForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditWorkflowRemarkDto input);

    Task Delete(EntityDto input);
}
