using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.SystemSettings;

/// <summary>
/// Tenant-wide notification configuration (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.SysSettings.ManageNotificationSettings</c>). Legacy <c>OnDeleting</c>
/// unconditionally throws -- enforced in
/// <c>ManageNotificationSettingsAppService.DeleteNotificationSettings</c>, not here.
/// </summary>
public class ManageNotificationSettings : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual bool DisableNotificationWindow { get; set; }

    public virtual AdvancePaymentReminderDateType AdvancePaymentReminderDateType { get; set; }
}
