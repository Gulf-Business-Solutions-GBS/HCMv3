using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.SystemSecurity;

/// <summary>
/// Outbound email send-attempt log (ported from legacy XAF
/// HCM10.Module.BusinessObjects.SysSecurity.EmailNotificationLog). Ported ahead of PunchNotificationLog
/// (in the same batch) since the latter FKs this entity.
/// </summary>
public class EmailNotificationLog : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual DateTime Date { get; set; }

    [StringLength(256)]
    public virtual string Email { get; set; }

    [StringLength(256)]
    public virtual string Source { get; set; }

    [StringLength(500)]
    public virtual string Subject { get; set; }

    public virtual string Body { get; set; }

    public virtual bool IsSuccessful { get; set; }

    public virtual string Exception { get; set; }
}
