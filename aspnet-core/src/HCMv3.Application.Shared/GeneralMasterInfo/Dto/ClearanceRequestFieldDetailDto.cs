using Abp.Application.Services.Dto;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Read/list DTO for <see cref="ClearanceRequestFieldDetail"/>, i.e. which department owns a checklist line.
/// </summary>
public class ClearanceRequestFieldDetailDto : EntityDto<int>
{
    public int? ClearanceRequestFieldId { get; set; }

    public string ClearanceRequestFieldName { get; set; }

    /// <summary>The approver role that owns this checklist line -- the department that has to answer it.</summary>
    public int WorkflowRoleId { get; set; }

    public string WorkflowRoleName { get; set; }
}
