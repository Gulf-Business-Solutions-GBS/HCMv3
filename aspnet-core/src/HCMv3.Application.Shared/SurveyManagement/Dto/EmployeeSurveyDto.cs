using Abp.Application.Services.Dto;

namespace HCMv3.SurveyManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="EmployeeSurvey"/>.
/// </summary>
public class EmployeeSurveyDto : EntityDto<int>
{
    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public int SurveyId { get; set; }

    public string SurveyNameEn { get; set; }
}
