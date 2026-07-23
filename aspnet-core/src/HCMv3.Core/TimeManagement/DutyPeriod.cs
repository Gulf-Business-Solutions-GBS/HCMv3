using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.TimeManagement;

/// <summary>
/// Weekly Duty Period template (ported from legacy XAF <c>HCM10.Module.BusinessObjects.TimeManagement.DutyPeriod</c>).
///
/// DELIBERATE SIMPLIFICATION: the legacy entity has a dual-mode design — when the (not-yet-ported)
/// <c>SysService.GetTimeSettings().EnableRosterDuty</c> global setting is off, the 7 day-of-week Duty FKs
/// (SundayDuty..SaturdayDuty) are required; when it's on, the <see cref="DutyPeriodDetail"/> multi-week
/// child collection is used instead (with a "weeks must be contiguous" check). Both the 7 day-FKs AND the
/// <see cref="DutyPeriodDetail"/> child are ported here, but BOTH are left optional — no
/// EnableRosterDuty-driven conditional validation branching is ported (no global-settings infrastructure
/// exists in this port yet) and the "weeks must be contiguous" check is not ported. The legacy
/// <c>IExcelImport</c> marker and the <c>DutyPeriodCopy</c> "copy wizard" DTO are XAF UI concepts and are
/// not ported.
/// </summary>
public class DutyPeriod : FullAuditedEntity, IMustHaveTenant
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

    public virtual DateTime EndDate { get; set; }

    public virtual int? HolidayCalendarId { get; set; }

    public virtual HolidayCalendar HolidayCalendar { get; set; }

    public virtual int? SundayDutyId { get; set; }

    public virtual Duty SundayDuty { get; set; }

    public virtual int? MondayDutyId { get; set; }

    public virtual Duty MondayDuty { get; set; }

    public virtual int? TuesdayDutyId { get; set; }

    public virtual Duty TuesdayDuty { get; set; }

    public virtual int? WednesdayDutyId { get; set; }

    public virtual Duty WednesdayDuty { get; set; }

    public virtual int? ThursdayDutyId { get; set; }

    public virtual Duty ThursdayDuty { get; set; }

    public virtual int? FridayDutyId { get; set; }

    public virtual Duty FridayDuty { get; set; }

    public virtual int? SaturdayDutyId { get; set; }

    public virtual Duty SaturdayDuty { get; set; }
}
