using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.JobProfileManagement;

/// <summary>
/// Aggregated child of <see cref="JobDescription"/> linking it to an <see cref="EducationDegree"/>
/// (ported from legacy XAF <c>HCM10.Module.BusinessObjects.JobProfileManagement.JDDegree</c>). Legacy
/// <c>PerformValidation</c> parity (no duplicate Degree for the same JobDescription) is enforced in
/// <c>JobDescriptionDegreesAppService</c>, not here.
/// </summary>
public class JobDescriptionDegree : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int JobDescriptionId { get; set; }

    public virtual JobDescription JobDescription { get; set; }

    public virtual int EducationDegreeId { get; set; }

    public virtual EducationDegree EducationDegree { get; set; }
}
