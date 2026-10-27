using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.SystemSecurity;

/// <summary>
/// Merge-token definition for an EmailTemplate (ported from legacy XAF
/// HCM10.Module.BusinessObjects.SysSecurity.EmailToken).
/// </summary>
public class EmailToken : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [Required]
    [StringLength(256)]
    public virtual string TokenName { get; set; }

    [StringLength(1000)]
    public virtual string TokenValue { get; set; }

    public virtual bool IsDateTimeType { get; set; }

    public virtual int EmailTemplateId { get; set; }

    public virtual EmailTemplate EmailTemplate { get; set; }

    public virtual bool ObjectKey { get; set; }
}
