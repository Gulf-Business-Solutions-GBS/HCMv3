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

    /// <summary>
    /// Submits a Draft <see cref="WorkflowRequest"/> into the approval chain. See
    /// <see cref="HCMv3.Workflow.Services.IWorkflowApprovalService.SubmitAsync"/>.
    /// </summary>
    Task SubmitWorkflowRequest(EntityDto input);

    /// <summary>
    /// Approves the current workflow step on behalf of the calling employee. See
    /// <see cref="HCMv3.Workflow.Services.IWorkflowApprovalService.ApproveAsync"/>.
    /// </summary>
    Task ApproveWorkflowRequest(ApproveWorkflowRequestDto input);

    /// <summary>
    /// Rejects the request on behalf of the calling employee. See
    /// <see cref="HCMv3.Workflow.Services.IWorkflowApprovalService.RejectAsync"/>.
    /// </summary>
    Task RejectWorkflowRequest(RejectWorkflowRequestDto input);

    /// <summary>
    /// Sends the request back to the submitter for correction. See
    /// <see cref="HCMv3.Workflow.Services.IWorkflowApprovalService.SendBackAsync"/>.
    /// </summary>
    Task SendBackWorkflowRequest(SendBackWorkflowRequestDto input);

    /// <summary>
    /// Delegates/reassigns the current step's approval to another employee. See
    /// <see cref="HCMv3.Workflow.Services.IWorkflowApprovalService.ReassignAsync"/>.
    /// </summary>
    Task ReassignWorkflowRequest(ReassignWorkflowRequestDto input);
}
