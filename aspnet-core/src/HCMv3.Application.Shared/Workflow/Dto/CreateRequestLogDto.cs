using System.ComponentModel.DataAnnotations;

namespace HCMv3.Workflow.Dto;

/// <summary>
/// Create-only input for <see cref="RequestLogDto"/>: legacy UI made rows non-editable/non-deletable-by-user
/// after creation, so no Edit DTO is provided (see <c>RequestLog</c> entity remarks).
/// </summary>
public class CreateRequestLogDto
{
    [Required]
    public int WorkflowRequestId { get; set; }

    public int? ActionById { get; set; }

    [StringLength(4000)]
    public string RequestActionComment { get; set; }

    [Required]
    public RequestActionType ActionType { get; set; }

    public int WorkflowStep { get; set; }

    public int ActionDays { get; set; }

    public WorkflowPlatform Platform { get; set; }
}
