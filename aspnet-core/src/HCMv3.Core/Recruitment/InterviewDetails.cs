using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.Recruitment;

/// <summary>
/// An interview scheduled for an <see cref="ApplicationStatus"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Recruitment.InterviewDetails</c>). Legacy <c>DocAttachment</c> is
/// dropped (same simplification used throughout this port -- no HCMv3 equivalent).
/// </summary>
public class InterviewDetails : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [Required]
    public virtual int ApplicationStatusId { get; set; }
    public virtual ApplicationStatus ApplicationStatus { get; set; }

    public virtual DateTime InterviewDate { get; set; }

    public virtual DateTime InterviewTime { get; set; }

    [StringLength(256)]
    public virtual string Place { get; set; }

    public virtual int? Interviewer1Id { get; set; }

    public virtual int? Interviewer2Id { get; set; }

    public virtual int? Interviewer3Id { get; set; }

    public virtual string Comments1 { get; set; }

    public virtual string Comments2 { get; set; }

    public virtual string Comments3 { get; set; }
}
