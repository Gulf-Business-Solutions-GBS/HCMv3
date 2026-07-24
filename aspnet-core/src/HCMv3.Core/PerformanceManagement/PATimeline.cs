using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PerformanceManagement;

/// <summary>
/// Appraisal timeline window per <see cref="PAPeriod"/>/<see cref="ActivityType"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PerformanceManagement.PATimeline</c>). The legacy in-memory
/// activity-type-ordering check (<c>PerformValidation</c>'s "ActivityType should be in order") is enforced
/// in <c>PATimelinesAppService</c>.
/// </summary>
public class PATimeline : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int PAPeriodId { get; set; }

    public virtual PAPeriod PAPeriod { get; set; }

    public virtual ActivityType ActivityType { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }
}
