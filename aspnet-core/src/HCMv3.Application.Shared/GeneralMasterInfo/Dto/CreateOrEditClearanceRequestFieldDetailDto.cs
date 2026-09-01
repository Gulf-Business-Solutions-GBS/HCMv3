using System.ComponentModel.DataAnnotations;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="ClearanceRequestFieldDetail"/>. <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditClearanceRequestFieldDetailDto
{
    public int? Id { get; set; }

    public int? ClearanceRequestFieldId { get; set; }

    /// <summary>
    /// The approver role that owns this checklist line. Required, because that ownership is the only thing
    /// this record says: without it the line appears on nobody's clearance form.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int WorkflowRoleId { get; set; }
}
