using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="EmployeeTimePolicy"/>. <c>Id == null</c> means create;
/// otherwise update. <c>EndDate</c> is server-computed by the EndDate auto-chaining logic, so it is not
/// part of this input.
/// </summary>
public class CreateOrEditEmployeeTimePolicyDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int TimePolicyId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }
}
