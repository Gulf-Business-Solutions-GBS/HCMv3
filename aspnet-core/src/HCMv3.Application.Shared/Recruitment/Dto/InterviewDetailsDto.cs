using System;
using Abp.Application.Services.Dto;

namespace HCMv3.Recruitment.Dto;

/// <summary>
/// Read/list DTO for <see cref="InterviewDetails"/>.
/// </summary>
public class InterviewDetailsDto : EntityDto<int>
{
    public int ApplicationStatusId { get; set; }

    public DateTime InterviewDate { get; set; }

    public DateTime InterviewTime { get; set; }

    public string Place { get; set; }

    public int? Interviewer1Id { get; set; }

    public int? Interviewer2Id { get; set; }

    public int? Interviewer3Id { get; set; }

    public string Comments1 { get; set; }

    public string Comments2 { get; set; }

    public string Comments3 { get; set; }
}
