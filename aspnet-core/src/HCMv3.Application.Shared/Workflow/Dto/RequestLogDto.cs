using System;
using Abp.Application.Services.Dto;

namespace HCMv3.Workflow.Dto;

public class RequestLogDto : EntityDto<int>
{
    public int WorkflowRequestId { get; set; }

    public int? ActionById { get; set; }

    public string ActionByNameEn { get; set; }

    public string RequestActionComment { get; set; }

    public DateTime ActionDateTime { get; set; }

    public RequestActionType ActionType { get; set; }

    public int WorkflowStep { get; set; }

    public int ActionDays { get; set; }

    public WorkflowPlatform Platform { get; set; }
}
