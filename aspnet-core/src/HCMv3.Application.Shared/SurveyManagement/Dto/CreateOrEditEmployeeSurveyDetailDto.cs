using System.ComponentModel.DataAnnotations;

namespace HCMv3.SurveyManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="EmployeeSurveyDetail"/>. <c>Id == null</c> means create;
/// otherwise update.
/// </summary>
public class CreateOrEditEmployeeSurveyDetailDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeSurveyId { get; set; }

    [Required]
    public int QuestionId { get; set; }

    [StringLength(2000)]
    public string Answer { get; set; }
}
