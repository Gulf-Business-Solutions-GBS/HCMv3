using System.ComponentModel.DataAnnotations;

namespace HCMv3.CompetencyDevelopment.Dto;

/// <summary>Create/Update input DTO for <see cref="CDPeriod"/>. <c>Id == null</c> means create.</summary>
public class CreateOrEditCDPeriodDto
{
    public int? Id { get; set; }

    [Required]
    [StringLength(256)]
    public string Name { get; set; }

    public bool Closed { get; set; }
}
