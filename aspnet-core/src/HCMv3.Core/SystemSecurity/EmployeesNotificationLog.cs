using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.SystemSecurity;

/// <summary>
/// Log of sent/dismissed in-app notifications (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Util.EmployeesNotificationLog</c>). Legacy's commented-out
/// <c>WorkFlowRole</c> FK was never active and is not ported.
/// </summary>
public class EmployeesNotificationLog : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int? EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual DateTime DismissTime { get; set; }

    public virtual int? TaskEventId { get; set; }

    public virtual TaskEvent TaskEvent { get; set; }

    /// <summary>
    /// Added 12-05-2022 (legacy comment) for Punch In/Out notification support.
    /// </summary>
    public virtual int? EmailNotificationLogId { get; set; }

    public virtual EmailNotificationLog EmailNotificationLog { get; set; }

    [System.ComponentModel.DataAnnotations.StringLength(128)]
    public virtual string NotificationType { get; set; }

    public virtual DateTime Date { get; set; }
}
