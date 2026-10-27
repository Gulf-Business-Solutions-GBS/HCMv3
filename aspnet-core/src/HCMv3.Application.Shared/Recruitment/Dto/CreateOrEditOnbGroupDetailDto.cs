using System.ComponentModel.DataAnnotations;

namespace HCMv3.Recruitment.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="OnbGroupDetail"/>. <c>Id == null</c> means create; otherwise
/// update.
/// </summary>
public class CreateOrEditOnbGroupDetailDto
{
    public int? Id { get; set; }

    [Required]
    public int OnbGroupId { get; set; }

    public int? OnbTaskId { get; set; }

    public int? JobTitleId { get; set; }
}
