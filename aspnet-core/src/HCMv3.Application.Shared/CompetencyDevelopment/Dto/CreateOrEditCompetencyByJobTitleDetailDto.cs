using System.ComponentModel.DataAnnotations;

namespace HCMv3.CompetencyDevelopment.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="CompetencyByJobTitleDetail"/>. <c>Id == null</c> means create.
/// </summary>
public class CreateOrEditCompetencyByJobTitleDetailDto
{
    public int? Id { get; set; }

    [Required]
    public int CompetencyByJobTitleId { get; set; }

    [Required]
    public int CompetencyId { get; set; }

    [Range(1, 100)]
    public double Weight { get; set; }

    [Required]
    [StringLength(512)]
    public string Target { get; set; }
}
