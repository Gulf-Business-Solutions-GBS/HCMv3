using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Workflow.Dto;

namespace HCMv3.Workflow;

public interface IWorkflowGroupFieldAccessesAppService : IApplicationService
{
    Task<PagedResultDto<WorkflowGroupFieldAccessDto>> GetAll(GetAllWorkflowGroupFieldAccessesInput input);

    Task<GetWorkflowGroupFieldAccessForEditOutput> GetWorkflowGroupFieldAccessForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditWorkflowGroupFieldAccessDto input);

    Task Delete(EntityDto input);
}
