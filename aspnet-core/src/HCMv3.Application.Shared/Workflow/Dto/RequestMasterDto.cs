using Abp.Application.Services.Dto;

namespace HCMv3.Workflow.Dto;

public class RequestMasterDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public int WorkflowSteps { get; set; }

    public WorkflowSecurityLevel SecurityLevel { get; set; }

    public bool AutoClose { get; set; }

    public int AutoCloseInDays { get; set; }

    public int? DefaultApproverId { get; set; }

    public bool IsCommentRequire { get; set; }

    public int DueInDays { get; set; }
}
