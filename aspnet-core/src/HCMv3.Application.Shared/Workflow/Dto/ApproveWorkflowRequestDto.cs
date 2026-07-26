using System.ComponentModel.DataAnnotations;

namespace HCMv3.Workflow.Dto;

/// <summary>
/// Input for <c>IWorkflowRequestsAppService.ApproveWorkflowRequest</c> (ported from legacy XAF
/// <c>RequestProcessApproval.Approve_Execute</c>).
/// </summary>
public class ApproveWorkflowRequestDto
{
    [Required]
    public int RequestId { get; set; }

    [StringLength(4000)]
    public string Remark { get; set; }
}
