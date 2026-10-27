using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.CompetencyDevelopment;

namespace HCMv3.JobProfileManagement;

/// <summary>
/// Aggregated child of <see cref="JobDescription"/> linking it to a <see cref="Competency"/> (ported
/// from legacy XAF <c>HCM10.Module.BusinessObjects.JobProfileManagement.JDCompetency</c>). Legacy
/// <c>PerformValidation</c> parity (no duplicate Competency for the same JobDescription) is enforced in
/// <c>JobDescriptionCompetenciesAppService</c>, not here.
///
/// Note: <c>HCMv3.CompetencyDevelopment.Competency</c> already exists in this codebase (ported earlier),
/// so unlike most other cross-module dependencies encountered in this port, this FK is wired up directly
/// rather than dropped.
/// </summary>
public class JobDescriptionCompetency : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int JobDescriptionId { get; set; }

    public virtual JobDescription JobDescription { get; set; }

    public virtual int CompetencyId { get; set; }

    public virtual Competency Competency { get; set; }
}
