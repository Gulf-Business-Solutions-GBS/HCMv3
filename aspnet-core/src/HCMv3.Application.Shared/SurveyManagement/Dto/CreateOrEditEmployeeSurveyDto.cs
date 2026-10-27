using System.ComponentModel.DataAnnotations;

namespace HCMv3.SurveyManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="EmployeeSurvey"/>. <c>Id == null</c> means create; otherwise
/// update.
/// </summary>
public class CreateOrEditEmployeeSurveyDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int SurveyId { get; set; }
}
