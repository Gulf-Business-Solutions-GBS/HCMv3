using System.ComponentModel.DataAnnotations;

namespace HCMv3.CompetencyDevelopment.Dto;

/// <summary>Create/Update input DTO for <see cref="CDPlan"/>. <c>Id == null</c> means create.</summary>
public class CreateOrEditCDPlanDto
{
    public int? Id { get; set; }

    [Required]
    public int CDFormId { get; set; }

    [Required]
    public int CompetencyId { get; set; }

    [Range(0, 100)]
    public double Weight { get; set; }

    [StringLength(512)]
    public string Target { get; set; }

    [StringLength(500)]
    public string EmployeeComments { get; set; }

    [StringLength(500)]
    public string ManagerComments { get; set; }
}
