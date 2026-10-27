using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.Recruitment.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="InterviewDetails"/>. <c>Id == null</c> means create; otherwise
/// update.
/// </summary>
public class CreateOrEditInterviewDetailsDto
{
    public int? Id { get; set; }

    [Required]
    public int ApplicationStatusId { get; set; }

    [Required]
    public DateTime InterviewDate { get; set; }

    [Required]
    public DateTime InterviewTime { get; set; }

    [StringLength(256)]
    public string Place { get; set; }

    [Required]
    public int Interviewer1Id { get; set; }

    public int? Interviewer2Id { get; set; }

    public int? Interviewer3Id { get; set; }

    public string Comments1 { get; set; }

    public string Comments2 { get; set; }

    public string Comments3 { get; set; }
}
