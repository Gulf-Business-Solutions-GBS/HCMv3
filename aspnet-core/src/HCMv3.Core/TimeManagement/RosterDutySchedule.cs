using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.TimeManagement;

/// <summary>
/// Roster Duty Schedule (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.TimeManagement.RosterDutySchedule</c>).
///
/// NAMING NOTE (verified by reading the legacy source): despite living in the Roster subsystem, this
/// entity's <c>DutyPeriod</c> property actually references the CLASSIC <see cref="TimeManagement.DutyPeriod"/>
/// entity, NOT <see cref="RosterDutyPeriod"/>. This is a legacy naming inconsistency, ported faithfully
/// (FK target verified against the actual legacy property type, not just its name).
///
/// DELIBERATE SIMPLIFICATION: per the wave brief, <see cref="HolidayCalendarId"/> is ported as nullable
/// even though the legacy <c>PerformValidation</c> required it (<c>"HolidayCalendar cannot be blank"</c>);
/// that requirement is not enforced here.
/// </summary>
public class RosterDutySchedule : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [StringLength(SimpleLookupConsts.MaxCodeLength)]
    public virtual string Code { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public virtual string NameEn { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public virtual string NameAr { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual int DutyPeriodId { get; set; }

    public virtual DutyPeriod DutyPeriod { get; set; }

    public virtual int StartPoint { get; set; }

    public virtual int? HolidayCalendarId { get; set; }

    public virtual HolidayCalendar HolidayCalendar { get; set; }
}
