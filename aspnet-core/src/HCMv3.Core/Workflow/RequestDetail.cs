using System.Collections.Generic;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.OrganizationManagement;

namespace HCMv3.Workflow;

/// <summary>
/// One approval step of a <see cref="RequestMaster"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Workflow.WorkflowSetup.RequestDetail</c>). Legacy step-renumbering /
/// dynamic-step-shifting logic (<c>PerformValidation</c>, <c>AddStep</c>, <c>OnDeleted</c>) is enforced in
/// <c>RequestDetailsAppService</c>, not here.
///
/// DELIBERATE SIMPLIFICATION: the legacy entity's <c>NotificationConfiguration</c> /
/// <c>RejectNotificationConfiguration</c> / <c>SendBackNotificationConfiguration</c> /
/// <c>SubmitNotificationConfiguration</c> / <c>RemarksNotificationConfiguration</c> /
/// <c>AssignNotificationConfiguration</c> / <c>ReminderNotificationConfiguration</c> fields (all FKs to the
/// unported <c>NotificationConfig</c> entity) are dropped — same "blocked cross-module dependency" pattern
/// used throughout this port (cf. <c>DocAttachment</c> dropped everywhere it appeared). <c>ReminderDays</c>
/// is kept as a plain int since it has no FK dependency.
/// </summary>
public class RequestDetail : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int RequestMasterId { get; set; }

    public virtual RequestMaster RequestMaster { get; set; }

    public virtual int Step { get; set; }

    public virtual int? WorkflowRoleId { get; set; }

    public virtual WorkflowRole WorkflowRole { get; set; }

    public virtual bool ShowPayment { get; set; }

    public virtual bool CanSendBack { get; set; }

    public virtual bool CanReject { get; set; }

    public virtual bool ShouldEscalate { get; set; }

    public virtual int EscalateAfterDays { get; set; }

    public virtual bool EscalateNextLevel { get; set; }

    public virtual int? EscalateWorkflowRoleId { get; set; }

    public virtual WorkflowRole EscalateWorkflowRole { get; set; }

    public virtual WorkflowRoleGroup EscalateWorkflowRoleGroup { get; set; }

    public virtual int? EscalateWorkflowRoleOrgId { get; set; }

    public virtual OrganizationUnitType EscalateWorkflowRoleOrg { get; set; }

    public virtual bool EnableReadLog { get; set; }

    public virtual WorkflowPersonScope Person { get; set; }

    public virtual WorkflowApproverPersonType ApproverPerson { get; set; }

    public virtual WorkflowRoleGroup WorkflowRoleGroup { get; set; }

    public virtual int? WorkflowRoleOrgId { get; set; }

    public virtual OrganizationUnitType WorkflowRoleOrg { get; set; }

    public virtual ApprovalRequire ApprovalRequire { get; set; }

    public virtual bool IsCommentRequire { get; set; }

    public virtual bool SignatoryStep { get; set; }

    public virtual int ReminderDays { get; set; }

    public virtual ICollection<RequestDetailParallel> RequestDetailParallels { get; set; }
}
