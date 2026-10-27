using Abp.Application.Services.Dto;

namespace HCMv3.Workflow.Dto;

public class WorkflowRemarkDto : EntityDto<int>
{
    public int WorkflowRequestId { get; set; }

    public int? CCReceiverId { get; set; }

    public string CCReceiverNameEn { get; set; }
}
