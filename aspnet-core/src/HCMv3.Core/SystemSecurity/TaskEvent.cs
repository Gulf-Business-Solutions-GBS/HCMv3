using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;

namespace HCMv3.SystemSecurity;

/// <summary>
/// Scheduled/pending notification task record (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Util.TaskEvent</c>). Legacy wrapped DevExpress's Outlook-style
/// <c>TaskImpl</c>/<c>ISupportNotifications</c> XAF plumbing (postpone-time picker, alarm popups, etc.);
/// only the persisted scalar task data is ported here -- the popup/alarm UI machinery
/// (<c>ReminderTime</c>/<c>PostponeTimeList</c>/<c>IsPostponed</c>/<c>BackupAlarmtime</c>/
/// <c>NotificationMessage</c>/<c>UniqueId</c>) is XAF-UI-only and dropped, along with the commented-out
/// (never-active) legacy <c>WorkflowRole</c>/<c>sendToAll</c> members. <see cref="ObjectType"/>/
/// <see cref="ObjectOId"/> preserve the legacy loose polymorphic reference (which entity/row this task was
/// raised for) as plain scalars, same as elsewhere in this module (e.g. loose EntityObjectId-style refs).
/// </summary>
public class TaskEvent : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int? RefEmployeeId { get; set; }

    public virtual Employee RefEmployee { get; set; }

    public virtual int? NotificationConfigId { get; set; }

    public virtual NotificationConfig NotificationConfig { get; set; }

    [StringLength(256)]
    public virtual string Subject { get; set; }

    public virtual string Description { get; set; }

    public virtual DateTime DueDate { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual TaskEventStatus Status { get; set; }

    public virtual int PercentCompleted { get; set; }

    public virtual DateTime? DateCompleted { get; set; }

    /// <summary>How long before <see cref="StartDate"/> the reminder/alarm should fire.</summary>
    public virtual TimeSpan? RemindIn { get; set; }

    public virtual DateTime? AlarmTime { get; set; }

    /// <summary>Loose polymorphic discriminator for the entity this task was raised for (legacy parity).</summary>
    public virtual int ObjectType { get; set; }

    /// <summary>Loose polymorphic row id for the entity this task was raised for (legacy parity).</summary>
    public virtual int ObjectOId { get; set; }
}
