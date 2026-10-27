using System;
using Abp.Application.Services.Dto;

namespace HCMv3.Recruitment.Dto;

/// <summary>
/// Read/list DTO for <see cref="OnbApplicantTasks"/>.
/// </summary>
public class OnbApplicantTasksDto : EntityDto<int>
{
    public int ApplicantId { get; set; }

    public string ApplicantFullNameEn { get; set; }

    public int? OnbTaskId { get; set; }

    public string OnbTaskNameEn { get; set; }

    public int DueDays { get; set; }

    public bool IsCompleted { get; set; }

    public DateTime? CompletedOn { get; set; }

    public string TemplateUserData { get; set; }
}
