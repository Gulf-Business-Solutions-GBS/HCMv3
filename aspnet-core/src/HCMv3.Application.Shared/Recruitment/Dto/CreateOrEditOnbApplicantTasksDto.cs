using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.Recruitment.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="OnbApplicantTasks"/>. <c>Id == null</c> means create; otherwise
/// update.
/// </summary>
public class CreateOrEditOnbApplicantTasksDto
{
    public int? Id { get; set; }

    [Required]
    public int ApplicantId { get; set; }

    public int? OnbTaskId { get; set; }

    public int DueDays { get; set; }

    public bool IsCompleted { get; set; }

    public DateTime? CompletedOn { get; set; }

    [StringLength(4000)]
    public string TemplateUserData { get; set; }
}
