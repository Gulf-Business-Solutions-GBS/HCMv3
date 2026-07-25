using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.CompetencyDevelopment.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="CompetencyByJobTitle"/>. <c>Id == null</c> means create.
/// </summary>
public class CreateOrEditCompetencyByJobTitleDto
{
    public int? Id { get; set; }

    [Required]
    public int JobTitleId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }
}
