using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.TimeManagement;

/// <summary>
/// Child of <see cref="SimulationTimeRegister"/>, one row per <see cref="TimeType"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.TimeManagement.SimulationTimeRegisterDetail</c>, Wave 4). Same
/// dead-code-skip as <see cref="MonthlyTimeRegisterDetail"/> (see its remarks for the dropped ~35
/// commented-out legacy fields).
/// </summary>
public class SimulationTimeRegisterDetail : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int SimulationTimeRegisterId { get; set; }

    public virtual SimulationTimeRegister SimulationTimeRegister { get; set; }

    public virtual int TimeTypeId { get; set; }

    public virtual TimeType TimeType { get; set; }

    /// <summary>
    /// Legacy stores this as a string, not a numeric type -- ported faithfully as <see cref="string"/>.
    /// </summary>
    public virtual string TimeValue { get; set; }
}
