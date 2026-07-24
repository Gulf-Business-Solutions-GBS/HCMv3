using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee task/reminder record (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeTask</c>, Wave 4 deferred bucket).
///
/// <see cref="TaskTypeId"/> FKs <c>HCMv3.GeneralMasterInfo.TaskType</c> (already ported).
///
/// <see cref="TaskStatus"/>/<see cref="MonitoringType"/>/<see cref="MonitoringUnit"/> are plain <c>int</c>
/// fields in the legacy source (NOT enums) -- preserved as <c>int</c> here, per instruction, rather than
/// inventing enums for them.
/// </summary>
public class EmployeeTask : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int TaskTypeId { get; set; }

    public virtual TaskType TaskType { get; set; }

    public virtual DateTime TaskDate { get; set; }

    public virtual int TaskStatus { get; set; }

    public virtual DateTime ReminderDate { get; set; }

    public virtual int MonitoringType { get; set; }

    public virtual int MonitoringCount { get; set; }

    public virtual int MonitoringUnit { get; set; }

    [StringLength(500)]
    public virtual string Comments { get; set; }
}
