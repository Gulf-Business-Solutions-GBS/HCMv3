using Abp.Application.Services.Dto;
using HCMv3.Workflow;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Read/list DTO for <see cref="NotificationGroup"/>.
/// </summary>
public class NotificationGroupDto : EntityDto<int>
{
    public int? NotificationConfigId { get; set; }

    public string NotificationConfigName { get; set; }

    /// <summary>The role whose current holders this group notifies.</summary>
    public int? WorkflowRoleId { get; set; }

    public string WorkflowRoleNameEn { get; set; }

    /// <summary>Which slice of the organisation the holders must share with the requester.</summary>
    public WorkflowRoleGroup WorkflowRoleGroup { get; set; }

    public int? WorkflowRoleOrgId { get; set; }
}
