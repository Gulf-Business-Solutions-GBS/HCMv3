using Abp.Application.Services.Dto;

namespace HCMv3.Workflow.Dto;

public class WorkflowGroupFieldAccessDto : EntityDto<int>
{
    public int WorkflowGroupDetailId { get; set; }

    public int Step { get; set; }

    public string Field { get; set; }

    public bool IsVisible { get; set; }

    public bool IsEditable { get; set; }

    public bool IsRequired { get; set; }
}
