using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// NotificationGroup (ported from legacy XAF <c>HCM10.Module.BusinessObjects.GeneralMasterInfo.NotificationGroup</c>).
/// Pure detail record: no Code/NameEn/NameAr. Owned by a <see cref="NotificationConfig"/>.
/// The legacy <c>WorkflowRole</c>, <c>WorkflowRoleGroup</c> and <c>OrganizationUnitType</c> FKs are not ported
/// (Workflow/Organization modules out of scope).
/// </summary>
public class NotificationGroup : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int? NotificationConfigId { get; set; }

    public virtual NotificationConfig NotificationConfig { get; set; }
}
