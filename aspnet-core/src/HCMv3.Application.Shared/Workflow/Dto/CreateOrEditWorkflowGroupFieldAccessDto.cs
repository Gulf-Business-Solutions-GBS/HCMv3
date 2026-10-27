using System.ComponentModel.DataAnnotations;

namespace HCMv3.Workflow.Dto;

public class CreateOrEditWorkflowGroupFieldAccessDto
{
    public int? Id { get; set; }

    [Required]
    public int WorkflowGroupDetailId { get; set; }

    [Required]
    public int Step { get; set; }

    [Required]
    [StringLength(256)]
    public string Field { get; set; }

    public bool IsVisible { get; set; }

    public bool IsEditable { get; set; }

    public bool IsRequired { get; set; }
}
