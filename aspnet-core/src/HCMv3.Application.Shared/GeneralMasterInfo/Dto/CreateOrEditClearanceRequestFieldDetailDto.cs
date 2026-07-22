namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="ClearanceRequestFieldDetail"/>. <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditClearanceRequestFieldDetailDto
{
    public int? Id { get; set; }

    public int? ClearanceRequestFieldId { get; set; }
}
