using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.TimeManagement;

/// <summary>
/// Child of <see cref="YearlyTimeRegister"/>, one row per <see cref="TimeType"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.TimeManagement.YearlyTimeRegisterDetail</c>, Wave 4). Same shape and same
/// dead-code-skip as <see cref="MonthlyTimeRegisterDetail"/> (see its remarks for the dropped ~35
/// commented-out legacy fields).
/// </summary>
public class YearlyTimeRegisterDetail : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int YearlyTimeRegisterId { get; set; }

    public virtual YearlyTimeRegister YearlyTimeRegister { get; set; }

    public virtual int TimeTypeId { get; set; }

    public virtual TimeType TimeType { get; set; }

    /// <summary>
    /// Legacy stores this as a string, not a numeric type -- ported faithfully as <see cref="string"/>.
    /// </summary>
    public virtual string TimeValue { get; set; }
}
