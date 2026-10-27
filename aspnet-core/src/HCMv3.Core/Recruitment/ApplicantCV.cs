using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.OrganizationManagement;

namespace HCMv3.Recruitment;

/// <summary>
/// A CV/resume submitted by an <see cref="Applicant"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Recruitment.ApplicantCV</c>). Legacy <c>DocAttachment</c> (the uploaded
/// file) is dropped -- same simplification used throughout this port wherever a business object referenced
/// the legacy generic attachment entity, which has no HCMv3 equivalent. Legacy computed
/// <c>EmailCVName</c> (Applicant.Email/CVName concatenation alias) is not persisted -- computed in the DTO
/// mapping instead.
/// </summary>
public class ApplicantCV : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [Required]
    public virtual int ApplicantId { get; set; }
    public virtual Applicant Applicant { get; set; }

    public virtual int? JobTitleId { get; set; }
    public virtual JobTitle JobTitle { get; set; }

    [StringLength(256)]
    public virtual string CVName { get; set; }

    public virtual DateTime UploadDate { get; set; }
}
