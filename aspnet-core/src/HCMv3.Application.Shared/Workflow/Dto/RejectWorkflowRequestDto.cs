using System.ComponentModel.DataAnnotations;

namespace HCMv3.Workflow.Dto;

/// <summary>
/// Input for <c>IWorkflowRequestsAppService.RejectWorkflowRequest</c> (ported from legacy XAF
/// <c>RequestProcessApproval.Reject_Execute</c>).
/// </summary>
public class RejectWorkflowRequestDto
{
    [Required]
    public int RequestId { get; set; }

    [StringLength(4000)]
    public string Remark { get; set; }
}
