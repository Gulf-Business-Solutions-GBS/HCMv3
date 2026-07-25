using System.ComponentModel.DataAnnotations;

namespace HCMv3.Workflow.Dto;

public class CreateOrEditWorkflowRequestDto
{
    public int? Id { get; set; }

    public int? EmployeeId { get; set; }

    public int WorkflowStep { get; set; }

    public int? ApproverId { get; set; }

    [StringLength(256)]
    public string RequestType { get; set; }

    [StringLength(64)]
    public string RequestNumber { get; set; }

    public RequestStatus RequestStatus { get; set; }

    public int ApprovalProcessTime { get; set; }
}
