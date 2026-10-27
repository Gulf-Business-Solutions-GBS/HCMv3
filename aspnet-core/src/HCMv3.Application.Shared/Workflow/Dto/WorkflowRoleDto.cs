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

    /// <summary>
    /// How many employees hold this role today -- the people a step pointing at it would actually reach.
    /// Counted on today's date because an assignment outside its dates is invisible to approver resolution.
    /// </summary>
    public int EmployeeCount { get; set; }
}
