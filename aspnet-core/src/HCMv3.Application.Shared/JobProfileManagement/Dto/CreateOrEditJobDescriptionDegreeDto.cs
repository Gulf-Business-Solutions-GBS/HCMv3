using System.ComponentModel.DataAnnotations;

namespace HCMv3.JobProfileManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="JobDescriptionDegree"/>. <c>Id == null</c> means create;
/// otherwise update.
/// </summary>
public class CreateOrEditJobDescriptionDegreeDto
{
    public int? Id { get; set; }

    [Required]
    public int JobDescriptionId { get; set; }

    [Required]
    public int EducationDegreeId { get; set; }
}
