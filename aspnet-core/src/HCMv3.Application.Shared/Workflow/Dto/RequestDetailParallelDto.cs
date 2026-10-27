using Abp.Application.Services.Dto;

namespace HCMv3.Workflow.Dto;

public class RequestDetailParallelDto : EntityDto<int>
{
    public int RequestDetailId { get; set; }

    public int? JobTitleId { get; set; }

    public string JobTitleNameEn { get; set; }

    public int? WorkflowRoleId { get; set; }

    public string WorkflowRoleNameEn { get; set; }
}
