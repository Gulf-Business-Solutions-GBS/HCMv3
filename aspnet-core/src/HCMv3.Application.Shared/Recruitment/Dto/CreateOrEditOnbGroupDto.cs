using System.ComponentModel.DataAnnotations;

namespace HCMv3.Recruitment.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="OnbGroup"/>. <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditOnbGroupDto
{
    public int? Id { get; set; }

    [Required]
    [StringLength(256)]
    public string NameEn { get; set; }

    [Required]
    [StringLength(256)]
    public string NameAr { get; set; }
}
