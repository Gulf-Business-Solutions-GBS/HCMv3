namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="DisciplinaryTypeDetail"/>. <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditDisciplinaryTypeDetailDto
{
    public int? Id { get; set; }

    public int? DisciplinaryTypeId { get; set; }

    public int DisciplinaryCount { get; set; }

    public int? DisciplinaryDeductionTypeId { get; set; }
}
