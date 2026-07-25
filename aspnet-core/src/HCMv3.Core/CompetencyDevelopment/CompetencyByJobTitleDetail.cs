using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.CompetencyDevelopment;

/// <summary>
/// One weighted <see cref="Competency"/> target within a <see cref="CompetencyByJobTitle"/> profile (ported
/// from legacy XAF <c>HCM10.Module.BusinessObjects.CompetencyDevelopment.CompetencyByJobTitleDetail</c>, an
/// aggregated child of <see cref="CompetencyByJobTitle"/> -- see its class remarks).
/// </summary>
public class CompetencyByJobTitleDetail : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int CompetencyByJobTitleId { get; set; }

    public virtual CompetencyByJobTitle CompetencyByJobTitle { get; set; }

    public virtual int CompetencyId { get; set; }

    public virtual Competency Competency { get; set; }

    public virtual double Weight { get; set; }

    [StringLength(512)]
    public virtual string Target { get; set; }
}
