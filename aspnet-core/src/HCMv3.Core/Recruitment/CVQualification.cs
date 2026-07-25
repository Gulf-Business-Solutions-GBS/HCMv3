using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.Recruitment;

/// <summary>
/// Educational qualification line of an <see cref="ApplicantCV"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Recruitment.CVQualification</c>).
/// </summary>
public class CVQualification : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [Required]
    public virtual int ApplicantCVId { get; set; }
    public virtual ApplicantCV ApplicantCV { get; set; }

    public virtual int? QualificationId { get; set; }
    public virtual Qualification Qualification { get; set; }

    [StringLength(256)]
    public virtual string University { get; set; }

    public virtual double Percentage { get; set; }

    public virtual int PassedYear { get; set; }
}
