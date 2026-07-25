using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.SurveyManagement;

/// <summary>
/// Survey master (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.SurveyManagement.Survey</c>). Code/NameEn/NameAr + StartDate/EndDate, no
/// FK, so the shape mirrors <c>ProjectManagement.Project</c> minus the type FK. Legacy
/// <c>PerformValidation</c> parity (auto Code, duplicate Code/Name checks, StartDate/EndDate required,
/// EndDate &gt;= StartDate) is enforced in <c>SurveysAppService</c>, not here. Its aggregated child is
/// <see cref="SurveyDetail"/>, linked back via <see cref="SurveyDetail.SurveyId"/>.
/// </summary>
public class Survey : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [StringLength(SimpleLookupConsts.MaxCodeLength)]
    public virtual string Code { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public virtual string NameEn { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public virtual string NameAr { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }
}
