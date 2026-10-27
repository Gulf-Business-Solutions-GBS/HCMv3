using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.TimeManagement;

/// <summary>
/// Per-week child of <see cref="DutyPeriod"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.TimeManagement.DutyPeriodDetail</c>), listed separately and linked back
/// via <see cref="DutyPeriodId"/>, following the flat parent+child pattern already used elsewhere in this
/// codebase (e.g. HolidayCalendar/Holiday).
///
/// DELIBERATE SIMPLIFICATION: the legacy <c>PerformValidation</c> requires all 7 Day1Duty..Day7Duty FKs to
/// be non-null. That requirement is NOT ported — all 7 day FKs are optional here (same simplification
/// rationale as <see cref="DutyPeriod"/>'s day FKs). Only the "Week not duplicated within the same
/// DutyPeriod" check is ported.
/// </summary>
public class DutyPeriodDetail : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int DutyPeriodId { get; set; }

    public virtual DutyPeriod DutyPeriod { get; set; }

    public virtual int Week { get; set; }

    public virtual int? Day1DutyId { get; set; }

    public virtual Duty Day1Duty { get; set; }

    public virtual int? Day2DutyId { get; set; }

    public virtual Duty Day2Duty { get; set; }

    public virtual int? Day3DutyId { get; set; }

    public virtual Duty Day3Duty { get; set; }

    public virtual int? Day4DutyId { get; set; }

    public virtual Duty Day4Duty { get; set; }

    public virtual int? Day5DutyId { get; set; }

    public virtual Duty Day5Duty { get; set; }

    public virtual int? Day6DutyId { get; set; }

    public virtual Duty Day6Duty { get; set; }

    public virtual int? Day7DutyId { get; set; }

    public virtual Duty Day7Duty { get; set; }
}
