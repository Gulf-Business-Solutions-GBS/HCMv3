using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.Recruitment;

/// <summary>
/// An onboarding task belonging to an <see cref="OnbTemplate"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Recruitment.OnbTask</c>). See <see cref="OnbGroup"/> remarks re: dropped
/// legacy manual audit fields.
/// </summary>
public class OnbTask : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [Required]
    [StringLength(256)]
    public virtual string TaskNameEn { get; set; }

    [Required]
    [StringLength(256)]
    public virtual string TaskNameAr { get; set; }

    public virtual int? OnbTemplateId { get; set; }
    public virtual OnbTemplate OnbTemplate { get; set; }

    public virtual string Guideline { get; set; }

    public virtual int DueDays { get; set; }
}
