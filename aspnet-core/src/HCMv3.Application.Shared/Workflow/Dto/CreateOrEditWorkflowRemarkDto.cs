using System.ComponentModel.DataAnnotations;

namespace HCMv3.Workflow.Dto;

public class CreateOrEditWorkflowRemarkDto
{
    public int? Id { get; set; }

    [Required]
    public int WorkflowRequestId { get; set; }

    [Required]
    public int? CCReceiverId { get; set; }
}
