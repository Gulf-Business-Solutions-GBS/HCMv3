using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.Recruitment;

/// <summary>
/// Skill line of an <see cref="ApplicantCV"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Recruitment.CVSkill</c>).
/// </summary>
public class CVSkill : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [Required]
    public virtual int ApplicantCVId { get; set; }
    public virtual ApplicantCV ApplicantCV { get; set; }

    [StringLength(256)]
    public virtual string Skill { get; set; }

    public virtual ProficiencyType Proficiency { get; set; }
}
