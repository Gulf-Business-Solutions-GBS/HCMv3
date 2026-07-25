using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.Recruitment;

/// <summary>
/// A data row belonging to an <see cref="OnbTemplate"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Recruitment.OnbTemplateDetails</c>). See <see cref="OnbGroup"/> remarks
/// re: dropped legacy manual audit fields.
/// </summary>
public class OnbTemplateDetails : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [Required]
    public virtual int OnbTemplateId { get; set; }
    public virtual OnbTemplate OnbTemplate { get; set; }

    public virtual long TemplateId { get; set; }

    [StringLength(5000)]
    public virtual string TemplateData { get; set; }
}
