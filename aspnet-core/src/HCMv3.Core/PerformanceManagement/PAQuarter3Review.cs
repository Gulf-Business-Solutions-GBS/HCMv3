using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PerformanceManagement;

/// <summary>
/// "Quarter 3" review-stage KPI line for a <see cref="PAForm"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PerformanceManagement.PAQuarter3Review</c>). See <see cref="PAPlan"/>
/// remarks for why the repeated shape is not factored into a shared base class.
/// </summary>
public class PAQuarter3Review : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int PAFormId { get; set; }

    public virtual PAForm PAForm { get; set; }

    public virtual int KpiInfoId { get; set; }

    public virtual KpiInfo KpiInfo { get; set; }

    public virtual double Weight { get; set; }

    public virtual string Target { get; set; }

    public virtual int? AppraiseeRatingId { get; set; }

    public virtual PARating AppraiseeRating { get; set; }

    public virtual int? RatingId { get; set; }

    public virtual PARating Rating { get; set; }

    [StringLength(500)]
    public virtual string AppraiseeComments { get; set; }

    [StringLength(500)]
    public virtual string AppraiserComments { get; set; }
}
