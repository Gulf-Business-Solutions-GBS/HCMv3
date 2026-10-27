using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.SurveyManagement;

/// <summary>
/// Aggregated child of <see cref="EmployeeSurvey"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.SurveyManagement.EmployeeSurveyDetail</c>), linked back via
/// <see cref="EmployeeSurveyId"/>. Represents an employee's answer to one <see cref="SurveyDetail"/>
/// question. Legacy <c>PerformValidation</c> parity (Question required) is enforced in
/// <c>EmployeeSurveyDetailsAppService</c>, not here.
/// </summary>
public class EmployeeSurveyDetail : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeSurveyId { get; set; }

    public virtual EmployeeSurvey EmployeeSurvey { get; set; }

    public virtual int QuestionId { get; set; }

    public virtual SurveyDetail Question { get; set; }

    [StringLength(2000)]
    public virtual string Answer { get; set; }
}
