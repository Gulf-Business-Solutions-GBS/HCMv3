using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PerformanceManagement;

/// <summary>
/// KPI weighting line for a <see cref="PAEmployee"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PerformanceManagement.PAEmployeeDetail</c>). Legacy <c>FillReview</c>
/// (auto-create a <see cref="PAEmployeeRating"/> row for every existing <see cref="PAEmployeeReviewer"/>
/// when a detail line is added) is ported in <c>PAEmployeeDetailsAppService</c>.
/// </summary>
public class PAEmployeeDetail : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int PAEmployeeId { get; set; }

    public virtual PAEmployee PAEmployee { get; set; }

    public virtual int KpiInfoId { get; set; }

    public virtual KpiInfo KpiInfo { get; set; }

    public virtual double Weight { get; set; }

    public virtual string Target { get; set; }
}
