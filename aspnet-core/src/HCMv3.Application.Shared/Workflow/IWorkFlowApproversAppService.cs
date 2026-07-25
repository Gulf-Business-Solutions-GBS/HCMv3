using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Workflow.Dto;

namespace HCMv3.Workflow;

public interface IWorkFlowApproversAppService : IApplicationService
{
    Task<PagedResultDto<WorkFlowApproverDto>> GetAll(GetAllWorkFlowApproversInput input);

    Task<GetWorkFlowApproverForEditOutput> GetWorkFlowApproverForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditWorkFlowApproverDto input);

    Task Delete(EntityDto input);
}
