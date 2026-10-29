using System.Collections.Generic;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;
using HCMv3.OrganizationManagement;

namespace HCMv3.Workflow;

/// <summary>
/// One approval step of a <see cref="RequestMaster"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Workflow.WorkflowSetup.RequestDetail</c>). Legacy step-renumbering /
/// dynamic-step-shifting logic (<c>PerformValidation</c>, <c>AddStep</c>, <c>OnDeleted</c>) is enforced in
/// <c>RequestDetailsAppService</c>, not here.
///
/// Each step carries its own notification settings, one per action, as legacy did: who is told when a request
/// is submitted, approved, rejected, sent back, commented on or reassigned, and who is reminded while it waits.
/// They were dropped in the first pass of this port (the "blocked cross-module dependency" pattern) and restored
/// on 2026-09-22 once <see cref="HCMv3.GeneralMasterInfo.NotificationConfig"/> was in place. All are optional:
/// a step with no configuration for an action simply sends nothing for it.
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

    /// <summary>
    /// For <see cref="WorkflowApproverPersonType.PersonOnRequest"/>: which person field of the request names
    /// the approver, as a <see cref="WorkflowRequestPersonFields"/> code. Null for every other approver type.
    /// </summary>
    [System.ComponentModel.DataAnnotations.StringLength(WorkflowRequestPersonFields.MaxCodeLength)]
    public virtual string RequestPersonField { get; set; }

    public virtual WorkflowRoleGroup WorkflowRoleGroup { get; set; }

    public virtual int? WorkflowRoleOrgId { get; set; }

    public virtual OrganizationUnitType WorkflowRoleOrg { get; set; }

    public virtual ApprovalRequire ApprovalRequire { get; set; }

    public virtual bool IsCommentRequire { get; set; }

    public virtual bool SignatoryStep { get; set; }

    public virtual int ReminderDays { get; set; }

    /// <summary>Who is told when this step APPROVES (legacy <c>NotificationConfiguration</c>).</summary>
    public virtual int? NotificationConfigId { get; set; }

    /// <inheritdoc cref="NotificationConfigId"/>
    public virtual NotificationConfig NotificationConfig { get; set; }

    /// <summary>Who is told when the request is SUBMITTED to this step.</summary>
    public virtual int? SubmitNotificationConfigId { get; set; }

    /// <inheritdoc cref="SubmitNotificationConfigId"/>
    public virtual NotificationConfig SubmitNotificationConfig { get; set; }

    /// <summary>Who is told when this step REJECTS.</summary>
    public virtual int? RejectNotificationConfigId { get; set; }

    /// <inheritdoc cref="RejectNotificationConfigId"/>
    public virtual NotificationConfig RejectNotificationConfig { get; set; }

    /// <summary>Who is told when this step SENDS BACK.</summary>
    public virtual int? SendBackNotificationConfigId { get; set; }

    /// <inheritdoc cref="SendBackNotificationConfigId"/>
    public virtual NotificationConfig SendBackNotificationConfig { get; set; }

    /// <summary>Who is told when a REMARK is added at this step.</summary>
    public virtual int? RemarksNotificationConfigId { get; set; }

    /// <inheritdoc cref="RemarksNotificationConfigId"/>
    public virtual NotificationConfig RemarksNotificationConfig { get; set; }

    /// <summary>Who is told when the request is REASSIGNED at this step.</summary>
    public virtual int? AssignNotificationConfigId { get; set; }

    /// <inheritdoc cref="AssignNotificationConfigId"/>
    public virtual NotificationConfig AssignNotificationConfig { get; set; }

    /// <summary>Who is REMINDED while this step has the request, every <see cref="ReminderDays"/> days.</summary>
    public virtual int? ReminderNotificationConfigId { get; set; }

    /// <inheritdoc cref="ReminderNotificationConfigId"/>
    public virtual NotificationConfig ReminderNotificationConfig { get; set; }

    public virtual ICollection<RequestDetailParallel> RequestDetailParallels { get; set; }
}
