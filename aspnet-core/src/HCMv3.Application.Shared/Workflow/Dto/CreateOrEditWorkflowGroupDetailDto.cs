using System.ComponentModel.DataAnnotations;

namespace HCMv3.Workflow.Dto;

public class CreateOrEditWorkflowGroupDetailDto
{
    public int? Id { get; set; }

    [Required]
    public int WorkflowGroupId { get; set; }

    [Required]
    public int RequestMasterId { get; set; }

    [Required]
    [StringLength(256)]
    public string RequestTypeName { get; set; }

    public int? RequestSubTypeId { get; set; }

    public int? CustomSubTypeId { get; set; }

    /// <summary>The action this line applies to (amendment, cancellation, return); empty means every action.</summary>
    public WorkflowRequestAction? RequestAction { get; set; }
}
