using System.ComponentModel.DataAnnotations;

namespace HCMv3.ManpowerPlanningManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="ManpowerPlanning"/>. <c>Id == null</c> means create; otherwise
/// update.
/// </summary>
public class CreateOrEditManpowerPlanningDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public long OrganizationUnitId { get; set; }

    [Required]
    public int PeriodId { get; set; }

    [StringLength(1000)]
    public string RequestComments { get; set; }
}
