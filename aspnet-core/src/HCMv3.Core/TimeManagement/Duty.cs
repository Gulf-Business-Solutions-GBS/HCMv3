using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.TimeManagement;

/// <summary>
/// Duty master (ported from legacy XAF <c>HCM10.Module.BusinessObjects.TimeManagement.Duty</c>).
/// Classic DutyPeriod building block (as opposed to <see cref="DailyDuty"/>, the Roster subsystem's
/// equivalent building block — kept as a separate entity, not merged).
/// The legacy <c>IExcelImport</c> marker and the <c>DutyCopy</c> "copy wizard" DTO are XAF UI concepts
/// and are not ported. Legacy sentinel FlexiStartTime/FlexiEndTime values (1800-01-01 00:00/23:59, meaning
/// "not set") are replaced here with nullable <see cref="TimeSpan"/>. <see cref="IsActive"/> defaults to
/// true for a new duty, as legacy <c>AfterConstruction</c> set it; an inactive duty stays on the records that
/// already use it but is no longer offered when choosing a duty.
/// </summary>
public class Duty : FullAuditedEntity, IMustHaveTenant
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

    public virtual int? BreakShiftId { get; set; }

    public virtual BreakShift BreakShift { get; set; }

    public virtual int? OvertimeTypeId { get; set; }

    public virtual OvertimeType OvertimeType { get; set; }

    public virtual bool AllowTransStart { get; set; }

    public virtual bool AllowTransEnd { get; set; }

    public virtual bool IsNightShift { get; set; }

    public virtual bool IsWeeklyOff { get; set; }

    public virtual bool IsRestDay { get; set; }

    public virtual bool IsHalfDay { get; set; }

    public virtual TimeSpan? FlexiStartTime { get; set; }

    public virtual TimeSpan? FlexiEndTime { get; set; }

    /// <summary>Legacy <c>Duty.IsActive</c>. True for a new duty (legacy <c>AfterConstruction</c>).</summary>
    public virtual bool IsActive { get; set; } = true;
}
