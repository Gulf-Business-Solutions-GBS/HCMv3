using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.SystemSecurity;

/// <summary>
/// Admin-editable email template (ported from legacy XAF
/// HCM10.Module.BusinessObjects.SysSecurity.EmailTemplate).
///
/// SIMPLIFICATION: the legacy EmailSetup FK (a bespoke multi-SMTP-profile picker) is not ported --
/// ASP.NET Zero already has its own native tenant SMTP settings, superseding that legacy concept. In its
/// place, SmtpProfile is kept as a free-text label field only (e.g. to note which logical
/// mail profile/purpose a template belongs to), with no FK/enforcement behind it.
/// </summary>
public class EmailTemplate : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [StringLength(64)]
    public virtual string Code { get; set; }

    [Required]
    [StringLength(256)]
    public virtual string NameEn { get; set; }

    [Required]
    [StringLength(256)]
    public virtual string NameAr { get; set; }

    /// <summary>
    /// Free-text label noting which logical SMTP profile/purpose this template is intended for.
    /// SIMPLIFICATION: replaces the legacy EmailSetup FK -- see class remarks.
    /// </summary>
    [StringLength(256)]
    public virtual string SmtpProfile { get; set; }

    [StringLength(500)]
    public virtual string Subject { get; set; }

    public virtual string EmailBodyHtml { get; set; }

    public virtual string EmailBodyText { get; set; }
}
