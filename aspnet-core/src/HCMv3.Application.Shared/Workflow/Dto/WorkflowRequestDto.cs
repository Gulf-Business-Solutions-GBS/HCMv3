using Abp.Application.Services.Dto;

namespace HCMv3.Workflow.Dto;

public class WorkflowRequestDto : EntityDto<int>
{
    public int? EmployeeId { get; set; }

    public string EmployeeNameEn { get; set; }

    public int WorkflowStep { get; set; }

    public int? ApproverId { get; set; }

    public string ApproverNameEn { get; set; }

    public string RequestType { get; set; }

    public string RequestNumber { get; set; }

    public RequestStatus RequestStatus { get; set; }

    public int ApprovalProcessTime { get; set; }
}
