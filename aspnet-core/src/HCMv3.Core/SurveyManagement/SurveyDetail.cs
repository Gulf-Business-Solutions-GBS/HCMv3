using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.SurveyManagement;

/// <summary>
/// Aggregated child of <see cref="Survey"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.SurveyManagement.SurveyDetail</c>), linked back via
/// <see cref="SurveyId"/>. Represents one question in the survey. Legacy <c>PerformValidation</c> parity
/// (auto OrderNo, Question required, Options required when QuestionType is RadioList/CheckboxList) is
/// enforced in <c>SurveyDetailsAppService</c>, not here.
/// </summary>
public class SurveyDetail : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int SurveyId { get; set; }

    public virtual Survey Survey { get; set; }

    public virtual int OrderNo { get; set; }

    [Required]
    [StringLength(1000)]
    public virtual string Question { get; set; }

    public virtual QuestionType QuestionType { get; set; }

    [StringLength(2000)]
    public virtual string Options { get; set; }
}
