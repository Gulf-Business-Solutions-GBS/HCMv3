using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="EmployeeWageAccGroup"/>. <c>Id == null</c> means create;
/// otherwise update. EndDate is always server-managed (auto-chained) and is not part of the input.
/// </summary>
public class CreateOrEditEmployeeWageAccGroupDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    /// <summary>"Effective Date" in the legacy UI.</summary>
    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public int WageAccGroupId { get; set; }
}
