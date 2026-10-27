using System.ComponentModel.DataAnnotations;

namespace HCMv3.Recruitment.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="OnbAppTaskComments"/>. <c>Id == null</c> means create; otherwise
/// update.
/// </summary>
public class CreateOrEditOnbAppTaskCommentsDto
{
    public int? Id { get; set; }

    [Required]
    public int OnbApplicantTasksId { get; set; }

    [StringLength(4000)]
    public string Comments { get; set; }

    public bool IsByAdmin { get; set; }
}
