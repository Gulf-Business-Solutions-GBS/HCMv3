using System.ComponentModel.DataAnnotations;

namespace HCMv3.Workflow.Dto;

public class CreateOrEditWorkFlowApproverDto
{
    public int? Id { get; set; }

    [Required]
    public int WorkflowRequestId { get; set; }

    public int? ApproverId { get; set; }

    public bool IsApproved { get; set; }
}
