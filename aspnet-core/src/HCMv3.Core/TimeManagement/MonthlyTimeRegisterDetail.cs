using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.TimeManagement;

/// <summary>
/// Child of <see cref="MonthlyTimeRegister"/>, one row per <see cref="TimeType"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.TimeManagement.MonthlyTimeRegisterDetail</c>, Wave 4), listed separately
/// and linked back via <see cref="MonthlyTimeRegisterId"/>, following the flat parent+child pattern already
/// used elsewhere in this codebase (e.g. DutyPeriod/DutyPeriodDetail).
///
/// The legacy source has a large block of ~35 commented-out (dead) fields (an abandoned earlier
/// per-day-detail design, e.g. <c>ShiftStartTime1</c>, <c>ActualLateIn1</c>, <c>AnnualLeave</c> etc.) --
/// confirmed dead code, NOT ported.
/// </summary>
public class MonthlyTimeRegisterDetail : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int MonthlyTimeRegisterId { get; set; }

    public virtual MonthlyTimeRegister MonthlyTimeRegister { get; set; }

    public virtual int TimeTypeId { get; set; }

    public virtual TimeType TimeType { get; set; }

    /// <summary>
    /// Legacy stores this as a string, not a numeric type -- ported faithfully as <see cref="string"/>.
    /// </summary>
    public virtual string TimeValue { get; set; }
}
