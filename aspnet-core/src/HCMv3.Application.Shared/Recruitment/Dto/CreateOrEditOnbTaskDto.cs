using System.ComponentModel.DataAnnotations;

namespace HCMv3.Recruitment.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="OnbTask"/>. <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditOnbTaskDto
{
    public int? Id { get; set; }

    [Required]
    [StringLength(256)]
    public string TaskNameEn { get; set; }

    [Required]
    [StringLength(256)]
    public string TaskNameAr { get; set; }

    public int? OnbTemplateId { get; set; }

    public string Guideline { get; set; }

    public int DueDays { get; set; }
}
