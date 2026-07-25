using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.Recruitment;

/// <summary>
/// An onboarding video link (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Recruitment.OnbVideoLink</c>). See <see cref="OnbGroup"/> remarks re:
/// dropped legacy manual audit fields.
/// </summary>
public class OnbVideoLink : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [Required]
    [StringLength(256)]
    public virtual string NameEn { get; set; }

    [Required]
    [StringLength(256)]
    public virtual string NameAr { get; set; }

    [StringLength(500)]
    public virtual string Link { get; set; }

    public virtual bool IsActive { get; set; }
}
