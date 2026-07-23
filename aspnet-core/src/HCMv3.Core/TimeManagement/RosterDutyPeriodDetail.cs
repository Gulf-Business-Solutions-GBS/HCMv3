using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.TimeManagement;

/// <summary>
/// Per-week child of <see cref="RosterDutyPeriod"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.TimeManagement.RosterDutyPeriodDetail</c>), listed separately and linked
/// back via <see cref="RosterDutyPeriodId"/>. Shape is near-identical to <see cref="DutyPeriodDetail"/>, but
/// the day-of-week FKs reference <see cref="DailyDuty"/> (the Roster subsystem's building block) instead of
/// <see cref="Duty"/> — this is the key difference from <see cref="DutyPeriodDetail"/>, not a mistake.
///
/// The legacy <c>PerformValidation()</c> is an empty no-op stub, so only basic required-field validation
/// (RosterDutyPeriodId, Week) is ported here — there is no legacy behavior to replicate.
/// </summary>
public class RosterDutyPeriodDetail : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int RosterDutyPeriodId { get; set; }

    public virtual RosterDutyPeriod RosterDutyPeriod { get; set; }

    public virtual int Week { get; set; }

    public virtual int? Day1DutyId { get; set; }

    public virtual DailyDuty Day1Duty { get; set; }

    public virtual int? Day2DutyId { get; set; }

    public virtual DailyDuty Day2Duty { get; set; }

    public virtual int? Day3DutyId { get; set; }

    public virtual DailyDuty Day3Duty { get; set; }

    public virtual int? Day4DutyId { get; set; }

    public virtual DailyDuty Day4Duty { get; set; }

    public virtual int? Day5DutyId { get; set; }

    public virtual DailyDuty Day5Duty { get; set; }

    public virtual int? Day6DutyId { get; set; }

    public virtual DailyDuty Day6Duty { get; set; }

    public virtual int? Day7DutyId { get; set; }

    public virtual DailyDuty Day7Duty { get; set; }
}
