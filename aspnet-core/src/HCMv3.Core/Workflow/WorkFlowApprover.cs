using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.Workflow;

/// <summary>
/// Current approver assignment for a <see cref="WorkflowRequest"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Workflow.General.WorkFlowApprover</c> -- note the legacy file lives
/// under the <c>General</c> folder despite belonging conceptually to the core workflow engine). Legacy
/// computed <c>JobTitle</c> (<c>Approver.CurrentJobtitle.NameEn</c>) is a display-only helper, not ported
/// as entity state.
/// </summary>
public class WorkFlowApprover : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int WorkflowRequestId { get; set; }

    public virtual WorkflowRequest WorkflowRequest { get; set; }

    public virtual int? ApproverId { get; set; }

    public virtual Employee Approver { get; set; }

    public virtual bool IsApproved { get; set; }
}
