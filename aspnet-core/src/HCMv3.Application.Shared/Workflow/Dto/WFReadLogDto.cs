using System;
using Abp.Application.Services.Dto;

namespace HCMv3.Workflow.Dto;

public class WFReadLogDto : EntityDto<int>
{
    public int WorkflowRequestId { get; set; }

    public DateTime ReadDateTime { get; set; }

    public string User { get; set; }
}
