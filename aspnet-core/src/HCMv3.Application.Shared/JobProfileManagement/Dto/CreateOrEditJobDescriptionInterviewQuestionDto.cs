using System.ComponentModel.DataAnnotations;

namespace HCMv3.JobProfileManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="JobDescriptionInterviewQuestion"/>. <c>Id == null</c> means
/// create; otherwise update.
/// </summary>
public class CreateOrEditJobDescriptionInterviewQuestionDto
{
    public int? Id { get; set; }

    [Required]
    public int JobDescriptionId { get; set; }

    [Required]
    public int InterviewQuestionId { get; set; }
}
