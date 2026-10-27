using System.ComponentModel.DataAnnotations;

namespace HCMv3.Workflow.Dto;

public class CreateOrEditRequestDetailDto
{
    public int? Id { get; set; }

    [Required]
    public int RequestMasterId { get; set; }

    public int Step { get; set; }

    public int? WorkflowRoleId { get; set; }

    public bool ShowPayment { get; set; }

    public bool CanSendBack { get; set; }

    public bool CanReject { get; set; }

    public bool ShouldEscalate { get; set; }

    public int EscalateAfterDays { get; set; }

    public bool EscalateNextLevel { get; set; }

    public int? EscalateWorkflowRoleId { get; set; }

    public WorkflowRoleGroup EscalateWorkflowRoleGroup { get; set; }

    public int? EscalateWorkflowRoleOrgId { get; set; }

    /// <summary>
    /// Whether to record who opened this step's requests without acting on them.
    ///
    /// <para>
    /// Nullable so that "not specified" is a thing a caller can say, which a plain bool cannot: false and
    /// unset are the same value, and the two need different answers. Unset means on -- an approver wanting
    /// to know whether a colleague has even looked is the ordinary case, and leaving it off by default
    /// meant the read log stayed empty on every chain until somebody knew the toggle existed.
    /// </para>
    /// </summary>
    public bool? EnableReadLog { get; set; }

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
