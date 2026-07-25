using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.Recruitment;

/// <summary>
/// Prior work-experience line of an <see cref="ApplicantCV"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Recruitment.CVWorkExp</c>).
/// </summary>
public class CVWorkExp : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [Required]
    public virtual int ApplicantCVId { get; set; }
    public virtual ApplicantCV ApplicantCV { get; set; }

    [StringLength(256)]
    public virtual string Company { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    [StringLength(256)]
    public virtual string Position { get; set; }

    public virtual string Responsibilities { get; set; }
}
