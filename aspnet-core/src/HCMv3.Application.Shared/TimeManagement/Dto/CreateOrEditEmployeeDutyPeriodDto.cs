using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="EmployeeDutyPeriod"/>. <c>Id == null</c> means create;
/// otherwise update. <c>EndDate</c> is server-computed by the EndDate auto-chaining logic, so it is not
/// part of this input (matches <c>CreateOrEditEmployeeOrgAssignmentDto</c> convention).
/// </summary>
public class CreateOrEditEmployeeDutyPeriodDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int DutyPeriodId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }
}
