using Abp.Application.Services.Dto;

namespace HCMv3.Workflow.Dto;

public class WorkflowGroupDetailDto : EntityDto<int>
{
    public int WorkflowGroupId { get; set; }

    public int RequestMasterId { get; set; }

    public string RequestMasterNameEn { get; set; }

    public string RequestTypeName { get; set; }

    public int? RequestSubTypeId { get; set; }

    public int? CustomSubTypeId { get; set; }
}
