using System.ComponentModel.DataAnnotations;

namespace HCMv3.Recruitment.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="OnbTemplateDetails"/>. <c>Id == null</c> means create; otherwise
/// update.
/// </summary>
public class CreateOrEditOnbTemplateDetailsDto
{
    public int? Id { get; set; }

    [Required]
    public int OnbTemplateId { get; set; }

    public long TemplateId { get; set; }

    [StringLength(5000)]
    public string TemplateData { get; set; }
}
