using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.OrganizationManagement;

namespace HCMv3.Recruitment;

/// <summary>
/// A single (task, job title) row within an <see cref="OnbGroup"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Recruitment.OnbGroupDetail</c>). See <see cref="OnbGroup"/> remarks re:
/// dropped legacy manual audit fields.
/// </summary>
public class OnbGroupDetail : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [Required]
    public virtual int OnbGroupId { get; set; }
    public virtual OnbGroup OnbGroup { get; set; }

    public virtual int? OnbTaskId { get; set; }
    public virtual OnbTask OnbTask { get; set; }

    public virtual int? JobTitleId { get; set; }
    public virtual JobTitle JobTitle { get; set; }
}
