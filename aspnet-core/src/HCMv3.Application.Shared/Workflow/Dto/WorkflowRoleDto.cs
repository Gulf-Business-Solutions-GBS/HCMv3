using Abp.Application.Services.Dto;
using HCMv3.Workflow;

namespace HCMv3.Workflow.Dto;

public class WorkflowRoleDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public WorkflowRoleGroup WorkflowRoleGroup { get; set; }

    public int? WorkflowRoleOrgId { get; set; }

    public string WorkflowRoleOrgNameEn { get; set; }
}
