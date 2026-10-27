using System.ComponentModel.DataAnnotations;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="WageTypeException"/>. <c>Id == null</c> means create; otherwise
/// update.
/// </summary>
public class CreateOrEditWageTypeExceptionDto
{
    public int? Id { get; set; }

    [Required]
    public int WageTypeId { get; set; }

    [Required]
    public int ExceptWageTypeId { get; set; }
}
