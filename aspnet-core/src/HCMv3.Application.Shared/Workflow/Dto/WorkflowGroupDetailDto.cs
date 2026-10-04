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

    /// <summary>The action this line applies to (amendment, cancellation, return); empty means every action.</summary>
    public WorkflowRequestAction? RequestAction { get; set; }
}
