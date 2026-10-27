using System.ComponentModel.DataAnnotations;

namespace HCMv3.JobProfileManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="JobDescriptionResponsibility"/>. <c>Id == null</c> means
/// create; otherwise update.
/// </summary>
public class CreateOrEditJobDescriptionResponsibilityDto
{
    public int? Id { get; set; }

    [Required]
    public int JobDescriptionId { get; set; }

    [Required]
    public int JobResponsibilityId { get; set; }
}
