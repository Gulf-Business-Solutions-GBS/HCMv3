using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.ProjectManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="EmployeeProject"/>. <c>Id == null</c> means create; otherwise
/// update.
/// </summary>
public class CreateOrEditEmployeeProjectDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public ProjectCostingMethod CostingMethod { get; set; }
}
