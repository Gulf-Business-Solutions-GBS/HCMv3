using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.Recruitment;

/// <summary>
/// Language proficiency line of an <see cref="ApplicantCV"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Recruitment.CVLanguage</c>).
/// </summary>
public class CVLanguage : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int ApplicantCVId { get; set; }
    public virtual ApplicantCV ApplicantCV { get; set; }

    public virtual int? LanguageInfoId { get; set; }
    public virtual LanguageInfo LanguageInfo { get; set; }

    public virtual ProficiencyType Proficiency { get; set; }
}
