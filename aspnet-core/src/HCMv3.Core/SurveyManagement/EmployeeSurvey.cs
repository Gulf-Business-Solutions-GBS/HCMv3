using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.SurveyManagement;

/// <summary>
/// Header row assigning an Employee to a <see cref="Survey"/> to answer (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.SurveyManagement.EmployeeSurvey</c>). Legacy <c>PerformValidation</c>
/// parity (Employee and Survey both required) is enforced in <c>EmployeeSurveysAppService</c>, not here --
/// both are already non-nullable FK ints here, so the check is naturally satisfied by the DTO's
/// <c>[Required]</c> attributes. Its aggregated child is <see cref="EmployeeSurveyDetail"/>, linked back via
/// <see cref="EmployeeSurveyDetail.EmployeeSurveyId"/>.
/// </summary>
public class EmployeeSurvey : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int SurveyId { get; set; }

    public virtual Survey Survey { get; set; }
}
