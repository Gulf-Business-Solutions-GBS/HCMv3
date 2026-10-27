using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.SystemSecurity;

/// <summary>
/// Reminder-notification rule (ported from legacy XAF
/// HCM10.Module.BusinessObjects.SysSecurity.SystemNotification).
/// </summary>
public class SystemNotification : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [StringLength(256)]
    public virtual string NotificationName { get; set; }

    public virtual int NotificationConfigId { get; set; }

    public virtual NotificationConfig NotificationConfig { get; set; }

    public virtual int ReminderDays { get; set; }

    public virtual bool IsActive { get; set; }
}
