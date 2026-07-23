using System.ComponentModel.DataAnnotations;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="EmployeeSubGroupWageType"/>. <c>Id == null</c> means create;
/// otherwise update.
/// </summary>
public class CreateOrEditEmployeeSubGroupWageTypeDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeSubGroupId { get; set; }

    public int? WageTypeId { get; set; }
}
