using System.ComponentModel.DataAnnotations;

namespace HCMv3.Recruitment.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="CVQualification"/>. <c>Id == null</c> means create; otherwise
/// update.
/// </summary>
public class CreateOrEditCVQualificationDto
{
    public int? Id { get; set; }

    [Required]
    public int ApplicantCVId { get; set; }

    public int? QualificationId { get; set; }

    [StringLength(256)]
    public string University { get; set; }

    public double Percentage { get; set; }

    public int PassedYear { get; set; }
}
