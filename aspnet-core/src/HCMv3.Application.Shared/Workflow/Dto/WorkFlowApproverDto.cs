using Abp.Application.Services.Dto;

namespace HCMv3.Workflow.Dto;

public class WorkFlowApproverDto : EntityDto<int>
{
    public int WorkflowRequestId { get; set; }

    public int? ApproverId { get; set; }

    public string ApproverNameEn { get; set; }

    public bool IsApproved { get; set; }
}
