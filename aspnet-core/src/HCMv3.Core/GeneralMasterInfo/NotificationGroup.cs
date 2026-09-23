using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.OrganizationManagement;
using HCMv3.Workflow;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// NotificationGroup (ported from legacy XAF <c>HCM10.Module.BusinessObjects.GeneralMasterInfo.NotificationGroup</c>).
/// Pure detail record: no Code/NameEn/NameAr. Owned by a <see cref="NotificationConfig"/>.
///
/// <para>
/// One extra set of recipients for its parent configuration: everybody who currently holds
/// <see cref="WorkflowRole"/>, optionally narrowed to the people in the requester's own part of the
/// organisation. The three FKs that say this were dropped in the first pass of this port (the Workflow and
/// Organization modules were out of scope then) and restored on 2026-09-22, now that both exist and
/// <see cref="EmployeeWorkflowRole"/> can answer who holds a role.
/// </para>
/// </summary>
public class NotificationGroup : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int? NotificationConfigId { get; set; }

    public virtual NotificationConfig NotificationConfig { get; set; }

    /// <summary>The role whose current holders are notified.</summary>
    public virtual int? WorkflowRoleId { get; set; }

    /// <inheritdoc cref="WorkflowRoleId"/>
    public virtual WorkflowRole WorkflowRole { get; set; }

    /// <summary>
    /// Which part of the organisation the role holders must share with the requester — their company,
    /// department, branch or cost centre. <see cref="Workflow.WorkflowRoleGroup.None"/> notifies every holder
    /// of the role regardless of where they sit.
    /// </summary>
    public virtual WorkflowRoleGroup WorkflowRoleGroup { get; set; }

    /// <summary>
    /// A specific organisation unit to notify instead of the requester's own, when the configuration names
    /// one. Left empty, the requester's unit for <see cref="WorkflowRoleGroup"/> is used.
    /// </summary>
    public virtual int? WorkflowRoleOrgId { get; set; }

    /// <inheritdoc cref="WorkflowRoleOrgId"/>
    public virtual OrganizationUnitType WorkflowRoleOrg { get; set; }
}
