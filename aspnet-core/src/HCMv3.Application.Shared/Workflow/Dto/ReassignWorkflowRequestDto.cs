using System.ComponentModel.DataAnnotations;

namespace HCMv3.Workflow.Dto;

/// <summary>
/// Input for <c>IWorkflowRequestsAppService.ReassignWorkflowRequest</c> (ported from legacy XAF
/// <c>RequestProcessApproval.recAssign_Execute</c> / <c>RequestActionService.AssignNewApprover</c>).
/// </summary>
public class ReassignWorkflowRequestDto
{
    [Required]
    public int RequestId { get; set; }

    [Required]
    public int NewApproverEmployeeId { get; set; }

    [StringLength(4000)]
    public string Remark { get; set; }
}
