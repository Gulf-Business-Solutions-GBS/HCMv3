using Abp.Application.Services.Dto;

namespace HCMv3.SurveyManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="EmployeeSurveyDetail"/>.
/// </summary>
public class EmployeeSurveyDetailDto : EntityDto<int>
{
    public int EmployeeSurveyId { get; set; }

    public int QuestionId { get; set; }

    public string QuestionText { get; set; }

    public QuestionType QuestionType { get; set; }

    public string Answer { get; set; }
}
