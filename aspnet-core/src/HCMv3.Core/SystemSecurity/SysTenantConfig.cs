using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.SystemSecurity;

/// <summary>
/// Generic date-effective tenant-scoped Key/Value configuration, categorized by functional area (ported
/// from legacy XAF HCM10.Module.BusinessObjects.SysSecurity.SysTenantConfig).
/// </summary>
public class SysTenantConfig : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [Required]
    [StringLength(256)]
    public virtual string Key { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    [StringLength(1000)]
    public virtual string KeyValue { get; set; }

    public virtual TenantConfigCategory ConfigCategory { get; set; }

    [StringLength(1000)]
    public virtual string ConfigDescription { get; set; }
}
