using System.ComponentModel.DataAnnotations;

namespace HCMv3.Recruitment.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="ApplicationStatus"/>. <c>Id == null</c> means create; otherwise
/// update.
/// </summary>
public class CreateOrEditApplicationStatusDto
{
    public int? Id { get; set; }

    public int? VacancyId { get; set; }

    public int? ApplicantCVId { get; set; }

    [Required]
    public AppStatus AppStatus { get; set; }

    [StringLength(4000)]
    public string CoverLetter { get; set; }
}
