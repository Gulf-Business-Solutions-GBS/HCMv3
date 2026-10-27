using System.ComponentModel.DataAnnotations;

namespace HCMv3.Recruitment.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="OnbVideoLink"/>. <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditOnbVideoLinkDto
{
    public int? Id { get; set; }

    [Required]
    [StringLength(256)]
    public string NameEn { get; set; }

    [Required]
    [StringLength(256)]
    public string NameAr { get; set; }

    [StringLength(500)]
    public string Link { get; set; }

    public bool IsActive { get; set; }
}
