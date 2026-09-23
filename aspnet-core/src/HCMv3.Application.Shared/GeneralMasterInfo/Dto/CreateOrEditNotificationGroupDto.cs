using HCMv3.Workflow;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="NotificationGroup"/>. <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditNotificationGroupDto
{
    public int? Id { get; set; }

    public int? NotificationConfigId { get; set; }

    /// <summary>The role whose current holders this group notifies.</summary>
    public int? WorkflowRoleId { get; set; }

    /// <summary>Which slice of the organisation the holders must share with the requester.</summary>
    public WorkflowRoleGroup WorkflowRoleGroup { get; set; }

    public int? WorkflowRoleOrgId { get; set; }
}
