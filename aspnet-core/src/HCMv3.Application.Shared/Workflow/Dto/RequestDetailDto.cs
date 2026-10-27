using Abp.Application.Services.Dto;

namespace HCMv3.Workflow.Dto;

public class RequestDetailDto : EntityDto<int>
{
    public int RequestMasterId { get; set; }

    public int Step { get; set; }

    public int? WorkflowRoleId { get; set; }

    public string WorkflowRoleNameEn { get; set; }

    public bool ShowPayment { get; set; }

    public bool CanSendBack { get; set; }

    public bool CanReject { get; set; }

    public bool ShouldEscalate { get; set; }

    public int EscalateAfterDays { get; set; }

    public bool EscalateNextLevel { get; set; }

    public int? EscalateWorkflowRoleId { get; set; }

    public WorkflowRoleGroup EscalateWorkflowRoleGroup { get; set; }

    public int? EscalateWorkflowRoleOrgId { get; set; }

    public bool EnableReadLog { get; set; }

    public WorkflowPersonScope Person { get; set; }

    public WorkflowApproverPersonType ApproverPerson { get; set; }

    public WorkflowRoleGroup WorkflowRoleGroup { get; set; }

    public int? WorkflowRoleOrgId { get; set; }

    public ApprovalRequire ApprovalRequire { get; set; }

    public bool IsCommentRequire { get; set; }

    public bool SignatoryStep { get; set; }

    public int ReminderDays { get; set; }

    /// <summary>
    /// Who is told when this step acts, one setting per action. All optional: an action with nothing chosen
    /// notifies nobody, which is how every chain behaved before these were restored.
    /// </summary>
    public int? NotificationConfigId { get; set; }

    /// <inheritdoc cref="NotificationConfigId"/>
    public int? SubmitNotificationConfigId { get; set; }

    /// <inheritdoc cref="NotificationConfigId"/>
    public int? RejectNotificationConfigId { get; set; }

    /// <inheritdoc cref="NotificationConfigId"/>
    public int? SendBackNotificationConfigId { get; set; }

    /// <inheritdoc cref="NotificationConfigId"/>
    public int? RemarksNotificationConfigId { get; set; }

    /// <inheritdoc cref="NotificationConfigId"/>
    public int? AssignNotificationConfigId { get; set; }

    /// <inheritdoc cref="NotificationConfigId"/>
    public int? ReminderNotificationConfigId { get; set; }
}
