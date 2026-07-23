using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="EmployeeSubGroupBonus"/>. <c>Id == null</c> means create;
/// otherwise update.
/// </summary>
public class CreateOrEditEmployeeSubGroupBonusDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeSubGroupId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public double ChangeYear { get; set; }

    public double Percentage { get; set; }
}
