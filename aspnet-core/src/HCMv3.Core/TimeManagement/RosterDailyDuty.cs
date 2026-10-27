using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.TimeManagement;

/// <summary>
/// Materialized, per-calendar-date roster assignment (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.TimeManagement.RosterDailyDuty</c>).
///
/// This looks like calculated/generated data, normally populated by an external legacy
/// <c>RosterService</c> calculation engine that is not present in the legacy source available for this
/// port. The legacy <c>PerformValidation()</c> is an empty no-op stub, so this is ported as a plain CRUD
/// entity with only basic required-field validation — no attempt is made to replicate a calculation engine
/// that isn't in scope/available.
/// </summary>
public class RosterDailyDuty : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual DateTime CalendarDate { get; set; }

    public virtual int RosterDutyScheduleId { get; set; }

    public virtual RosterDutySchedule RosterDutySchedule { get; set; }

    public virtual int DailyDutyId { get; set; }

    public virtual DailyDuty DailyDuty { get; set; }
}
