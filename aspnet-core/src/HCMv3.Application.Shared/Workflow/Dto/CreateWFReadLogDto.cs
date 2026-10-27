using System.ComponentModel.DataAnnotations;

namespace HCMv3.Workflow.Dto;

public class CreateWFReadLogDto
{
    [Required]
    public int WorkflowRequestId { get; set; }

    [StringLength(256)]
    public string User { get; set; }
}
