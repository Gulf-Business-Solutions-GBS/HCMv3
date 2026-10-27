using System.ComponentModel.DataAnnotations;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="AbsenceTypeException"/>. <c>Id == null</c> means create;
/// otherwise update.
/// </summary>
public class CreateOrEditAbsenceTypeExceptionDto
{
    public int? Id { get; set; }

    [Required]
    public int AbsenceTypeId { get; set; }

    [Required]
    public int ExceptAbsenceTypeId { get; set; }
}
