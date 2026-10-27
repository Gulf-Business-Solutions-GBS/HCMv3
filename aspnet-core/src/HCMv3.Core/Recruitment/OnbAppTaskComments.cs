using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.Recruitment;

/// <summary>
/// A comment on an <see cref="OnbApplicantTasks"/> row (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Recruitment.OnbAppTaskComments</c>). See <see cref="OnbGroup"/> remarks
/// re: dropped legacy manual audit fields.
/// </summary>
public class OnbAppTaskComments : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [Required]
    public virtual int OnbApplicantTasksId { get; set; }
    public virtual OnbApplicantTasks OnbApplicantTasks { get; set; }

    [StringLength(4000)]
    public virtual string Comments { get; set; }

    public virtual bool IsByAdmin { get; set; }
}
