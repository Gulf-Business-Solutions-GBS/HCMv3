using System.ComponentModel.DataAnnotations;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="MonthlyTimeRegisterDetail"/>. <c>Id == null</c> means create;
/// otherwise update.
/// </summary>
public class CreateOrEditMonthlyTimeRegisterDetailDto
{
    public int? Id { get; set; }

    [Required]
    public int MonthlyTimeRegisterId { get; set; }

    [Required]
    public int TimeTypeId { get; set; }

    public string TimeValue { get; set; }
}
