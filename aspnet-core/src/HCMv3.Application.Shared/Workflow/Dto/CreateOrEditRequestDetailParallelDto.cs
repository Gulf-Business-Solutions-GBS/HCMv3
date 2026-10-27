using System.ComponentModel.DataAnnotations;

namespace HCMv3.Workflow.Dto;

public class CreateOrEditRequestDetailParallelDto
{
    public int? Id { get; set; }

    [Required]
    public int RequestDetailId { get; set; }

    public int? JobTitleId { get; set; }

    [Required]
    public int? WorkflowRoleId { get; set; }
}
