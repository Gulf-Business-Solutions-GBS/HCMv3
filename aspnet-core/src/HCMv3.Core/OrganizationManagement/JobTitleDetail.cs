using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.Workflow;

namespace HCMv3.OrganizationManagement;

/// <summary>
/// Legacy <c>JobTitleDetail</c>: a workflow role that holders of a job title take on by default. Legacy reads
/// it in <c>WorkFlowServices.GetWorkflowParallelApprover</c> as the fallback when a parallel approval step
/// names no roles; HCMv3 has no parallel approval step yet, so the rows are kept ready for it.
/// </summary>
public class JobTitleDetail : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int JobTitleId { get; set; }

    public virtual JobTitle JobTitle { get; set; }

    public virtual int WorkflowRoleId { get; set; }

    public virtual WorkflowRole WorkflowRole { get; set; }
}
