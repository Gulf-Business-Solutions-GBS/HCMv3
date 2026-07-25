using Abp.Application.Services.Dto;

namespace HCMv3.Workflow.Dto;

public class WorkflowGroupDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public bool IsDefault { get; set; }
}
