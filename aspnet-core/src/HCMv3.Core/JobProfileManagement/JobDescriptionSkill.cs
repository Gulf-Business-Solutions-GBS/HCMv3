using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.JobProfileManagement;

/// <summary>
/// Aggregated child of <see cref="JobDescription"/> linking it to a <see cref="Skill"/> (ported from
/// legacy XAF <c>HCM10.Module.BusinessObjects.JobProfileManagement.JDSkill</c>). Legacy
/// <c>PerformValidation</c> parity (no duplicate Skill for the same JobDescription) is enforced in
/// <c>JobDescriptionSkillsAppService</c>, not here.
/// </summary>
public class JobDescriptionSkill : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int JobDescriptionId { get; set; }

    public virtual JobDescription JobDescription { get; set; }

    public virtual int SkillId { get; set; }

    public virtual Skill Skill { get; set; }
}
