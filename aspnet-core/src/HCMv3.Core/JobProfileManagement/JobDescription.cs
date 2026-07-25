using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.OrganizationManagement;

namespace HCMv3.JobProfileManagement;

/// <summary>
/// Job description master (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.JobProfileManagement.JobDescription</c>). Legacy
/// <c>PerformValidation</c> parity (JobTitle required; ShortDescriptionEn/Ar and LongDescriptionEn/Ar
/// required and each unique across all JobDescriptions) is enforced in <c>JobDescriptionsAppService</c>,
/// not here. Its six aggregated legacy child collections (JDCertificate, JDDegree, JDInterviewQuestion,
/// JDResponsibility, JDSkill, JDCompetency) are ported as standalone linking entities
/// (<see cref="JobDescriptionCertificate"/>, <see cref="JobDescriptionDegree"/>,
/// <see cref="JobDescriptionInterviewQuestion"/>, <see cref="JobDescriptionResponsibility"/>,
/// <see cref="JobDescriptionSkill"/>, <see cref="JobDescriptionCompetency"/>), each linked back via its
/// own JobDescriptionId FK -- mirroring the EmployeeProject/EmployeeProjectDetail pattern used elsewhere
/// in this port, rather than XPO's aggregated <c>XPCollection</c>.
/// </summary>
public class JobDescription : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int JobTitleId { get; set; }

    public virtual JobTitle JobTitle { get; set; }

    [Required]
    [StringLength(256)]
    public virtual string ShortDescriptionEn { get; set; }

    [Required]
    [StringLength(256)]
    public virtual string ShortDescriptionAr { get; set; }

    [Required]
    public virtual string LongDescriptionEn { get; set; }

    [Required]
    public virtual string LongDescriptionAr { get; set; }

    [StringLength(64)]
    public virtual string Version { get; set; }

    public virtual DateTime VersionChangedOn { get; set; }
}
