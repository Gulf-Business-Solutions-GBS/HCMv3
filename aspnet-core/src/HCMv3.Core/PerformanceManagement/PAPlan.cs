using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PerformanceManagement;

/// <summary>
/// "Plan" review-stage KPI line for a <see cref="PAForm"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PerformanceManagement.PAPlan</c>). Same repeated shape as
/// <see cref="PAQuarter1Review"/>/<see cref="PAMidYearReview"/>/<see cref="PAQuarter3Review"/>/
/// <see cref="PAAnnualYearReview"/> -- kept as separate concrete flat entities (rather than a shared base
/// class or TPH hierarchy) to match this codebase's established flat-entity-per-legacy-BO convention.
/// </summary>
public class PAPlan : FullAuditedEntity, IMustHaveTenant
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

    /// <summary>Appraiser rating (legacy property name is just <c>Rating</c>).</summary>
    public virtual int? RatingId { get; set; }

    public virtual PARating Rating { get; set; }

    [StringLength(500)]
    public virtual string AppraiseeComments { get; set; }

    [StringLength(500)]
    public virtual string AppraiserComments { get; set; }
}
