using System.ComponentModel.DataAnnotations;

namespace HCMv3.Recruitment.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="OnbTemplate"/>. <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditOnbTemplateDto
{
    public int? Id { get; set; }

    public long TemplateId { get; set; }

    [StringLength(256)]
    public string TemplateTitle { get; set; }
}
