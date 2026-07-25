using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.Recruitment;

/// <summary>
/// An onboarding task assigned to an <see cref="Applicant"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Recruitment.OnbApplicantTasks</c>). See <see cref="OnbGroup"/> remarks
/// re: dropped legacy manual audit fields.
/// </summary>
public class OnbApplicantTasks : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [Required]
    public virtual int ApplicantId { get; set; }
    public virtual Applicant Applicant { get; set; }

    public virtual int? OnbTaskId { get; set; }
    public virtual OnbTask Task { get; set; }

    public virtual int DueDays { get; set; }

    public virtual bool IsCompleted { get; set; }

    public virtual DateTime? CompletedOn { get; set; }

    [StringLength(4000)]
    public virtual string TemplateUserData { get; set; }
}
