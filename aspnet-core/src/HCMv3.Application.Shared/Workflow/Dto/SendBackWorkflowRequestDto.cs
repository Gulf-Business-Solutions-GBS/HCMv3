using System.ComponentModel.DataAnnotations;

namespace HCMv3.Workflow.Dto;

/// <summary>
/// Input for <c>IWorkflowRequestsAppService.SendBackWorkflowRequest</c> (ported from legacy XAF
/// <c>RequestProcessApproval.SendBack_Execute</c>).
/// </summary>
public class SendBackWorkflowRequestDto
{
    [Required]
    public int RequestId { get; set; }

    [StringLength(4000)]
    public string Remark { get; set; }
}
