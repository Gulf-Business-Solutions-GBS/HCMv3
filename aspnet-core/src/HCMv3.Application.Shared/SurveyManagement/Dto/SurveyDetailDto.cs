using Abp.Application.Services.Dto;

namespace HCMv3.SurveyManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="SurveyDetail"/>.
/// </summary>
public class SurveyDetailDto : EntityDto<int>
{
    public int SurveyId { get; set; }

    public int OrderNo { get; set; }

    public string Question { get; set; }

    public QuestionType QuestionType { get; set; }

    public string Options { get; set; }
}
