using System.ComponentModel.DataAnnotations;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="NotificationConfig"/>. <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditNotificationConfigDto
{
    public int? Id { get; set; }

    [Required]
    [StringLength(256)]
    public string Name { get; set; }

    public bool SendNotification { get; set; }

    public bool SendEmail { get; set; }

    public bool ToEmployee { get; set; }

    public bool ToRequestCreator { get; set; }

    public bool ToManager { get; set; }

    public bool SendToAll { get; set; }

    public bool SendToAllApproved { get; set; }

    public bool SendToApprover { get; set; }

    /// <summary>
    /// The template this configuration sends. Optional -- without one it notifies in the app only.
    /// </summary>
    public int? EmailTemplateId { get; set; }

    /// <summary>
    /// Who else to tell: everyone holding a workflow role, optionally only in the requester's own part of the
    /// organisation. Edited inside the configuration, as legacy did; the list sent replaces the one saved.
    /// </summary>
    public System.Collections.Generic.List<NotificationConfigGroupDto> Groups { get; set; } = new();
}

/// <summary>One notification group line, edited inside its configuration.</summary>
public class NotificationConfigGroupDto
{
    [Required]
    public int? WorkflowRoleId { get; set; }

    /// <summary>Narrows the role holders to the requester's company, branch, department or cost centre.</summary>
    public HCMv3.Workflow.WorkflowRoleGroup WorkflowRoleGroup { get; set; }

    /// <summary>
    /// Narrows the role holders to the requester's organisation unit at this level (legacy WorkflowRoleOrg).
    /// Applied when "Enable org unit type in workflow" is on in the workflow settings.
    /// </summary>
    public int? WorkflowRoleOrgId { get; set; }
}
