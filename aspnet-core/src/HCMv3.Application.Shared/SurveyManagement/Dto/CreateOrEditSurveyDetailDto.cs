using System.ComponentModel.DataAnnotations;

namespace HCMv3.SurveyManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="SurveyDetail"/>. <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditSurveyDetailDto
{
    public int? Id { get; set; }

    [Required]
    public int SurveyId { get; set; }

    /// <summary>
    /// When 0, <c>SurveyDetailsAppService</c> auto-assigns the next OrderNo (legacy <c>PerformValidation</c>
    /// parity).
    /// </summary>
    public int OrderNo { get; set; }

    [Required]
    [StringLength(1000)]
    public string Question { get; set; }

    [Required]
    public QuestionType QuestionType { get; set; }

    [StringLength(2000)]
    public string Options { get; set; }
}
