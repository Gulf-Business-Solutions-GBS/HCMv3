using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.Recruitment;

/// <summary>
/// Onboarding template header (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Recruitment.OnbTemplate</c>). <see cref="TemplateId"/> is a legacy
/// business identifier distinct from the entity's own <c>Id</c>. See <see cref="OnbGroup"/> remarks re:
/// dropped legacy manual audit fields.
/// </summary>
public class OnbTemplate : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual long TemplateId { get; set; }

    [StringLength(256)]
    public virtual string TemplateTitle { get; set; }
}
