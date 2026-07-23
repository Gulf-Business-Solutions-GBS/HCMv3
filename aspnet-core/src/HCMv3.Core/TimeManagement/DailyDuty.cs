using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.TimeManagement;

/// <summary>
/// Daily Duty master (ported from legacy XAF <c>HCM10.Module.BusinessObjects.TimeManagement.DailyDuty</c>).
/// The Roster subsystem's building block — near-identical shape to <see cref="Duty"/> but kept as a
/// separate entity since Roster entities reference <c>DailyDuty</c> while classic DutyPeriod references
/// <see cref="Duty"/>.
/// </summary>
public class DailyDuty : FullAuditedEntity, IMustHaveTenant
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

    public virtual TimeSpan StartTime { get; set; }

    public virtual TimeSpan EndTime { get; set; }

    public virtual bool IsNightShift { get; set; }

    public virtual int? BreakShiftId { get; set; }

    public virtual BreakShift BreakShift { get; set; }

    public virtual bool AllowTransStart { get; set; }

    public virtual bool AllowTransEnd { get; set; }
}
