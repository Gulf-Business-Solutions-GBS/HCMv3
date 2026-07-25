using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.JobProfileManagement;

/// <summary>
/// Aggregated child of <see cref="JobDescription"/> linking it to an <see cref="EducationCertificate"/>
/// (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.JobProfileManagement.JDEducationCertificate</c>). Legacy
/// <c>PerformValidation</c> parity (no duplicate Certificate for the same JobDescription) is enforced in
/// <c>JobDescriptionCertificatesAppService</c>, not here.
/// </summary>
public class JobDescriptionCertificate : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int JobDescriptionId { get; set; }

    public virtual JobDescription JobDescription { get; set; }

    public virtual int EducationCertificateId { get; set; }

    public virtual EducationCertificate EducationCertificate { get; set; }
}
