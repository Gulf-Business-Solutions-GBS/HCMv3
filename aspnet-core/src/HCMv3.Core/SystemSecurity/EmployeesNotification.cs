using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.SystemSecurity;

/// <summary>
/// In-app notification record for an employee, raised from a <see cref="TaskEvent"/> (ported from legacy
/// XAF <c>HCM10.Module.BusinessObjects.Util.EmployeesNotification</c>). Legacy's commented-out
/// <c>IsNotificationDeleted != True1</c> filtered collection variant was never active and is not ported.
/// </summary>
public class EmployeesNotification : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int? TaskEventId { get; set; }

    public virtual TaskEvent TaskEvent { get; set; }

    public virtual bool IsNotificationDeleted { get; set; }

    public virtual bool IsNotificationRead { get; set; }
}
