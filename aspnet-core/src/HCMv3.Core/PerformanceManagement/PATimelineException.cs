using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.PerformanceManagement;

/// <summary>
/// Per-employee deadline extension for a <see cref="PATimeline"/> activity (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PerformanceManagement.PATimelineException</c>).
/// </summary>
public class PATimelineException : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int PAPeriodId { get; set; }

    public virtual PAPeriod PAPeriod { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual ActivityType ActivityType { get; set; }

    public virtual DateTime ExtensionDate { get; set; }
}
