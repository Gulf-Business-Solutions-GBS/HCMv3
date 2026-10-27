using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.JobProfileManagement;

/// <summary>
/// Aggregated child of <see cref="JobDescription"/> linking it to a <see cref="JobResponsibility"/>
/// (ported from legacy XAF <c>HCM10.Module.BusinessObjects.JobProfileManagement.JDResponsibility</c>).
/// Legacy <c>PerformValidation</c> parity (no duplicate Responsibility for the same JobDescription) is
/// enforced in <c>JobDescriptionResponsibilitiesAppService</c>, not here.
/// </summary>
public class JobDescriptionResponsibility : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int JobDescriptionId { get; set; }

    public virtual JobDescription JobDescription { get; set; }

    public virtual int JobResponsibilityId { get; set; }

    public virtual JobResponsibility JobResponsibility { get; set; }
}
