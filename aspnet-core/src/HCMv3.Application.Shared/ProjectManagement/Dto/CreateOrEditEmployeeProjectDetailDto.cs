using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.ProjectManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="EmployeeProjectDetail"/>. <c>Id == null</c> means create;
/// otherwise update.
/// </summary>
public class CreateOrEditEmployeeProjectDetailDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeProjectId { get; set; }

    [Required]
    public int ProjectId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public double Percentage { get; set; }
}
