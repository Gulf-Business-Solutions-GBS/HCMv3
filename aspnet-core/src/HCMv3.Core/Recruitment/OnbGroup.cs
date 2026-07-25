using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.Recruitment;

/// <summary>
/// Onboarding task group, e.g. per job-title bundle of onboarding tasks (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Recruitment.OnbGroup</c>). Legacy manual <c>CreatedOn</c>/<c>CreatedBy</c>
/// /<c>UpdatedOn</c>/<c>UpdatedBy</c> fields are dropped in favor of <see cref="FullAuditedEntity"/>'s
/// built-in <c>CreationTime</c>/<c>CreatorUserId</c>/<c>LastModificationTime</c>/<c>LastModifierUserId</c> --
/// same simplification used throughout this port for the other Onb* entities below.
/// </summary>
public class OnbGroup : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [Required]
    [StringLength(256)]
    public virtual string NameEn { get; set; }

    [Required]
    [StringLength(256)]
    public virtual string NameAr { get; set; }
}
