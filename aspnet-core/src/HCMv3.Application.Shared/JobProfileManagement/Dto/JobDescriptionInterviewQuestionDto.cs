using Abp.Application.Services.Dto;

namespace HCMv3.JobProfileManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="JobDescriptionInterviewQuestion"/>.
/// </summary>
public class JobDescriptionInterviewQuestionDto : EntityDto<int>
{
    public int JobDescriptionId { get; set; }

    public int InterviewQuestionId { get; set; }

    public string InterviewQuestionNameEn { get; set; }
}
